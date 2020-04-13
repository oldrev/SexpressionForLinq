using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using Sprache;
using System.Reflection;

namespace SexprForLinq {
    using static Sprache.Parse;

    /// <summary>
    /// 一个简化的 S-Eval 解析器
    /// 与标准的 Lisp 不同
    /// </summary>
    public static class SexprParser {

        // 辅助函数
        static Parser<T> SexprToken<T>(this Parser<T> parser) {
            if (parser == null) {
                throw new ArgumentNullException(nameof(parser));
            }

            return from leading in SpaceChar.Many()
                   from item in parser
                   from trailing in SpaceChar.Many()
                   select item;
        }

        static Parser<T> ReducedOr<T>(params Parser<T>[] parsers) {
            return parsers.Aggregate((p1, p2) => Parse.Or(p1, p2));
        }

        //   public delegate Expression CustomFunction(EvalContext<TargetException> context);
        const string AtomAllowedSymbolicChars = @"`~!@#$%^&*-_+=|/?,<>;,";

        // 字符定义

        static Parser<char> SpaceChar = Char(' ').Or(Char('\t')).Or(Char(',')).Or(Char('\r')).Or(Char('\n'));
        static Parser<char> SymbolHeadChar = Letter.Or(Chars("+-%/!_?><="));
        static Parser<char> SymbolRestChar = SymbolHeadChar.Or(Digit);
        static Parser<char> IdentifierHeadChar = Letter.Or(Char('_'));
        static Parser<char> IdentifierRestChar = IdentifierHeadChar.Or(Digit);
        static Parser<char> HexDigitChar = Digit.Or(Chars("ABCDEFabcdef"));
        static Parser<char> OpenParenChar = Char('(');
        static Parser<char> CloseParenChar = Char(')');
        static Parser<char> LeftSquareBracketChar = Char('[');
        static Parser<char> RightSquareBracketChar = Char(']');

        static Parser<string> Symbol =
            (from first in SymbolHeadChar.AtLeastOnce()
             from rest in SymbolRestChar.Many()
             select first.Concat(rest))
            .Text();

        static Parser<string> Identifier =
            (from first in IdentifierHeadChar.AtLeastOnce()
             from rest in IdentifierRestChar.Many()
             select first.Concat(rest))
            .Text();

        //Tokens
        static Parser<char> LeftSquareBracketToken = LeftSquareBracketChar.SexprToken();
        static Parser<char> RightSquareBracketToken = RightSquareBracketChar.SexprToken();
        static Parser<char> OpenParenToken = OpenParenChar.SexprToken();
        static Parser<char> CloseParenToken = CloseParenChar.SexprToken();


        // 浮点数
        static Parser<NodeBinder> FloatLiteralToken =>
            (Digit.AtLeastOnce().Then(_ => Char('.')).Then(_ => Digit.AtLeastOnce()))
            .Text().SexprToken().BindFloatConstant().Named(nameof(FloatLiteralToken));

        // 整数
        static Parser<NodeBinder> IntegerLiteralToken =>
            Digit.AtLeastOnce().SexprToken().Text()
            .BindIntegerConstant()
            .Named(nameof(IntegerLiteralToken));

        // 字面量表达式
        static Parser<NodeBinder> LiteralToken =>
            ReducedOr(
                NumberLiteralToken,
                StringLiteralToken,
                BooleanLiteralToken,
                DateTimeLiteral,
                DateTimeOffsetLiteral)
            .Named(nameof(LiteralToken));

        // 布尔型常量
        static Parser<NodeBinder> BooleanLiteralToken =>
            TrueLiteral.Or(FalseLiteral)
            .Named(nameof(BooleanLiteralToken));

        // true 常量
        static Parser<NodeBinder> TrueLiteral =>
            String("true").SexprToken().Text()
            .BindTrueConstant()
            .Named(nameof(TrueLiteral));

        // false 常量
        static Parser<NodeBinder> FalseLiteral =>
            String("false").SexprToken().Text()
            .BindFalseConstant()
            .Named(nameof(FalseLiteral));

