using Pharos.Common.Web.Models.DataGrid;

namespace Pharos.Common.Web.Components.DataGrid.Filters.Models;
public class FieldExpression : IColumnFilterBase
{
    public string PropertyName { get; }
    public LogicalFilterOperator LogicalOperator { get; }
    public IEnumerable<PropertyFilterExpression> FilterExpressions { get; }

    public FieldExpression(string propName, PropertyFilterOperator propertyFilterOperator, object? value, LogicalFilterOperator logicalFilterOperator = LogicalFilterOperator.And)
    {
        PropertyName = propName;
        LogicalOperator = logicalFilterOperator;
        FilterExpressions = new List<PropertyFilterExpression>();
        FilterExpressions = SetPropertyFilterExpression(propName, propertyFilterOperator, value);
    }

    private static List<PropertyFilterExpression> SetPropertyFilterExpression(string propertyName, PropertyFilterOperator propertyFilterOperator, object? value)
    {
        var propFilterExpression = new List<PropertyFilterExpression>
        {
            new PropertyFilterExpression<object?>(propertyName, propertyFilterOperator, value)
        };

        return propFilterExpression;
    }
}