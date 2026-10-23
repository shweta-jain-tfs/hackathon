using Microsoft.Extensions.Localization;
using Pharos.Client.App.Quality.ViewModels;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.Localization;
using Pharos.Common.Web.Components.DataGrid;
using Pharos.Contracts.Security;

namespace Pharos.Client.App.Quality.Views.QualityControls;

public partial class MethodQualityControlTubesTabContent
{
    private PhDataGrid<QualityControlTubeViewModel> _dataGrid = default!;

    private bool _hasUnloadPermission;

    private HashSet<QualityControlTubeViewModel> _selectedItems = new();

    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;

    [Inject] private IAuthorizationService AuthorizationService { get; set; } = default!;

    [Parameter] public required MethodQualityControlDetailsViewModel ViewModel { get; set; }

    private bool IsUnloadEnabled => _hasUnloadPermission && _selectedItems.Count > 0;

    protected override async Task OnInitializedAsync()
    {
        _hasUnloadPermission = await AuthorizationService.IsAuthorized(Permissions.ManageSamples);
    }

    private void OnSelectedItemsChanged(HashSet<QualityControlTubeViewModel> selectedItems)
    {
        _selectedItems = selectedItems;
    }

    private async Task Unload()
    {
        await ViewModel.UnloadTubes(_selectedItems);

        _selectedItems.Clear();
        await _dataGrid.ClearSelectedItem();
    }
}