        // null 常量
        static Parser<NodeBinder> NullConstant =>
            String("null").SexprToken().Text()
            .BindNullConstant()
            .Named(nameof(NullConstant));

        // 数字字面值
        static Parser<NodeBinder> NumberLiteralToken =>
            FloatLiteralToken.Or(IntegerLiteralToken)
            .Named(nameof(NumberLiteralToken));

        // DateTime 型常量
        static Parser<NodeBinder> DateTimeLiteral =>
            (String("dt").Then(_ => SingleQuotedString.Or(DoubleQuotedString)))
            .BindDateTimeConstant();

        // DateTimeOffset 型常量
        static Parser<NodeBinder> DateTimeOffsetLiteral =>
            (String("dto").Then(_ => SingleQuotedString.Or(DoubleQuotedString)))
            .BindDateTimeOffsetConstant();

        // TimeSpan 型常量
        static Parser<NodeBinder> TimeSpanLiteral =>
            (String("ts").Then(_ => SingleQuotedString.Or(DoubleQuotedString)))
            .BindTimeSpanConstant();

        //字符串常量
        static Parser<NodeBinder> StringLiteralToken =>
            (SingleQuotedString.Or(DoubleQuotedString)).BindStringConstant()
            .Named(nameof(StringLiteralToken));

        // 单引号包括的字符串
        static Parser<string> SingleQuotedString =>
            (from lquote in Char('\'')
             from stringLiteral in CharExcept('\'').XMany().Text()
             from rquote in Char('\'')
             select stringLiteral)
            .Named(nameof(SingleQuotedString));

        // 双引号包括的字符串
        static Parser<string> DoubleQuotedString =
            (from lquote in Char('"')
             from stringLiteral in CharExcept('"').XMany().Text()
             from rquote in Char('"')
             select stringLiteral)
            .Named(nameof(DoubleQuotedString));

        // 匹配属性导航表达式 xxx[.yyy[.zzz]]
        static Parser<NodeBinder> DotExpressionToken =
            (from first in Identifier // '.' 之前有个标识符
             from rest in (Char('.').Then(_ => Identifier)).Many() // '.' 之后的标识符可以重复零次或多次
             select new NodeBinder(ctx => ctx.AccessProperties(new string[] { first }.Concat(rest)))
            )
            .SexprToken()
            .Named(nameof(DotExpressionToken));

        // 访问用户提供常量 Token
        static Parser<NodeBinder> ContextSymbolToken =
            (from lead in Char('$')
             from name in Symbol.Text()
             select name).BindUserConstant().SexprToken();

        // 列表

        /*
        static Parser<NodeBinder> Form =>
            Literal.Or(List).Or(Vector);

        static Parser<IEnumerable<NodeBinder>> List =>
            OpenParenChar.Then()

        // 字面量
        static Parser<NodeBinder> Literal =>
            FloatLiteralToken.Or(IntegerLiteralToken).Or(BooleanConstant)
            */

        static Parser<NodeBinder> EmptyList =
            OpenParenToken.Then(_ => CloseParenToken)
            .BindTrueConstant()
            .Named(nameof(EmptyList));

        // vector 数组类型
        static Parser<IEnumerable<NodeBinder>> Vector =>
            (from l in LeftSquareBracketToken
             from reducedMembers in (LiteralToken.Or(UnaryContextSymbol)).Many()
             from r in RightSquareBracketToken
             select reducedMembers)
            .Named(nameof(Vector));

        // 布尔型列表
        static Parser<NodeBinder> BooleanList => AndAlsoList.Or(OrElseList).Or(NotList);

        static Parser<NodeBinder> AndAlsoList =>
            from lp in OpenParenToken
            from andOp in AndAlsoOperator
            from first in BooleanExpression.Once()
            from rest in BooleanExpression.AtLeastOnce()
            from rp in CloseParenToken
            select new NodeBinder(ctx => (first.Concat(rest)).Select(lm => lm(ctx)).ToAndAlsoExpression());

