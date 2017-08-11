using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Globalization;
using Sprache;
using System.Reflection;

namespace SexpressionForLinq
{

    internal delegate Expression NodeBinder(BindingContext context);


    internal static class Binder
    {
        public static Expression ToAndAlsoExpression(this IEnumerable<Expression> exps) =>
            exps.Aggregate((e1, e2) => Expression.AndAlso(e1, e2));

        public static Expression ToOrElseExpression(this IEnumerable<Expression> exps) =>
            exps.Aggregate((e1, e2) => Expression.OrElse(e1, e2));

        public static Parser<NodeBinder> BindTrueConstant<T>(this Parser<T> parser) =>
            parser.Return(new NodeBinder(ctx => Expression.Constant(true, typeof(bool))));

        public static Parser<NodeBinder> BindFalseConstant<T>(this Parser<T> parser) =>
            parser.Return(new NodeBinder(ctx => Expression.Constant(false, typeof(bool))));

        public static Parser<NodeBinder> BindNullConstant<T>(this Parser<T> parser) =>
            parser.Return(new NodeBinder(ctx => Expression.Constant(null)));

        public static Parser<NodeBinder> BindFloatConstant(this Parser<string> parser) =>
            parser.Select(s => new NodeBinder(ctx => Expression.Constant(double.Parse(s))));

        public static Parser<NodeBinder> BindIntegerConstant(this Parser<string> parser) =>
            parser.Select(s => new NodeBinder(ctx => Expression.Constant(int.Parse(s))));

        public static Parser<NodeBinder> BindStringConstant(this Parser<string> parser) =>
            parser.Select(s => new NodeBinder(ctx => Expression.Constant(s, typeof(string))));

        public static Parser<NodeBinder> BindDateTimeConstant(this Parser<string> parser)
        {
            return parser.Select(s => new NodeBinder(ctx => Expression.Constant(DateTime.Parse(s, CultureInfo.CurrentUICulture))));
        }

        public static Parser<NodeBinder> BindDateTimeOffsetConstant(this Parser<string> parser)
        {
            return parser.Select(s => new NodeBinder(ctx => Expression.Constant(DateTimeOffset.Parse(s, CultureInfo.CurrentUICulture))));
        }

        public static Parser<NodeBinder> BindTimeSpanConstant(this Parser<string> parser)
        {
            return parser.Select(s => new NodeBinder(ctx => Expression.Constant(TimeSpan.Parse(s, CultureInfo.CurrentUICulture))));
        }

        public static Parser<NodeBinder> BindUserConstant(this Parser<string> parser)
        {
            return parser.Select(s => new NodeBinder(ctx =>
            {
                if (!ctx.UserConstants.ContainsKey(s))
                {
                    throw new BindingException($"User constant '{s}' not found");
                }

                return ctx.UserConstants[s];
            }));
        }

        public static NodeBinder BindInList(NodeBinder left, IEnumerable<NodeBinder> rest)
        {
            return new NodeBinder(ctx =>
            {
                var equalExprs = rest.Select(
                    n =>
                    Expression.Equal(left(ctx), n(ctx)));
                return equalExprs.ToOrElseExpression();
            });
        }

        public static NodeBinder BindNotInList(NodeBinder left, IEnumerable<NodeBinder> rest)
        {
            return new NodeBinder(ctx =>
            {
                var equalExprs = rest.Select(
                    n =>
                    Expression.NotEqual(left(ctx), n(ctx)));
                return equalExprs.ToOrElseExpression();
            });
        }

        public static NodeBinder BindUserOperator(string opr, NodeBinder left, NodeBinder right)
        {
            return new NodeBinder(ctx =>
            {
                if (!ctx.UserFunctions.ContainsKey(opr))
                {
                    throw new BindingException($"Undefined user function: '{opr}'");
                }
                var func = ctx.UserFunctions[opr];
                var leftExpr = left(ctx);
                var rightExpr = right(ctx);
                if (func.GetMethodInfo().IsStatic)
                {
                    return Expression.Call(func.GetMethodInfo(), leftExpr, rightExpr);
                }
                else
                {
                    return Expression.Call(Expression.Constant(func.Target), func.GetMethodInfo(), leftExpr, rightExpr);
                }
            });
        }


    }
}
