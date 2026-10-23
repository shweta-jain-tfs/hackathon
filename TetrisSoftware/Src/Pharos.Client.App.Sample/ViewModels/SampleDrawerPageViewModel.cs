using System.ComponentModel;
using Pharos.Client.App.Sample.Models;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Common.Web.Components.Drawer;

namespace Pharos.Client.App.Sample.ViewModels;

public class SampleDrawerPageViewModel : LoadingViewModel, IDisposable
{
    private readonly ISingletonDataSource<UiSettings> _uiSettingsDataSource;
    private readonly IAllSampleDrawerDataSource _sampleDrawersDataSource;
    private readonly ICurrentInstrumentTaskCardItemSource _taskCardItemSource;
    private readonly ITaskCardRefreshNotificationProvider _taskCardRefreshNotificationProvider;

    public LeftSampleDrawerViewModel LeftSampleDrawerViewModel { get; }
    public RightSampleDrawerViewModel RightSampleDrawerViewModel { get; }
    public SampleLegendViewModel LegendViewModel { get; }
    public bool IsLeftDrawerAsInputAndRightDrawerAsOutput { get; set; }

    public SampleDrawerPageViewModel(IAllSampleDrawerDataSource sampleDrawersDataSource, LeftSampleDrawerViewModel leftSampleDrawerViewModel,
                                     RightSampleDrawerViewModel rightSampleDrawerViewModel, SampleLegendViewModel legendViewModel,
                                     ISingletonDataSource<UiSettings> uiSettingsDataSource, ICurrentInstrumentTaskCardItemSource taskCardItemSource,
                                     ITaskCardRefreshNotificationProvider taskCardRefreshNotificationProvider)
    {
        _sampleDrawersDataSource = sampleDrawersDataSource;
        LeftSampleDrawerViewModel = leftSampleDrawerViewModel;
        RightSampleDrawerViewModel = rightSampleDrawerViewModel;
        LegendViewModel = legendViewModel;
        _uiSettingsDataSource = uiSettingsDataSource;
        _taskCardItemSource = taskCardItemSource;
        _taskCardRefreshNotificationProvider = taskCardRefreshNotificationProvider;
    }

    public void Dispose()
    {
        LeftSampleDrawerViewModel.LockStateChanged -= OnLeftSampleDrawerLockStateChanged;
        RightSampleDrawerViewModel.LockStateChanged -= OnRightSampleDrawerLockStateChanged;
        LeftSampleDrawerViewModel.ScanningStateChanged -= OnScanningStateChanged;
        RightSampleDrawerViewModel.ScanningStateChanged -= OnScanningStateChanged;
        LeftSampleDrawerViewModel.PropertyChanged -= OnDrawerViewModelPropertyChanged!;
        RightSampleDrawerViewModel.PropertyChanged -= OnDrawerViewModelPropertyChanged!;
        _taskCardRefreshNotificationProvider.RefreshNeeded -= OnTaskCardsRefreshNeeded;

        LeftSampleDrawerViewModel.Dispose();
        RightSampleDrawerViewModel.Dispose();
        GC.SuppressFinalize(this);
    }

    protected override async Task DoLoadAsync()
    {
        var uiSettings = await _uiSettingsDataSource.GetAsync();
        IsLeftDrawerAsInputAndRightDrawerAsOutput = uiSettings.SamplesSettings.IsLeftDrawerAsInputAndRightDrawerAsOutput;

        await Task.WhenAll(LeftSampleDrawerViewModel.LoadAsync(), RightSampleDrawerViewModel.LoadAsync());

        LeftSampleDrawerViewModel.LockStateChanged += OnLeftSampleDrawerLockStateChanged;
        RightSampleDrawerViewModel.LockStateChanged += OnRightSampleDrawerLockStateChanged;
        LeftSampleDrawerViewModel.ScanningStateChanged += OnScanningStateChanged;
        RightSampleDrawerViewModel.ScanningStateChanged += OnScanningStateChanged;
        LeftSampleDrawerViewModel.PropertyChanged += OnDrawerViewModelPropertyChanged!;
        RightSampleDrawerViewModel.PropertyChanged += OnDrawerViewModelPropertyChanged!;
        _taskCardRefreshNotificationProvider.RefreshNeeded += OnTaskCardsRefreshNeeded;

        await UpdateViewModels();
    }

    private async Task UpdateViewModels()
    {
        var drawerDetails = await _sampleDrawersDataSource.GetDrawersDetailsAsync();
        var sampleTrays = drawerDetails?.SelectMany(d => d.SampleTrays).ToList() ?? new List<SampleTrayDetails>();

        LeftSampleDrawerViewModel.UpdateDrawer(drawerDetails?.FirstOrDefault(x => x.Drawer == DrawerType.Sample1) ?? new SampleDrawer());
        RightSampleDrawerViewModel.UpdateDrawer(drawerDetails?.FirstOrDefault(x => x.Drawer == DrawerType.Sample2) ?? new SampleDrawer());

        LegendViewModel.UpdateLegend(sampleTrays);
        await UpdateTaskCardLegendAsync();
    }

    private async void OnTaskCardsRefreshNeeded(object? sender, EventArgs e)
    {
        await UpdateTaskCardLegendAsync();
    }

    private async Task UpdateTaskCardLegendAsync()
    {
        var taskCards = await _taskCardItemSource.GetAsync();
        LegendViewModel.UpdateTaskCardLegend(taskCards);
    }

    private void OnScanningStateChanged(object? sender, EventArgs e)
    {
        SetDrawerDimState(LeftSampleDrawerViewModel.PhDrawerLockState, RightSampleDrawerViewModel.PhDrawerLockState);
    }

    private void OnLeftSampleDrawerLockStateChanged(object? sender, PhDrawerLockState leftDrawerLockState)
    {
        SetDrawerDimState(leftDrawerLockState, RightSampleDrawerViewModel.PhDrawerLockState);
    }

    private void OnRightSampleDrawerLockStateChanged(object? sender, PhDrawerLockState rightDrawerLockState)
    {
        SetDrawerDimState(LeftSampleDrawerViewModel.PhDrawerLockState, rightDrawerLockState);
    }

    private void SetDrawerDimState(PhDrawerLockState leftDrawerLockState, PhDrawerLockState rightDrawerLockState)
    {
        if (leftDrawerLockState.Equals(PhDrawerLockState.ClosedLocked) && !rightDrawerLockState.Equals(PhDrawerLockState.ClosedLocked))
        {
            LeftSampleDrawerViewModel.IsDrawerDim = false;
            if (!LeftSampleDrawerViewModel.IsScanningOngoing)
            {
                LeftSampleDrawerViewModel.IsDrawerDim = true;
            }
        }
        else if (!leftDrawerLockState.Equals(PhDrawerLockState.ClosedLocked) && rightDrawerLockState.Equals(PhDrawerLockState.ClosedLocked))
        {
            RightSampleDrawerViewModel.IsDrawerDim = false;
            if (!RightSampleDrawerViewModel.IsScanningOngoing)
            {
                RightSampleDrawerViewModel.IsDrawerDim = true;
            }
        }
        else
        {
            RightSampleDrawerViewModel.IsDrawerDim = false;
            LeftSampleDrawerViewModel.IsDrawerDim = false;
        }
    }

    private void OnDrawerViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SampleDrawerViewModelBase.SampleTrays))
        {
            var sampleTrays = LeftSampleDrawerViewModel.SampleTrays.Concat(RightSampleDrawerViewModel.SampleTrays).ToList();
            LegendViewModel.UpdateLegend(sampleTrays);
        }
    }
}
