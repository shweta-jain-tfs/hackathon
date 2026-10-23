using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.Reagents.ViewModels.BottleDrawer;
using Pharos.Client.Common.Localization;
using Pharos.Common.Web.Utilities;

namespace Pharos.Client.App.Reagents.Views.BottleDrawer;
public partial class BottleDrawerPanel : ComponentBase
{
    [Parameter]
    public BottleDrawerPanelViewModel BottlePanel { get; set; } = default!;

    [Parameter]
    public EventCallback OnCloseClick { get; set; }

    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    
    private string BottleIndicatorCss => new CssBuilder("ph-bottle-drawer-indicator-container")
        .AddClass("ph-bottle-drawer-indicator-container--not-visible", !BottlePanel.IsLoading)
        .Build();

    protected override void OnInitialized()
    {
        BottlePanel.PropertyChanged += OnViewModelPropertyChanged;
        BottlePanel.OnClosePanel += ClosePanel;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        StateHasChanged();
    }

    public async Task OnCloseButtonClicked()
    {
        await ClosePanel();
    }

    public async Task ClosePanel()
    {
        BottlePanel.ClearSelection();
        await OnCloseClick.InvokeAsync();
        StateHasChanged();
    }

    public void Dispose()
    {
        BottlePanel.PropertyChanged -= OnViewModelPropertyChanged;
        BottlePanel.OnClosePanel -= ClosePanel;
    }


    public List<string> BottleNameList => new() { BottlePanel?.SelectedBottle?.Name ?? string.Empty };
}
