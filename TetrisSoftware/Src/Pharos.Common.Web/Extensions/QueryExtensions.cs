using System.Linq.Expressions;
using Pharos.Common.Web.Models.DataGrid;

namespace Pharos.Common.Web.Extensions;

public static class QueryExtensions
{
    public static IQueryable<T> SortBy<T>(this IQueryable<T> source, string propertyName, SortDirection direction)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (string.IsNullOrEmpty(propertyName))
        {
            return source;
        }

        // Create a parameter to pass into the Lambda expression
        var parameter = Expression.Parameter(typeof(T), "Entity");

        //  create the selector part, but support child properties (it works without . too)
        var childProperties = propertyName.Split('.');
        var property = Expression.Property(parameter, childProperties[0]);
        for (var i = 1; i < childProperties.Length; i++)
        {
            property = Expression.Property(property, childProperties[i]);
        }

        var selector = Expression.Lambda(property, parameter);

        var methodName = direction == SortDirection.Ascending ? "OrderBy" : "OrderByDescending";

        var resultExp = Expression.Call(typeof(Queryable), methodName,
            new Type[] { source.ElementType, property.Type },
            source.Expression, Expression.Quote(selector));

        return source.Provider.CreateQuery<T>(resultExp);
    }
}
