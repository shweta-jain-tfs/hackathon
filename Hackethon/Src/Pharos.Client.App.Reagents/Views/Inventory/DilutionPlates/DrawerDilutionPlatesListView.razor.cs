using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.Reagents.ViewModels.Inventory;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.Models;
using Pharos.Client.Common.ViewModels;

namespace Pharos.Client.App.Reagents.Views.Inventory.DilutionPlates
{
    public partial class DrawerDilutionPlatesListView : IDisposable
    {
        [Parameter] public DrawerDilutionPlatesViewModel ViewModel { get; set; } = default!;
        [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
        [Inject] private InstrumentFilterViewModel InstrumentFilterModel { get; set; } = default!;

        protected override async Task OnInitializedAsync()
        {
            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
            await ViewModel.LoadAsync();
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            StateHasChanged();
        }

        private string GetTitle()
        {
            var platesCount = string.Concat(Loc["InventoryPage_DilutionPlatesTab_ListTitle"], " (", ViewModel.DilutionPlatesCount, ") ");
            var wellsCount = string.Concat(Loc["InventoryPage_DilutionPlatesTab_Wells"], " (", ViewModel.DilutionPlatesWellsCount, ") ");
            return string.Concat(platesCount, wellsCount);
        }

        private string GetDilutionPlates(int numberOfPlates, ReagentPositionStatus status)
        {
            if (DrawerDilutionPlatesViewModel.HasStatusError(status) && numberOfPlates == 0)
            {
                return Loc["InventoryPage_DilutionPlatesTab_ZeroPlatesWithError"];
            }

            return numberOfPlates.ToString();
        }

        public void Dispose()
        {
            ViewModel.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}