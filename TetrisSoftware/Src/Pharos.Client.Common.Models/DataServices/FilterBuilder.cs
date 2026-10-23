using System.Linq.Expressions;

namespace Pharos.Client.Common.Models.DataServices;

public static class FilterBuilder
{
    public static Expression<Func<T, bool>> True<T>()
    {
        return _ => true;
    }
    
    public static Expression<Func<T, bool>> And<T>(
        this Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right)
    {
        var p = left.Parameters[0];
        var body = Expression.AndAlso(
            left.Body,
            new ReplaceParameterVisitor(right.Parameters[0], p).Visit(right.Body));
        return Expression.Lambda<Func<T, bool>>(body, p);
    }

    //public static Expression<Func<T, bool>> Or<T>(
    //    this Expression<Func<T, bool>> left,
    //    Expression<Func<T, bool>> right)
    //{
    //    var p = left.Parameters[0];
    //    var body = Expression.OrElse(
    //        left.Body,
    //        new ReplaceParameterVisitor(right.Parameters[0], p).Visit(right.Body));
    //    return Expression.Lambda<Func<T, bool>>(body, p);
    //}

    private sealed class ReplaceParameterVisitor(ParameterExpression from, ParameterExpression to) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == from ? to : base.VisitParameter(node);
        }
    }

    public static bool? GetBoolean(ICollection<DataSourceFilter> filters, string propertyName)
    {
        var filter = filters.FirstOrDefault(fd => fd.PropertyName == propertyName && fd.FilterOperator == FilterOperator.Contains);
        return filter?.FilterValue as bool?;
    }

    public static (DateTimeOffset? FromDate, DateTimeOffset? ToDate) GetToAndFromDate(ICollection<DataSourceFilter> filters, string propertyName)
    {
        var fromDate = filters.FirstOrDefault(fd => fd.PropertyName == propertyName && fd.FilterOperator == FilterOperator.GreaterThanOrEqualTo);
        var toDate = filters.FirstOrDefault(fd => fd.PropertyName == propertyName && fd.FilterOperator == FilterOperator.LessThanOrEqualTo);

        return (fromDate?.FilterValue as DateTimeOffset?, toDate?.FilterValue as DateTimeOffset?);
    }

    public static List<string?> GetList(ICollection<DataSourceFilter> filters, string propertyName)
    {
        return filters.Where(f => f.PropertyName == propertyName).Select(f => f.FilterValue as string).ToList() ?? [];
    }
}