using System.ComponentModel;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.Sample.ViewModels;
using Pharos.Client.App.Sample.Views.SampleDrawer;
using Pharos.Client.App.Sample.Views.TrayPanel;
using Pharos.Client.Common.Localization;
using Pharos.Common.Web.Components.Drawer;

namespace Pharos.Client.App.Sample.Views;

public partial class SampleDrawerPage : ComponentBase, IDisposable
{
    [Inject] protected SampleDrawerPageViewModel ViewModel { get; set; } = default!;

    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;

    private DrawerPosition? _selectedTrayDrawerPosition;

    private string? _trayId;
    private TrayPanelView? _trayPanelViewRef;

    protected override async Task OnInitializedAsync()
    {
        ViewModel.LegendViewModel.PropertyChanged += OnLegendPropertyChanged;
        await ViewModel.LoadAsync();
    }

    private async Task<bool> OnTraySelected(TrayDetailsEventArgs args)
    {
        var isUnsavedChangesViewClosed = _trayPanelViewRef is not null && !await _trayPanelViewRef.IsUnsavedChangesHandledAsync();
        if (isUnsavedChangesViewClosed)
        {
            return false;
        }
        _selectedTrayDrawerPosition = args.Position;
        _trayId = args.SelectedTray.Id;
        return true;
    }

    private void TrayPanelCloseClicked()
    {
        _selectedTrayDrawerPosition = null;
        _trayId = null;
        _trayPanelViewRef = null;
    }

    private void OnLegendPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.LegendViewModel.LegendSelectionResult))
        {
            StateHasChanged();
        }
    }

    public void Dispose()
    {
        ViewModel.LegendViewModel.PropertyChanged -= OnLegendPropertyChanged;
        ViewModel.Dispose();
        GC.SuppressFinalize(this);
    }

    private string GetDrawerName(DrawerPosition drawerPosition)
    {
        var drawerAName = Loc["SamplePage_DrawerA"].ToString();
        var drawerBName = Loc["SamplePage_DrawerB"].ToString();

        if (ViewModel.IsLeftDrawerAsInputAndRightDrawerAsOutput)
        {
            drawerAName = $"{drawerAName} {Loc["SamplePage_Input"]}";
            drawerBName = $"{drawerBName} {Loc["SamplePage_Output"]}";
        }

        switch (drawerPosition)
        {
            case DrawerPosition.Left:
                return drawerAName;
            case DrawerPosition.Right:
                return drawerBName;
            default:
                return string.Empty;
        }
    }
}
