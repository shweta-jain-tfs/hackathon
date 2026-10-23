using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.Reagents.ViewModels.Inventory;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.Models;
using Pharos.Client.Common.ViewModels;
using Pharos.Common.Web.Components.DataGrid;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Reagents.Views.Inventory.Carriers;
public partial class CarrierListView : IDisposable
{
    [Inject] private CarriersListViewModel ViewModel { get; set; } = default!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] private InstrumentFilterViewModel InstrumentFilterModel { get; set; } = default!;
    [Inject] private IModalService ModalService { get; set; } = default!;
    [Parameter] public bool DisplayErrorFilters { get; set; }

    private PhDataGrid<CarrierListItem>? _dataGrid;
    private string FreePositionsText => $"{ViewModel.FreePositions} / {ViewModel.TotalPositions}";
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
        if (e.PropertyName == nameof(ViewModel.FreePositions) ||
            e.PropertyName == nameof(ViewModel.TotalCount) ||
            e.PropertyName == nameof(ViewModel.TotalQuantity) ||
            e.PropertyName == nameof(ViewModel.TotalPositions) ||
            e.PropertyName == nameof(ViewModel.CanUnloadSelectedCarriers))
        {
            StateHasChanged();
        }
    }

    private string GetTitle()
    {
        return string.Concat(Loc["InventoryPage_CarriersTab_Title"], " (", ViewModel.TotalCount, ") ", "Tests (", ViewModel.TotalQuantity, ")");
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

    private string GetRefillLevelStatusText(bool isRefillLevelDefined)
    {
        return isRefillLevelDefined
            ? Loc["InventoryPage_CarriersTab_RefillLevelDefined"]
            : Loc["InventoryPage_CarriersTab_RefillLevelNotDefined"];
    }

    private void OnGridRefreshRequested()
    {
        StateHasChanged();
    }

    private async Task OnUnloadClicked()
    {
        var options = new ModalOptions
        {
            Title = Loc["InventoryPage_CarriersTab_UnloadTitle"],
            ModalHeight = ModalSize.Small,
            ModalWidth = ModalSize.Small,
            Status = ModalStatus.Default
        };
        var message = Loc["InventoryPage_CarriersTab_UnloadMessage"];
        var cancelButtonText = Loc["InventoryPage_CancelButton"];
        var okButtonText = Loc["InventoryPage_UnloadButtonText"];
        var modal = ModalService.ShowConfirm(message, cancelButtonText, okButtonText, options, true);
        var result = await modal.Result;

        if (!result.Canceled)
        {
            var unloadableCarrierIds = ViewModel.GetCurrentItemSelection()
            .Where(x => !CarriersListViewModel.IsInUnloadProhibitedState(x.Status)).Select(x => x.Id).ToHashSet();
            await ViewModel.UnloadSelectedCarriersAsync(unloadableCarrierIds);
            await RemoveCarrierSelection(unloadableCarrierIds);
            var notPossibleToUnloadCarriers = ViewModel.GetCurrentItemSelection()
                .Where(x => CarriersListViewModel.IsInUnloadProhibitedState(x.Status)).Select(x => x.Id).ToHashSet();
            if (notPossibleToUnloadCarriers.Count > 0)
            {
                var opt = new ModalOptions
                {
                    Title = Loc["InventoryPage_CarriersTab_UnloadNotPossibleTitle"],
                    ModalHeight = ModalSize.Small,
                    ModalWidth = ModalSize.Small,
                    Status = ModalStatus.Info
                };
                var notPossibleMessage = Loc["InventoryPage_CarriersTab_UnloadNotPossibleMessage"];
                var closeButtonText = Loc["InventoryPage_CloseButtonText"];
                ModalService.ShowInformation(notPossibleMessage, closeButtonText, opt, true);
            }
        }
    }

    private async Task RemoveCarrierSelection(HashSet<int> unloadCarriersIds)
    {
        // Get the carriers that should remain selected (all except the ones being unloaded)
        var remainingCarriers = ViewModel.SelectedCarriers?
            .Where(c => !unloadCarriersIds.Contains(c.Id))
            .ToList();

        // Use the DataGrid's ClearSelectedItem method to properly update both UI and ViewModel
        if (_dataGrid != null)
        {
            await _dataGrid.ClearSelectedItem(remainingCarriers);
        }
    }

    private async Task OnAbortClicked()
    {
        var options = new ModalOptions
        {
            Title = Loc["InventoryPage_CarriersTab_AbortTitle"],
            ModalHeight = ModalSize.Small,
            ModalWidth = ModalSize.Small,
            Status = ModalStatus.Default
        };
        var message = Loc["InventoryPage_CarriersTab_AbortMessage"];
        var cancelButtonText = Loc["InventoryPage_CancelButton"];
        var okButtonText = Loc["InventoryPage_AbortButtonText"];
        var modal = ModalService.ShowConfirm(message, cancelButtonText, okButtonText, options, true);
        var result = await modal.Result;
        if (!result.Canceled)
        {
            var abortCarriersIds = SelectCarriersForAbort();
            await ViewModel.AbortUnloadCarriersAsync(abortCarriersIds);
            await RemoveCarrierSelection(abortCarriersIds);
            var notPossibleToAbortCarriers = ViewModel.GetCurrentItemSelection()
                .Where(x => x.Status != ReagentPositionStatus.Unloading).Select(x => x.Id).ToHashSet();
            if (notPossibleToAbortCarriers.Count > 0)
            {
                var opt = new ModalOptions
                {
                    Title = Loc["InventoryPage_CarriersTab_AbortNotPossibleTitle"],
                    ModalHeight = ModalSize.Small,
                    ModalWidth = ModalSize.Small,
                    Status = ModalStatus.Info
                };
                var notPossibleMessage = Loc["InventoryPage_CarriersTab_AbortNotPossibleMessage"];
                var closeButtonText = Loc["InventoryPage_CloseButtonText"];
                ModalService.ShowInformation(notPossibleMessage, closeButtonText, opt, true);
            }
        }
    }

    private HashSet<int> SelectCarriersForAbort()
    {
        return ViewModel.GetCurrentItemSelection()
            .Where(x => x.Status == ReagentPositionStatus.Unloading)
            .Select(x => x.Id)
            .ToHashSet();
    }

    public void Dispose()
    {
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        ViewModel.GridRefreshRequested -= OnGridRefreshRequested;
        ViewModel.Dispose();
        GC.SuppressFinalize(this);
    }
}
