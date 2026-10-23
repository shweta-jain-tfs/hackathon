using Pharos.Client.Common.Models.DataServices;
using Pharos.Common.Web.Models.DataGrid;

namespace Pharos.Client.Common.ViewModels;

public static class GridItemsProviderRequestMapper
{
    public static DataSourceRequest ToDataSourceRequest<TModel>(this GridItemsProviderRequest<TModel> request, bool dataSourceSupportsRowLimitBypass)
    {
        if (request.RequiresRowLimitBypass && !dataSourceSupportsRowLimitBypass)
        {
            throw new InvalidOperationException(
                $"Select All requires server support to bypass the max-row limit in the query validator. " +
                $"The datasource has not confirmed this support. " +
                $"Add {nameof(ISupportsRowLimitBypass)} once the bypass is handled by the server validator, " +
                $"or disable select all for this grid.");
        }

        var filters = new List<DataSourceFilter>();
        if (request.ColumnFilters != null)
        {
            foreach (var f in request.ColumnFilters)
            {
                filters.AddRange(ColumnToDataSourceFilters(f));
            }
        }

        return new DataSourceRequest
        {
            StartIndex = request.StartIndex,
            Count = request.Count is > 0 ? request.Count.Value : 40,
            SortByProperty = request.SortByColumn,
            SortByAscending = request.SortByAscending,
            Filters = filters,
            RequiresRowLimitBypass = request.RequiresRowLimitBypass,
            CancellationToken = request.CancellationToken
        };
    }

    private static IEnumerable<DataSourceFilter> ColumnToDataSourceFilters(IColumnFilterBase filter)
    {
        if (filter.LogicalOperator == LogicalFilterOperator.And)
        {
            foreach (var expr in filter.FilterExpressions)
            {
                yield return new DataSourceFilter(expr.PropertyName, PropertyToDataSourceFilterOperator(expr.Operator), expr.UntypedValue);
            }
        }
        else if (filter.LogicalOperator == LogicalFilterOperator.Or)
        {
            if (filter.FilterExpressions.All(e => e.Operator == PropertyFilterOperator.EqualsTo))
            {
                foreach (var expr in filter.FilterExpressions)
                {
                    yield return new DataSourceFilter(expr.PropertyName, FilterOperator.EqualsTo, expr.UntypedValue);
                }
            }
            else
            {
                throw new NotSupportedException("You can combine logical OR operator only with equality expressions at the data source level.");
            }
        }
        else
        {
            throw new NotSupportedException($"Logical operator {filter.LogicalOperator} is not supported at the data source level.");
        }
    }

    private static FilterOperator PropertyToDataSourceFilterOperator(PropertyFilterOperator filterOperator)
    {
        return filterOperator switch
        {
            PropertyFilterOperator.Contains => FilterOperator.Contains,
            PropertyFilterOperator.EqualsTo => FilterOperator.EqualsTo,
            PropertyFilterOperator.GreaterThan => FilterOperator.GreaterThan,
            PropertyFilterOperator.GreaterThanOrEqualTo => FilterOperator.GreaterThanOrEqualTo,
            PropertyFilterOperator.LessThan => FilterOperator.LessThan,
            PropertyFilterOperator.LessThanOrEqualTo => FilterOperator.LessThanOrEqualTo,
            _ => throw new ArgumentOutOfRangeException(nameof(filterOperator))
        };
    }
}