using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Reflection;
using Sprache;

namespace SexprForLinq
{
    /// <summary>
    /// 绑定过程中的上下文环境
    /// </summary>
    internal sealed class BindingContext
    {
        /// <summary>
        /// 左操作数，用于操作的主对象
        /// </summary>
        public Expression TargetExpression { get; private set; }

        public Type TargetType { get; private set; }

        /// <summary>
        /// 上下文
        /// </summary>
        public IReadOnlyDictionary<string, object> Symbols { get; private set; }

        public IReadOnlyDictionary<string, Expression> UserConstants { get; private set; }

        public IReadOnlyDictionary<string, Delegate> UserFunctions { get; private set; }

        public BindingContext(Expression target, IReadOnlyDictionary<string, object> symbols = null)
        {
            if (symbols == null)
            {
                this.Symbols = new Dictionary<string, object>(0);
                this.UserConstants = new Dictionary<string, Expression>();
                this.UserFunctions = new Dictionary<string, Delegate>(0);
            }
            else
            {
                this.Symbols = symbols;
            }
            this.TargetExpression = target;
            this.TargetType = target.GetType();

            if (this.Symbols.Keys.Any(k => string.IsNullOrEmpty(k)))
            {
                throw new ArgumentOutOfRangeException(nameof(symbols));
            }

            this.SetSymbols();
        }

        public BindingContext(object target, IReadOnlyDictionary<string, object> symbols = null)
            : this(Expression.Constant(target))
        {

        }

        private void SetSymbols()
        {
            var userConstants = new Dictionary<string, Expression>();
            var userFunctions = new Dictionary<string, Delegate>();

            foreach (var item in this.Symbols)
            {
                switch (item.Value)
                {
                    case null:
                        userConstants.Add(item.Key, Expression.Constant(null));
                        break;

                    case Delegate func:
                        userFunctions.Add(item.Key, func);
                        break;

                    default:
                        userConstants.Add(item.Key, Expression.Constant(item.Value));
                        break;
                }
            }
            var y = typeof(Action);
            this.UserConstants = userConstants;

            this.UserFunctions = userFunctions;
        }

        public Expression AccessProperties(IEnumerable<string> props)
        {
            var expr = MakeMemberAccessExpression(this.TargetExpression, props.First());
            foreach (var p in props.Skip(1))
            {
                expr = MakeMemberAccessExpression(expr, p);
            }
            return expr;
        }

        private Expression MakeMemberAccessExpression(Expression objectExpr, string propertyName)
        {
            var propertyExpr = Expression.PropertyOrField(objectExpr, propertyName);
            MemberInfo mi = GetPropertyOrFieldInfo(objectExpr.Type, propertyName);

            return Expression.MakeMemberAccess(objectExpr, mi);
        }

        private MemberInfo GetPropertyOrFieldInfo(Type objectType, string propertyName)
        {
            var members = objectType.GetMembers(BindingFlags.Public | BindingFlags.Instance);
            var mi = (from it in members
                      where (it is PropertyInfo || it is FieldInfo) && it.Name == propertyName
                      select it).SingleOrDefault();
            if (mi == null)
            {
                throw new BindingException(
                    $"The type {objectType.FullName} does not have a accessable field/property that named: {propertyName}");
            }
            return mi;
        }
    }
}
