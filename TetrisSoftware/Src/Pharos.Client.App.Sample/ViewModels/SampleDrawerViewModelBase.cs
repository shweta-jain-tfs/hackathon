using Pharos.Client.App.Sample.Models;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Common.Web.Components.Drawer;
using Pharos.Contracts.Security;

namespace Pharos.Client.App.Sample.ViewModels;

public abstract class SampleDrawerViewModelBase : LoadingViewModel, IDisposable
{
    private readonly ISampleDrawerDataSource _sampleDrawerDataSource;
    private readonly IAuthorizationService _userInformationProvider;
    private bool _isSubscribedToNotifications;

    protected ICurrentInstrumentNotificationProvider InstrumentNotificationProvider { get; }
    protected ISampleTrayPositionNotificationProvider SampleTrayPositionNotificationProvider { get; }
    protected IEntityNotificationProvider<SampleTray, string> SampleTrayNotificationProvider { get; }

    protected SampleDrawerViewModelBase(
        ISampleDrawerDataSource sampleDrawerDataSource,
        ICurrentInstrumentNotificationProvider instrumentNotificationProvider,
        ISampleTrayPositionNotificationProvider sampleTrayPositionNotificationProvider,
        IEntityNotificationProvider<SampleTray, string> sampleTrayNotificationProvider,
        IAuthorizationService userInformationProvider)
    {
        _sampleDrawerDataSource = sampleDrawerDataSource;
        InstrumentNotificationProvider = instrumentNotificationProvider;
        InstrumentNotificationProvider.Error += OnError;
        SampleTrayPositionNotificationProvider = sampleTrayPositionNotificationProvider;
        SampleTrayPositionNotificationProvider.PositionStatusUpdated += OnSampleTrayPositionStatusUpdated;
        SampleTrayNotificationProvider = sampleTrayNotificationProvider;
        SampleTrayNotificationProvider.Deleted += OnSampleTrayChanged;
        _userInformationProvider = userInformationProvider;
    }

    public bool IsButtonVisible
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(IsButtonVisible));
        }
    }

    public PhDrawerLockState PhDrawerLockState
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(PhDrawerLockState));
        }
    }

    public bool IsDrawerDim
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(IsDrawerDim));
        }
    }

    public bool HasRequiredPermission
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(HasRequiredPermission));
        }
    }

    public bool IsScanningOngoing
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(IsScanningOngoing));
        }
    }

    public List<SampleTrayDetails> SampleTrays
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(SampleTrays));
        }
    } = new();

    public event EventHandler? ScanningStateChanged;


    public void Dispose()
    {
        InstrumentNotificationProvider.Error -= OnError;
        SampleTrayPositionNotificationProvider.PositionStatusUpdated -= OnSampleTrayPositionStatusUpdated;
        SampleTrayNotificationProvider.Deleted -= OnSampleTrayChanged;
        UnsubscribeFromNotifications();

        GC.SuppressFinalize(this);
    }

    protected abstract void SubscribeToNotifications();

    protected abstract void UnsubscribeFromNotifications();

    public async Task UnlockSampleDrawerAsync()
    {
        await _sampleDrawerDataSource.UnlockDrawerAsync();
    }

    public event EventHandler<PhDrawerLockState>? LockStateChanged;

    protected override async Task DoLoadAsync()
    {
        if (!_isSubscribedToNotifications)
        {
            SubscribeToNotifications();
            _isSubscribedToNotifications = true;
        }

        HasRequiredPermission = await _userInformationProvider.IsAuthorized(Permissions.ManageSamples);
        await Task.CompletedTask;
    }

    protected void OnDrawerLockStateChanged(LockState drawerLockState, bool isUnlockInProgress)
    {
        PhDrawerLockState = GetPhDrawerLockState(drawerLockState, isUnlockInProgress);
        SetDrawerButtonVisibility(PhDrawerLockState);
        OnLockStateChanged(PhDrawerLockState);
    }

    protected void OnDrawerScanningStatusChanged(bool isScanningOnGoing)
    {
        IsScanningOngoing = isScanningOnGoing;
        OnScanStateChanged();
    }

    protected async Task OnSampleDrawerContentsChanged()
    {
        SampleTrays = [.. (await _sampleDrawerDataSource.GetDrawerDetailsAsync()).SampleTrays];
    }

    private async Task OnSampleTrayPositionStatusUpdated(List<string> trayIds)
    {
        if (SampleTrays.Any(tray => trayIds.Contains(tray.Id)))
        {
            SampleTrays = [.. (await _sampleDrawerDataSource.GetDrawerDetailsAsync()).SampleTrays];
        }
    }

    private async Task OnSampleTrayChanged(string trayId)
    {
        if (SampleTrays.Any(tray => tray.Id == trayId))
        {
            SampleTrays = [.. (await _sampleDrawerDataSource.GetDrawerDetailsAsync()).SampleTrays];
        }
    }

    private void OnLockStateChanged(PhDrawerLockState lockState)
    {
        LockStateChanged?.Invoke(this, lockState);
    }

    private void OnScanStateChanged()
    {
        ScanningStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnError(object? sender, Exception e)
    {
        NeedsReload = true;
    }

    public void UpdateDrawer(SampleDrawer drawer)
    {
        SampleTrays = drawer?.SampleTrays?.ToList() ?? new List<SampleTrayDetails>();

        IsScanningOngoing = drawer?.IsScanOngoing ?? false;
        OnScanStateChanged();

        PhDrawerLockState = GetPhDrawerLockState(drawer?.LockState ?? LockState.ClosedLocked, drawer?.IsUnlockInProgress ?? false);
        SetDrawerButtonVisibility(PhDrawerLockState);
        OnLockStateChanged(PhDrawerLockState);
    }


    private static PhDrawerLockState GetPhDrawerLockState(LockState lockState, bool isUnlockInProgress)
    {
        if (isUnlockInProgress)
        {
            return PhDrawerLockState.UnlockInProgress;
        }
        else
        {
            return lockState switch
            {
                LockState.ClosedLocked => PhDrawerLockState.ClosedLocked,
                LockState.ClosedUnlocked => PhDrawerLockState.ClosedUnlocked,
                LockState.Open => PhDrawerLockState.Open,
                _ => PhDrawerLockState.ClosedLocked
            };
        }
    }

    private void SetDrawerButtonVisibility(PhDrawerLockState drawerLockState)
    {
        switch (drawerLockState)
        {
            case PhDrawerLockState.ClosedLocked:
                IsButtonVisible = true;
                break;
            case PhDrawerLockState.UnlockInProgress:
            case PhDrawerLockState.ClosedUnlocked:
            case PhDrawerLockState.Open:
                IsButtonVisible = false;
                break;
        }
    }
}