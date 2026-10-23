using Pharos.Common.Web.Components.DataGrid.Filters.Models;

namespace Pharos.Common.Web.Components.QuickFilter;

public partial class PhQuickMultiSelectFilter<TValue>
{
    [Parameter, EditorRequired]
    public MultiSelectQuickFilter<TValue> ColumnFilter { get; set; } = default!;

    [Parameter]
    public EventCallback OnValueChanged { get; set; }

    private async Task SelectionChanged(SelectableQuickFilterValue<TValue> selectable, bool isChecked)
    {
        if (selectable.IsSelected == isChecked)
        {
            return;
        }

        selectable.IsSelected = isChecked;
        await OnValueChanged.InvokeAsync();
    }

    private async Task ClearFilter()
    {
        ColumnFilter.Clear();
        await OnValueChanged.InvokeAsync();
    }
}
