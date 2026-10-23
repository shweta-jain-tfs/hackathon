using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.Reagents.ViewModels;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.Models;
using Pharos.Common.Web.Components.Drawer;

namespace Pharos.Client.App.Reagents.Views.Drawer;
public partial class ReagentDrawerView : IDisposable
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;

    [Parameter] public ReagentDrawerViewModel ViewModel { get; set; } = default!;

    [Parameter] public EventCallback LoaderSelected { get; set; }

    private int? _selectedLoaderPosition;

    public void Dispose()
    {
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        ViewModel.Dispose();
        GC.SuppressFinalize(this);
    }

    protected override void OnInitialized()
    {
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        StateHasChanged();
    }


    private void LoaderClicked(ReagentLoader loader)
    {
        _selectedLoaderPosition = loader.Position;
        ViewModel.LoaderPanelViewModel.SelectedReagentLoader = loader;
        LoaderSelected.InvokeAsync();
    }

    private string GetDrawerStateText(PhDrawerLockState drawerState)
    {
        return drawerState switch
        {
            PhDrawerLockState.Open => Loc["ReagentPage_DrawerOpen"],
            PhDrawerLockState.UnlockInProgress => Loc["ReagentPage_UnlockingDrawer"],
            PhDrawerLockState.ClosedUnlocked => Loc["ReagentPage_DrawerUnlocked"],
            _ => string.Empty
        };
    }
}
