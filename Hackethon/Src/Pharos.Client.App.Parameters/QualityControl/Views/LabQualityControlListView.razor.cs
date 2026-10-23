using Pharos.Client.App.Parameters.QualityControl.ViewModels;
using Pharos.Common.Web.Components.DataGrid;

namespace Pharos.Client.App.Parameters.QualityControl.Views;

public partial class LabQualityControlListView
{
    private PhDataGrid<LabQualityControl>? _dataGrid;

    private Dictionary<string, string> _methodNameDict = [];
    private HashSet<LabQualityControl> _selectedItems = new();

    [Inject] public LabQualityControlListViewModel ViewModel { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        _methodNameDict = await ViewModel.GetMethodNames();
    }

    private void Create()
    {
        NavigationManager.NavigateTo(PageRoute.Parameters.LabQualityControlDetails.Create);
    }

    private async Task DeleteSelectedItems()
    {
        if (_selectedItems.Count == 0)
        {
            return;
        }

        await ViewModel.DeleteLabQualityControls(_selectedItems);
        _selectedItems.Clear();
        _dataGrid?.ClearSelectedItem();
    }

    private void SelectedItemsChanged(HashSet<LabQualityControl> selectedItems)
    {
        _selectedItems = selectedItems;
    }
}