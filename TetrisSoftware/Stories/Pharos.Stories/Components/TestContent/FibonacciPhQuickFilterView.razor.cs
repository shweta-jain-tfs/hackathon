using Microsoft.AspNetCore.Components;
namespace Pharos.Stories.Components.TestContent;

/// <summary>
/// Custom quick filter component used in stories. Renders Fibonacci checkboxes in a PhQuickFilter popover.
/// </summary>
public partial class FibonacciPhQuickFilterView
{
    [Parameter] public FibonacciIdsFilter ColumnFilter { get; set; } = default!;

    [Parameter] public EventCallback OnValueChanged { get; set; }

    private static readonly List<int> _numbers = FibonacciIdFilter.FibonacciNumbers.Distinct().ToList();

    private int ActiveFilterCount => ColumnFilter.SelectedValues.Count;

    private async Task SelectionChanged(int number, bool isChecked)
    {
        if (isChecked)
        {
            ColumnFilter.SelectedValues.Add(number);
        }
        else
        {
            ColumnFilter.SelectedValues.Remove(number);
        }

        await OnValueChanged.InvokeAsync();
    }

    private async Task ClearFilter()
    {
        ColumnFilter.Clear();
        await OnValueChanged.InvokeAsync();
    }
}
