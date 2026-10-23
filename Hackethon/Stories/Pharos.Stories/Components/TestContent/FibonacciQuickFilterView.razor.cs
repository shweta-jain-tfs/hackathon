using Microsoft.AspNetCore.Components;
using Pharos.Common.Web.Components.FormElements;

namespace Pharos.Stories.Components.TestContent;

/// <summary>
/// Custom quick filter component used in stories. Renders a dropdown with the first 10 Fibonacci numbers.
/// </summary>
public partial class FibonacciQuickFilterView
{
    [Parameter] public FibonacciIdFilter ColumnFilter { get; set; } = default!;

    [Parameter] public EventCallback OnValueChanged { get; set; }

    private static readonly List<DropDownOption<int?>> _options =
        new List<DropDownOption<int?>>
        {
            new() { Text = "All", Value = null }
        }
        .Concat(FibonacciIdFilter.FibonacciNumbers
            .Distinct()
            .Select(x => new DropDownOption<int?> { Text = x.ToString(), Value = x }))
        .ToList();

    private async Task SelectionChanged(int? value)
    {
        ColumnFilter.SelectedValue = value;
        await OnValueChanged.InvokeAsync();
    }
}