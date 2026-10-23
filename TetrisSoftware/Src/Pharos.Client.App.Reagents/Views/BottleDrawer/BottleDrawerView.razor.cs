using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.Reagents.ViewModels.BottleDrawer;
using Pharos.Client.Common.Localization;
using Pharos.Common.Web.Utilities;

namespace Pharos.Client.App.Reagents.Views.BottleDrawer;

public partial class BottleDrawerView : IDisposable
{
    [Parameter] public BottleDrawerViewModel ViewModel { get; set; } = default!;
    [Parameter] public EventCallback OnBottleSelected { get; set; }
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;


    private string BottleDrawerCss => new CssBuilder("bottle-drawer-view")
        .AddClass("bottle-drawer-view--open", ViewModel.IsDoorOpen)
        .Build();

    protected override void OnInitialized()
    {
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
    }
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        StateHasChanged();
    }
    public void Dispose()
    {
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        ViewModel.Dispose();
        GC.SuppressFinalize(this);
    }

    private void BottleSelected(int selectedBottleId)
    {
        var panelUpdated = ViewModel.UpdatePanelInformation(ViewModel.BottlePositions, selectedBottleId);
        if(panelUpdated)
        {
            OnBottleSelected.InvokeAsync();
        }
    }
}
