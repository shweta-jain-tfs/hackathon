using Pharos.Common.Web.Components.DataGrid.Filters.Models;

namespace Pharos.Common.Web.Components.DataGrid.Filters.Quick;

public partial class PhQuickKeyValueListCheckboxColumnFilter<TGridItem, TProp>
{
    private async Task Callback(bool args, SelectableKeyValue selected)
    {
        var test = ColumnFilter!.SelectableStringValues.First(x => x.Key == selected.Key);
        test.IsSelected = args;
        await OnValueChanged.InvokeAsync();
    }
}