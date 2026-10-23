using Microsoft.Extensions.Localization;
using Pharos.Client.App.Quality.ViewModels;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.ViewModels;
using Pharos.Contracts.Security;

namespace Pharos.Client.App.Quality.Views.QualityControls;

public partial class MethodQualityControlCartridgesTabContent
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] private InstrumentFilterViewModel InstrumentFilterModel { get; set; } = default!;
    [Inject] private IAuthorizationService AuthorizationService { get; set; } = default!;
    
    [Parameter] public required MethodQualityControlDetailsViewModel ViewModel { get; set; }

    private HashSet<UnloadableQualityControlCartridge> _selectedItems = new();

    private bool _hasUnloadPermission;

    private bool IsUnloadEnabled => _hasUnloadPermission && _selectedItems.Count > 0;

    protected override async Task OnInitializedAsync()
    {
        await InstrumentFilterModel.LoadAsync();

        _hasUnloadPermission = await AuthorizationService.IsAuthorized(Permissions.ManageReagents);
    }

    private void OnSelectedItemsChanged(HashSet<UnloadableQualityControlCartridge> selectedItems)
    {
        _selectedItems = selectedItems;
    }

    private async Task Unload()
    {
        await ViewModel.UnloadReagents(_selectedItems);
    }
    
    private static string GetCartridgePosition(UnloadableQualityControlCartridge context)
    {
        var localized = context.Area.ToLocalizedString("Reagent");
        return string.Concat(localized, "-", context.Position);
    }
}