        static Parser<NodeBinder> OrElseList =>
            from lp in OpenParenToken
            from orOp in OrElseOperator
            from first in BooleanExpression.Once()
            from rest in BooleanExpression.AtLeastOnce()
            from rp in CloseParenToken
            select new NodeBinder(ctx => (first.Concat(rest)).Select(lm => lm(ctx)).ToOrElseExpression());

        static Parser<NodeBinder> NotList =>
            from lp in OpenParenToken
            from notOp in NotOperator
            from operand in BooleanExpression
            from rp in CloseParenToken
            select new NodeBinder(ctx => Expression.Not(operand(ctx)));

        static Parser<NodeBinder> BooleanExpression =>
            ReducedOr(BooleanLiteralToken, BooleanList, ComparisonList)
            .Named(nameof(BooleanExpression));

        // 比较运算符操作列表
        static Parser<NodeBinder> ComparisonList =>
            ReducedOr(
                GreaterThanList, GreaterThanOrEqualList, LessThanList, LessOrEqualList,
                EqualList, NotEqualList,
                InList, NotInList, UserOperatorList)
            .Named(nameof(ComparisonList));


        static Parser<NodeBinder> GreaterThanList =>
            from lp in OpenParenToken
            from gt in GreaterThanOperator
            from left in DotExpressionToken
            from right in UnaryRightOperand
            from rp in CloseParenToken
            select new NodeBinder(ctx => Expression.GreaterThan(left(ctx), right(ctx)));

        static Parser<NodeBinder> GreaterThanOrEqualList =>
            from lp in OpenParenToken
            from gt in GreaterEqualOperator
            from left in DotExpressionToken
            from right in UnaryRightOperand
            from rp in CloseParenToken
            select new NodeBinder(ctx => Expression.GreaterThanOrEqual(left(ctx), right(ctx)));

        static Parser<NodeBinder> LessThanList =>
            from lp in OpenParenToken
            from gt in LesserThanOperator
            from left in DotExpressionToken
            from right in UnaryRightOperand
            from rp in CloseParenToken
            select new NodeBinder(ctx => Expression.LessThan(left(ctx), right(ctx)));

        static Parser<NodeBinder> LessOrEqualList =>
            from lp in OpenParenToken
            from gt in LessOrEqualOperator
            from left in DotExpressionToken
            from right in UnaryRightOperand
            from rp in CloseParenToken
            select new NodeBinder(ctx => Expression.LessThanOrEqual(left(ctx), right(ctx)));

        static Parser<NodeBinder> EqualList =>
            from lp in OpenParenToken
            from gt in EqualOperator
            from left in DotExpressionToken
            from right in UnaryRightOperand
            from rp in CloseParenToken
            select new NodeBinder(ctx => Expression.Equal(left(ctx), right(ctx)));

        static Parser<NodeBinder> NotEqualList =>
            from lp in OpenParenToken
            from gt in NotEqualOperator
            from left in DotExpressionToken
            from right in UnaryRightOperand
            from rp in CloseParenToken
            select new NodeBinder(ctx => Expression.NotEqual(left(ctx), right(ctx)));

        static Parser<NodeBinder> InList =>
            from lp in OpenParenToken
            from inOpr in InOperator
            from left in DotExpressionToken
            from list in Vector
            from rp in CloseParenToken
            select Binder.BindInList(left, list);

        static Parser<NodeBinder> NotInList =>
            from lp in OpenParenToken
            from inOpr in NotInOperator
            from left in DotExpressionToken
            from list in Vector
            from rp in CloseParenToken
            select Binder.BindNotInList(left, list);

        static Parser<NodeBinder> UserOperatorList =>
            (from lp in OpenParenToken
             from opr in UserOperator
             from left in DotExpressionToken
             from right in UnaryRightOperand
             from rp in CloseParenToken
             select Binder.BindUserOperator(opr, left, right))
            .Named(nameof(UserOperatorList));

        static Parser<NodeBinder> UnaryContextSymbol =>
            ContextSymbolToken.Except(Keywords)
            .Named(nameof(UnaryContextSymbol));

        static Parser<NodeBinder> UnaryRightOperand =>
            (UnaryContextSymbol.Or(LiteralToken));

