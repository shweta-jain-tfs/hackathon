using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.Reagents.ViewModels.Inventory;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.Models;
using Pharos.Client.Common.ViewModels;
using Pharos.Common.Web.Components.DataGrid;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Reagents.Views.Inventory.Cartridges;

public partial class CartridgesListView : IDisposable
{
    [Inject] private CartridgesListViewModel ViewModel { get; set; } = default!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] private InstrumentFilterViewModel InstrumentFilterModel { get; set; } = default!;
    [Inject] private IModalService ModalService { get; set; } = default!;
    [Parameter] public bool DisplayErrorFilters { get; set; }

    private PhDataGrid<CartridgeListItem>? _dataGrid;

    private string FreePositionsFrontText => $"{ViewModel.FreePositionsFront} / {ViewModel.TotalPositionsFront}";
    private string FreePositionsBackText => $"{ViewModel.FreePositionsBack} / {ViewModel.TotalPositionsBack}";
    private bool IsAbortUnloadDisabled => !(ViewModel.GetCurrentItemSelection().Any(x => x.Status == ReagentPositionStatus.Unloading));
    private string? StatusFilterText => DisplayErrorFilters && Loc != null && Loc["InventoryPage_ErrorFilter"] != null
        ? Loc["InventoryPage_ErrorFilter"].Value
        : null;

    protected override async Task OnInitializedAsync()
    {
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        ViewModel.GridRefreshRequested += OnGridRefreshRequested;
        await ViewModel.LoadAsync();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ViewModel.FreePositionsFront)
            or nameof(ViewModel.FreePositionsBack)
            or nameof(ViewModel.TotalCount)
            or nameof(ViewModel.TotalQuantity)
            or nameof(ViewModel.TotalPositionsFront)
            or nameof(ViewModel.TotalPositionsBack)
            or nameof(ViewModel.CanUnloadSelectedCartridges))
        {
            InvokeAsync(StateHasChanged);
        }
    }

    private string GetTitle()
    {
        var totalCountText = string.Concat(Loc["InventoryPage_CartridgesTab_Title"], " (", ViewModel.TotalCount, ") ");

        var totalQuantityText = ViewModel.ShouldDisplayUnits() ? string.Concat(ViewModel.GetUnitsDisplayText(), " (", ViewModel.TotalQuantity, ")") : string.Empty;

        return string.Concat(totalCountText, totalQuantityText);
    }

    private Dictionary<string, string> StatusNameDict()
    {
        return new Dictionary<string, string> {
            { ReagentPositionStatus.Reserved.ToString(), ReagentPositionStatus.Reserved.ToString() },
            { ReagentPositionStatus.Unloading.ToString(), ReagentPositionStatus.Unloading.ToString() },
            { ReagentPositionStatus.Empty.ToString(), ReagentPositionStatus.Empty.ToString() },
            { Loc["InventoryPage_ErrorFilter"] , Loc["InventoryPage_ErrorFilter"] }
        };
    }
    private void OnGridRefreshRequested()
    {
        StateHasChanged();
    }

    private async Task OnUnloadClicked()
    {
        var options = new ModalOptions
        {
            Title = Loc["InventoryPage_CartridgesTab_UnloadTitle"],
            ModalHeight = ModalSize.Small,
            ModalWidth = ModalSize.Small,
            Status = ModalStatus.Default
        };
        var message = Loc["InventoryPage_CartridgesTab_UnloadMessage"];
        var cancelButtonText = Loc["InventoryPage_CancelButton"];
        var okButtonText = Loc["InventoryPage_UnloadButtonText"];
        var modal = ModalService.ShowConfirm(message, cancelButtonText, okButtonText, options, true);
        var result = await modal.Result;

        if (!result.Canceled)
        {
            var unloadableCartridgeIds = ViewModel.GetCurrentItemSelection()
            .Where(x => !CartridgesListViewModel.IsInUnloadProhibitedState(x.Status)).Select(x => x.Id).ToHashSet();
            await ViewModel.UnloadSelectedCartridgesAsync(unloadableCartridgeIds);
            await RemoveCartridgeSelection(unloadableCartridgeIds);
            var notPossibleToUnloadCartridges = ViewModel.GetCurrentItemSelection()
                .Where(x => CartridgesListViewModel.IsInUnloadProhibitedState(x.Status)).Select(x => x.Id).ToHashSet();
            if (notPossibleToUnloadCartridges.Count > 0)
            {
                var opt = new ModalOptions
                {
                    Title = Loc["InventoryPage_CartridgesTab_UnloadNotPossibleTitle"],
                    ModalHeight = ModalSize.Small,
                    ModalWidth = ModalSize.Small,
                    Status = ModalStatus.Info
                };
                var notPossibleMessage = Loc["InventoryPage_CartridgesTab_UnloadNotPossibleMessage"];
                var closeButtonText = Loc["InventoryPage_CloseButtonText"];
                ModalService.ShowInformation(notPossibleMessage, closeButtonText, opt, true);
            }
        }
    }

    private async Task RemoveCartridgeSelection(HashSet<int> unloadCartridgesIds)
    {
        // Get the cartridges that should remain selected (all except the ones being unloaded)
        var remainingCartridges = ViewModel.SelectedCartridges?
            .Where(c => !unloadCartridgesIds.Contains(c.Id))
            .ToList();

        // Use the DataGrid's ClearSelectedItem method to properly update both UI and ViewModel
        if (_dataGrid != null)
        {
            await _dataGrid.ClearSelectedItem(remainingCartridges);
        }
    }

    private async Task OnAbortClicked()
    {
        var options = new ModalOptions
        {
            Title = Loc["InventoryPage_CartridgesTab_AbortTitle"],
            ModalHeight = ModalSize.Small,
            ModalWidth = ModalSize.Small,
            Status = ModalStatus.Default
        };
        var message = Loc["InventoryPage_CartridgesTab_AbortMessage"];
        var cancelButtonText = Loc["InventoryPage_CancelButton"];
        var okButtonText = Loc["InventoryPage_AbortButtonText"];
        var modal = ModalService.ShowConfirm(message, cancelButtonText, okButtonText, options, true);
        var result = await modal.Result;
        if (!result.Canceled)
        {
            var abortCartridgeIds = ViewModel.GetCurrentItemSelection()
            .Where(x => x.Status == ReagentPositionStatus.Unloading).Select(x => x.Id).ToHashSet();
            await ViewModel.AbortUnloadCartridgesAsync(abortCartridgeIds);
            await RemoveCartridgeSelection(abortCartridgeIds);
            var notPossibleToAbortCartridges = ViewModel.GetCurrentItemSelection()
                .Where(x => x.Status != ReagentPositionStatus.Unloading).Select(x => x.Id).ToHashSet();
            if (notPossibleToAbortCartridges.Count > 0)
            {
                var opt = new ModalOptions
                {
                    Title = Loc["InventoryPage_CartridgesTab_AbortNotPossibleTitle"],
                    ModalHeight = ModalSize.Small,
                    ModalWidth = ModalSize.Small,
                    Status = ModalStatus.Info
                };
                var notPossibleMessage = Loc["InventoryPage_CartridgesTab_AbortNotPossibleMessage"];
                var closeButtonText = Loc["InventoryPage_CloseButtonText"];
                ModalService.ShowInformation(notPossibleMessage, closeButtonText, opt, true);
            }
        }
    }

    public void Dispose()
    {
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        ViewModel.GridRefreshRequested -= OnGridRefreshRequested;
        ViewModel.Dispose();
        GC.SuppressFinalize(this);
    }
}


