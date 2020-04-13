using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SexprForLinq
{
    /// <summary>
    /// IQueryable 的强类型和弱类型扩展
    /// </summary>
    public static class QueryableExtensions
    {

        public static IQueryable<TSource> Where<TSource>(
            this IQueryable<TSource> source,
            string sexpr, IReadOnlyDictionary<string, object> symbols = null)
        {
            var predicate = SexprParser.ParseAndBindToWherePredicate<TSource>(sexpr, symbols);

            var linqWhereType = Where_TSource_2(typeof(TSource));
            var callWhere = Expression.Call(null, linqWhereType, source.Expression, Expression.Quote(predicate));
            return source.Provider.CreateQuery<TSource>(callWhere);
        }

        public static IQueryable Where(this IQueryable source, Type sourceType, string sexpr, IReadOnlyDictionary<string, object> symbols = null)
        {
            var predicate = SexprParser.ParseAndBindToUntypedWherePredicate(sourceType, sexpr, symbols);
            var linqWhereType = Where_TSource_2(sourceType);
            var callWhere = Expression.Call(null, linqWhereType, source.Expression, Expression.Quote(predicate));
            return source.Provider.CreateQuery(callWhere);
        }

        private static MethodInfo Where_TSource_2(Type TSource)
        {
            var method =
                    new Func<IQueryable<object>, Expression<Func<object, bool>>, IQueryable<object>>(Queryable.Where)
                    .GetMethodInfo()
                    .GetGenericMethodDefinition();
            return method.MakeGenericMethod(TSource);
        }



    }
}
