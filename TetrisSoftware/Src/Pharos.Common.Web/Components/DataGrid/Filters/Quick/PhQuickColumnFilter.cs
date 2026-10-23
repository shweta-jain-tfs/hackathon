using Pharos.Common.Web.Components.DataGrid.Filters.Models;

namespace Pharos.Common.Web.Components.DataGrid.Filters.Quick;

public class PhQuickColumnFilter<TColumnFilter, TGridItem, TProp> : ComponentBase where TColumnFilter : ColumnFilterBase<TGridItem, TProp>
{
    [Parameter] public TColumnFilter ColumnFilter { get; set; } = default!;

    [Parameter] public EventCallback OnValueChanged { get; set; }

    protected async Task ClearFilter()
    {
        if (ColumnFilter.ActiveFilterCount > 0)
        {
            ColumnFilter.Clear();
            await OnValueChanged.InvokeAsync();
        }

        ResetLocalState();
    }

    /// <summary>
    /// Override to reset local UI state (e.g. uncommitted input) when the filter is cleared.
    /// </summary>
    protected virtual void ResetLocalState() { }
}