namespace Pharos.Common.Web.Components.DataGrid.Filters.Quick;

public partial class PhQuickBoolColumnFilter<TGridItem>
{
    private async Task OnToggleChanged(bool value)
    {
        if (ColumnFilter.Value == value)
        {
            return;
        }

        ColumnFilter.Value = value;
        await OnValueChanged.InvokeAsync();
    }
}