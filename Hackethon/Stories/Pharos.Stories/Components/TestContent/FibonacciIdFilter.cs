using Pharos.Common.Web.Components.DataGrid.Filters.Models;
using Pharos.Common.Web.Models.DataGrid;

namespace Pharos.Stories.Components.TestContent;

/// <summary>
/// Custom quick filter model used in stories. Filters the Id column on a single selected Fibonacci number.
/// </summary>
public sealed class FibonacciIdFilter : IColumnFilter
{
    public static readonly IReadOnlyList<int> FibonacciNumbers = CreateFibonacciNumbers(10);

    public FibonacciIdFilter(string propertyName)
    {
        PropertyName = propertyName;
    }

    public string PropertyName { get; }

    public LogicalFilterOperator LogicalOperator => LogicalFilterOperator.And;

    public int? SelectedValue { get; set; }

    public IEnumerable<PropertyFilterExpression> FilterExpressions =>
        SelectedValue is { } value
            ? [new PropertyFilterExpression<int>(PropertyName, PropertyFilterOperator.EqualsTo, value)]
            : [];

    public event EventHandler? OnClear;

    public void Clear()
    {
        SelectedValue = null;
        OnClearInvoked();
    }

    public void ResetToDefault() => Clear();

    public Task LoadAsync() => Task.CompletedTask;

    public void OnClearInvoked() => OnClear?.Invoke(this, EventArgs.Empty);

    public void ApplyState(ColumnFilterState? stateObject)
    {
        if (stateObject is EnumFilterState state)
        {
            SelectedValue = state.SelectedValues.Count > 0 ? state.SelectedValues[0] : null;
        }
    }

    public ColumnFilterState SaveState()
    {
        if (SelectedValue is not { } value)
        {
            return new EnumFilterState();
        }

        return new EnumFilterState
        {
            PropertyName = PropertyName,
            SelectedValues = [value]
        };
    }

    private static List<int> CreateFibonacciNumbers(int count)
    {
        var numbers = new List<int>(count);
        int current = 0, next = 1;

        for (var i = 0; i < count; i++)
        {
            numbers.Add(current);
            (current, next) = (next, current + next);
        }

        return numbers;
    }
}
