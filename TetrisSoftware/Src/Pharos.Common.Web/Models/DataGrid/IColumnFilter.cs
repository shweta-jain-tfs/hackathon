using Pharos.Common.Web.Components.DataGrid.Filters.Models;

namespace Pharos.Common.Web.Models.DataGrid;

public enum LogicalFilterOperator
{
    And,
    Or
}

/// <summary>
/// Defines a model for a column filter.
/// </summary>
public interface IColumnFilter : IColumnFilterBase
{
    public event EventHandler OnClear;
    void Clear();
    void ResetToDefault();

    Task LoadAsync();
    void OnClearInvoked();
    void ApplyState(ColumnFilterState? stateObject);
    ColumnFilterState SaveState();
}

public interface IColumnFilterBase
{
    /// <summary>
    /// The model property name for a column filter, or a unique logical key for a custom filter
    /// interpreted by the data source. Also used to identify saved filter state.
    /// </summary>
    string PropertyName { get; }

    LogicalFilterOperator LogicalOperator { get; }

    IEnumerable<PropertyFilterExpression> FilterExpressions { get; }
}