        // <summary>
        //所有的内部运算符
        //public static  Parser<Eval> BooleanList = 
        //逻辑运算符
        static Parser<IEnumerable<char>> AndAlsoOperator = String("and").SexprToken().Named(nameof(AndAlsoOperator));

        static Parser<IEnumerable<char>> OrElseOperator = String("or").SexprToken().Named(nameof(OrElseOperator));

        static Parser<IEnumerable<char>> NotOperator = String("not").SexprToken().Text();

        static Parser<IEnumerable<char>> GreaterThanOperator = String(">").Or(String("gt")).SexprToken();

        static Parser<IEnumerable<char>> GreaterEqualOperator = String(">=").Or(String("ge")).SexprToken();

        static Parser<IEnumerable<char>> LesserThanOperator = String("<").Or(String("lt")).SexprToken();

        static Parser<IEnumerable<char>> LessOrEqualOperator = String("<=").Or(String("le")).SexprToken();

        static Parser<IEnumerable<char>> EqualOperator = String("=").Or(String("eq")).SexprToken();

        static Parser<IEnumerable<char>> NotEqualOperator = String("!=").Or(String("noteq")).SexprToken();

        static Parser<IEnumerable<char>> InOperator = String("in").SexprToken();

        static Parser<IEnumerable<char>> NotInOperator = String("!in").SexprToken().Named(nameof(NotInOperator));

        static Parser<IEnumerable<char>> BultinOperators =
            ReducedOr(
                AndAlsoOperator, OrElseOperator, NotOperator,
                GreaterThanOperator, GreaterEqualOperator,
                LesserThanOperator, LessOrEqualOperator,
                EqualOperator, NotEqualOperator,
                InOperator, NotInOperator)
            .Named(nameof(BultinOperators));

        static Parser<string> UserOperator = Symbol.Except(BultinOperators).Token().Text().Named(nameof(UserOperator));

        static Parser<IEnumerable<char>> Keywords = BultinOperators.Or(UserOperator);

        static Parser<NodeBinder> FilterExpression =>
            ReducedOr(GlobalEmptyList, BooleanList, ComparisonList).Named(nameof(FilterExpression));

        static Parser<NodeBinder> GlobalEmptyList => EmptyList;

        public static Expression ParseAndBind(object target, string input, IReadOnlyDictionary<string, object> symbols = null) {
            var targetExpr = Expression.Constant(target);
            return ParseAndBind(targetExpr, input, symbols);
        }

        static Expression ParseAndBind(Expression expr, string input, IReadOnlyDictionary<string, object> symbols = null) {
            var evaluation = FilterExpression.Parse(input);
            var linqExpr = evaluation(new BindingContext(expr, symbols));
            return linqExpr;
        }

        /// <summary>
        /// 通过 S-Exprssion 生成 WHERE 的谓词LINQ Expression，强类型版本
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="sexpr"></param>
        /// <param name="symbols"></param>
        /// <returns></returns>
        public static Expression<Func<TSource, bool>> ParseAndBindToWherePredicate<TSource>(
            string sexpr, IReadOnlyDictionary<string, object> symbols = null) {
            var param = Expression.Parameter(typeof(TSource));
            var filterExpr = ParseAndBind(param, sexpr, symbols);
            return Expression.Lambda<Func<TSource, bool>>(filterExpr, param);
        }

        /// <summary>
        /// 通过 S-Exprssion 生成 WHERE 的谓词 LINQ Expression，弱类型版本
        /// 这个更有用
        /// </summary>
        /// <param name="sourceType">IQueryable 的泛型类型</param>
        /// <param name="sexpr"></param>
        /// <param name="symbols"></param>
        /// <returns></returns>
        public static Expression ParseAndBindToUntypedWherePredicate(
            Type sourceType, string sexpr, IReadOnlyDictionary<string, object> symbols = null) {
            var param = Expression.Parameter(sourceType);
            var filterExpr = ParseAndBind(param, sexpr, symbols);
            var lambdaType = typeof(Func<,>).GetTypeInfo().MakeGenericType(sourceType, typeof(bool));
            return Expression.Lambda(lambdaType, filterExpr, param);
        }

    }

}
