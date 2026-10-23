using Pharos.Common.Web.Components.DataGrid.Filters.Models;
using Pharos.Common.Web.Models.DataGrid;

namespace Pharos.Stories.Components.TestContent;

/// <summary>
/// Custom quick filter model used in stories. Filters the Id column on multiple selected Fibonacci numbers.
/// </summary>
public sealed class FibonacciIdsFilter : IColumnFilter
{
    public FibonacciIdsFilter(string propertyName)
    {
        PropertyName = propertyName;
    }

    public string PropertyName { get; }

    public LogicalFilterOperator LogicalOperator => LogicalFilterOperator.Or;

    public HashSet<int> SelectedValues { get; } = [];

    public IEnumerable<PropertyFilterExpression> FilterExpressions =>
        SelectedValues.Select(value => new PropertyFilterExpression<int>(PropertyName, PropertyFilterOperator.EqualsTo, value));

    public event EventHandler? OnClear;

    public void Clear()
    {
        SelectedValues.Clear();
        OnClearInvoked();
    }

    public void ResetToDefault() => Clear();

    public Task LoadAsync() => Task.CompletedTask;

    public void OnClearInvoked() => OnClear?.Invoke(this, EventArgs.Empty);

    public void ApplyState(ColumnFilterState? stateObject)
    {
        if (stateObject is EnumFilterState state)
        {
            SelectedValues.Clear();
            SelectedValues.UnionWith(state.SelectedValues);
        }
    }

    public ColumnFilterState SaveState() => new EnumFilterState
    {
        PropertyName = PropertyName,
        SelectedValues = SelectedValues.ToList()
    };
}
