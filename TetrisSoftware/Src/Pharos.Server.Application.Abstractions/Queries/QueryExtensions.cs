namespace Pharos.Server.Application.Queries;

public static class QueryExtensions
{
    public static IOrderedQueryable<TSource> OrderBy<TSource>(this IQueryable<TSource> source, string orderByProperty, bool orderByAscending)
    {
        var prop = typeof(TSource).GetProperty(orderByProperty)!;

        return orderByAscending
            ? source.OrderBy(x => prop.GetValue(x, null))
            : source.OrderByDescending(x => prop.GetValue(x, null));
    }
}
