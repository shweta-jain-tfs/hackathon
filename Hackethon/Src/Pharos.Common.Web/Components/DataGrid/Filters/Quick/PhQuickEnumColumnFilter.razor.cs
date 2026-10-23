using Pharos.Common.Web.Components.DataGrid.Filters.Models;

namespace Pharos.Common.Web.Components.DataGrid.Filters.Quick;

public partial class PhQuickEnumColumnFilter<TGridItem, TProp>
{
    private async Task SelectedEnumOnOnSelectionChanged(SelectableEnum selectableEnum, bool args)
    {
        var enumValue = ColumnFilter.SelectableValues!.First(x => x.Value == selectableEnum.Value);
        enumValue.IsSelected = args;
        await OnValueChanged.InvokeAsync();
    }
}