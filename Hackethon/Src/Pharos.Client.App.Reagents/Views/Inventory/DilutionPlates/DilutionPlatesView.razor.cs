using Microsoft.AspNetCore.Components;
using Pharos.Client.App.Reagents.ViewModels.Inventory;

namespace Pharos.Client.App.Reagents.Views.Inventory.DilutionPlates
{
    public partial class DilutionPlatesView
    {
        [Inject]
        public DilutionPlatesViewModel ViewModel { get; set; } = default!;

        protected override async Task OnInitializedAsync()
        {
            // Initialize the parent view model which will set up the default filter
            await ViewModel.InitializeAsync();
        }
    }
}
