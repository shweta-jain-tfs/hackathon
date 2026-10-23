using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.Reagents.ViewModels.Inventory;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.Models;
using Pharos.Common.Web.Components.DataGrid;
using Pharos.Common.Web.Components.Modal;
using Pharos.Contracts.Security;

namespace Pharos.Client.App.Reagents.Views.Inventory.DilutionPlates
{
    public partial class InternalDilutionPlatesListView : IDisposable
    {
        [Inject] public IAuthorizationService AuthorizationService { get; set; } = null!;
        [Parameter] public InternalDilutionPlatesViewModel ViewModel { get; set; } = default!;
        [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
        [Inject] private IModalService ModalService { get; set; } = default!;

        private PhDataGrid<InternalDilutionPlate>? _dataGrid;
        private bool _isAuthorized;

        private int SelectedCount => ViewModel.GetCurrentItemSelection().Count;
        private bool IsUnloadDisabled => SelectedCount == 0;

        protected override async Task OnInitializedAsync()
        {
            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
            await ViewModel.LoadAsync();
            _isAuthorized = await IsAuthorized();
        }

        private async void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.SelectedInternalDilutionPlates))
            {
                if (ViewModel.SelectedInternalDilutionPlates?.Count == 0 && _dataGrid != null)
                {
                    await _dataGrid.ClearSelectedItem(new List<InternalDilutionPlate>());
                }
            }
            StateHasChanged();
        }

        private string GetTitle()
        {
            var platesCount = string.Concat(Loc["InventoryPage_DilutionPlatesTab_InternalListTitle"], " (", ViewModel.DilutionPlatesCount, ") ");
            var wellsCount = string.Concat(Loc["InventoryPage_DilutionPlatesTab_Wells"], " (", ViewModel.FreeWellsCount, ") ");
            return string.Concat(platesCount, wellsCount);
        }

        private async Task<bool> IsAuthorized()
        {
            return await AuthorizationService.IsAuthorized(Permissions.ManageReagents);
        }

        private async Task OnUnloadClicked()
        {
            var options = new ModalOptions
            {
                Title = Loc["InventoryPage_DilutionPlatesTab_UnloadTitle"],
                ModalHeight = ModalSize.Small,
                ModalWidth = ModalSize.Small,
                Status = ModalStatus.Default
            };
            var message = Loc["InventoryPage_DilutionPlatesTab_UnloadMessage"];
            var cancelButtonText = Loc["InventoryPage_CancelButton"];
            var okButtonText = Loc["InventoryPage_UnloadButtonText"];
            var modal = ModalService.ShowConfirm(message, cancelButtonText, okButtonText, options, true);
            var result = await modal.Result;

            if (!result.Canceled)
            {
                var unloadableDilutionPlateIds = ViewModel.GetCurrentItemSelection().Select(x => x.Id).ToHashSet();
                await ViewModel.UnloadSelectedDilutionPlatesAsync(unloadableDilutionPlateIds);
                await RemoveDilutionPlatesSelection(unloadableDilutionPlateIds);
            }
            return;
        }

        private async Task RemoveDilutionPlatesSelection(HashSet<int> unloadDilutionPlateIds)
        {
            // Get the dilution plates that should remain selected (all except the ones being unloaded)
            var remainingDilutionPlates = ViewModel.SelectedInternalDilutionPlates?
                .Where(p => !unloadDilutionPlateIds.Contains(p.Id))
                .ToList();

            // Use the DataGrid's ClearSelectedItem method to properly update both UI and ViewModel
            if (_dataGrid != null)
            {
                await _dataGrid.ClearSelectedItem(remainingDilutionPlates);
            }
        }

        public void Dispose()
        {
            ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            ViewModel.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}