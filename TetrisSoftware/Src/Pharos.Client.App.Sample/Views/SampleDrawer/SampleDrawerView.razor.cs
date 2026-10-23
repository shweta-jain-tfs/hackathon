using System.ComponentModel;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.Sample.ViewModels;
using Pharos.Client.Common.Localization;
using Pharos.Common.Web.Components.Drawer;
using Pharos.Common.Web.Components.Legend;

namespace Pharos.Client.App.Sample.Views.SampleDrawer;

public partial class SampleDrawerView : IDisposable
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Parameter] public SampleDrawerViewModelBase ViewModel { get; set; } = default!;
    [Parameter] public DrawerPosition DrawerPosition { get; set; }
    [Parameter] public bool AnyTraySelected { get; set; }
    [Parameter] public bool TraySelectionChanged { get; set; } = true;
    [Parameter] public string DrawerName { get; set; } = default!;

    [Parameter] public Func<TrayDetailsEventArgs, Task<bool>>? TraySelected { get; set; }

    [CascadingParameter(Name = "SampleDrawerLegendSelectionResult")]
    public LegendSelectionResult? SampleDrawerLegendSelectionResult { get; set; }

    [CascadingParameter(Name = "SampleDrawerOnResetLegendSelectionRequested")]
    public Action? SampleDrawerOnResetLegendSelectionRequested { get; set; }


    private string? _selectedTrayId;

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

    protected async Task HandleTraySelected(SampleTrayDetails selectedTray)
    {
        var args = new TrayDetailsEventArgs
        {
            SelectedTray = selectedTray,
            Position = DrawerPosition
        };

        var isTraySelectionChanged = await TraySelected!.Invoke(args);
        if (isTraySelectionChanged)
        {
            _selectedTrayId = selectedTray.Id;
            ResetLegendSelection();
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.PhDrawerLockState))
        {
            ResetLegendSelection();
        }

        StateHasChanged();
    }

    private TrayBarcodePosition GetTrayBarcodePosition()
    {
        if (AnyTraySelected)
        {
            return TrayBarcodePosition.None;
        }

        return DrawerPosition switch
        {
            DrawerPosition.Left => TrayBarcodePosition.Right,
            DrawerPosition.Right => TrayBarcodePosition.Left,
            _ => TrayBarcodePosition.None
        };
    }

    private bool IsDrawerDim()
    {
        if (ViewModel.IsDrawerDim && !AnyTraySelected)
        {
            return true;
        }

        return SampleDrawerLegendSelectionResult?.AnyItemSelected() == true && !ViewModel.IsScanningOngoing;
    }

    private void ResetLegendSelection()
    {
        if (SampleDrawerOnResetLegendSelectionRequested is not null)
        {
            SampleDrawerOnResetLegendSelectionRequested.Invoke();
        }
    }

    private string GetDrawerStateText(PhDrawerLockState drawerState)
    {
        return drawerState switch
        {
            PhDrawerLockState.Open => Loc["SamplePage_DrawerOpen"],
            PhDrawerLockState.UnlockInProgress => Loc["SamplePage_UnlockingDrawer"],
            PhDrawerLockState.ClosedUnlocked => Loc["SamplePage_DrawerUnlocked"],
            _ => string.Empty
        };
    }

    private static string GetTrayErrorName(SampleTrayError trayError)
    {
        return trayError.ToLocalizedString("Samples");
    }
}

public class TrayDetailsEventArgs
{
    public DrawerPosition Position { get; set; }
    public SampleTrayDetails SelectedTray { get; set; } = default!;
}