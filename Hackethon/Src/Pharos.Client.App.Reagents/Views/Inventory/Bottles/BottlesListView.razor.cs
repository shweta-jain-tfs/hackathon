using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.Reagents.ViewModels.Inventory;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.Models;
using Pharos.Client.Common.ViewModels;

namespace Pharos.Client.App.Reagents.Views.Inventory.Bottles
{
    public partial class BottlesListView : IDisposable
    {
        [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
        [Inject] private BottlesListViewModel ViewModel { get; set; } = default!;
        [Inject] private InstrumentFilterViewModel InstrumentFilterModel { get; set; } = default!;
        private string FreePositionsText => $"{ViewModel.FreePositions} / {ViewModel.TotalPositions}";

        protected override async Task OnInitializedAsync()
        {
            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
            await ViewModel.LoadAsync();
        }
        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
           StateHasChanged();
        }

        private Dictionary<string, string> StatusNameDict()
        {
            return new Dictionary<string, string> {
            { ReagentPositionStatus.Reserved.ToString(), ReagentPositionStatus.Reserved.ToString() },
            { ReagentPositionStatus.Empty.ToString(), ReagentPositionStatus.Empty.ToString() },
            { Loc["InventoryPage_ErrorFilter"] , Loc["InventoryPage_ErrorFilter"] }};
        }

        public void Dispose()
        {
            ViewModel.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}