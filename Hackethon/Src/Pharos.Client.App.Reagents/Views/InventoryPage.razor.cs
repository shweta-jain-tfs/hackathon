using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.ViewModels;

namespace Pharos.Client.App.Reagents.Views
{
    public partial class InventoryPage : ComponentBase
    {
        [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;
        [Inject] private InstrumentFilterViewModel InstrumentFilterModel { get; set; } = default!;

        protected int? DefaultTab { get; set; }
        private bool _applyErrorFilterForCarriers;
        private bool _applyErrorFilterForCartridges;

        protected override void OnInitialized()
        {
            SetDefaultTabFromQuery();
        }
        
        protected override async Task OnInitializedAsync()
        {
            await InstrumentFilterModel.LoadAsync();
        }

        private void SetDefaultTabFromQuery()
        {
            var uri = Navigation.ToAbsoluteUri(Navigation.Uri);
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            var tab = query["tab"];

            DefaultTab = tab switch
            {
                "carriers" => (_applyErrorFilterForCarriers = true, 1).Item2,
                "cartridges" => (_applyErrorFilterForCartridges = true, 2).Item2,
                _ => DefaultTab
            };
        }
    }
}
