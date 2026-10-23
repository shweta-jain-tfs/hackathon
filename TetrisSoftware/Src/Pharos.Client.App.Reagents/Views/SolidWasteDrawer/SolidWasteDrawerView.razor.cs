using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.Reagents.ViewModels.SolidWasteDrawer;
using Pharos.Client.Common.Localization;
using Pharos.Common.Web.Components.Drawer;
using Pharos.Common.Web.Utilities;

namespace Pharos.Client.App.Reagents.Views.SolidWasteDrawer;

public partial class SolidWasteDrawerView : ComponentBase, IDisposable
{
    [Parameter] public SolidWasteDrawerViewModel ViewModel { get; set; } = default!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;

    private string WasteDrawerCss => new CssBuilder("ph-solid-waste-drawer-view__drawer")
        .AddClass("ph-solid-waste-drawer-view__drawer--open", ViewModel?.PhDrawerLockState == PhDrawerLockState.Open)
        .Build();

    protected override void OnInitialized()
    {
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        StateHasChanged();
    }

    private string GetDrawerStateText(PhDrawerLockState drawerState)
    {
        return drawerState switch
        {
            PhDrawerLockState.Open => Loc["Waste_DrawerOpen"],
            PhDrawerLockState.UnlockInProgress => Loc["Waste_UnlockingDrawer"],
            PhDrawerLockState.ClosedUnlocked => Loc["Waste_DrawerUnlocked"],
            _ => string.Empty
        };
    }

    public void Dispose()
    {
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        ViewModel.Dispose();
        GC.SuppressFinalize(this);
    }
}
