using Pharos.Client.App.Reagents.Models.SolidWasteDrawer;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.Models;
using Pharos.Client.Common.Models.Notifications;
using Pharos.Client.Common.ViewModels;
using Pharos.Common.Web.Components.Drawer;
using Pharos.Contracts.Security;

namespace Pharos.Client.App.Reagents.ViewModels.SolidWasteDrawer;

/// <summary>
/// ViewModel for the waste drawer view
/// </summary>
public class SolidWasteDrawerViewModel : LoadingViewModel, IDisposable
{
    private readonly ISolidWasteDrawerDataSource _wasteDrawerDataSource;
    private readonly IAuthorizationService _authorizationService;
    private bool _isSubscribedToNotifications;

    protected ICurrentInstrumentNotificationProvider NotificationProvider { get; }

    public SolidWasteDrawerViewModel(
        ICurrentInstrumentNotificationProvider notificationProvider,
        ISolidWasteDrawerDataSource wasteDrawerDataSource,
        IAuthorizationService authorizationService)
    {
        NotificationProvider = notificationProvider;
        _wasteDrawerDataSource = wasteDrawerDataSource;
        _authorizationService = authorizationService;
    }

    public ICollection<WasteContainer> Containers
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(Containers));
        }
    } = [];

    public PhDrawerLockState PhDrawerLockState
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(PhDrawerLockState));
            SetDrawerButtonVisibility(field);
            UpdateShowIndicator();
        }
    } = PhDrawerLockState.ClosedLocked;

    public bool IsButtonVisible
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(IsButtonVisible));
        }
    } = true;

    public bool HasRequiredPermission
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(HasRequiredPermission));
            UpdateShowIndicator();
        }
    } = true;

    public bool ShowIndicator
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(ShowIndicator));
        }
    }

    protected override async Task DoLoadAsync()
    {
        if (!_isSubscribedToNotifications)
        {
            SubscribeToNotifications();
            _isSubscribedToNotifications = true;
        }

        HasRequiredPermission = await _authorizationService.IsAuthorized(Permissions.ManageReagents);

        await GetWasteDrawerContents();
    }

    private async Task GetWasteDrawerContents()
    {
        var wasteDrawerDetails = await _wasteDrawerDataSource.GetWasteDrawerDetailsAsync();
        UpdateDrawerDetails(wasteDrawerDetails);
    }

    private void UpdateDrawerDetails(SolidWasteDrawerDetails? wasteDrawerDetails)
    {
        if (wasteDrawerDetails != null)
        {
            Containers = wasteDrawerDetails.Containers;
        }

        PhDrawerLockState = GetPhDrawerLockState(wasteDrawerDetails?.LockState ?? LockState.ClosedLocked, wasteDrawerDetails?.IsUnlockInProgress ?? false);
    }

    private void UpdateShowIndicator()
    {
        ShowIndicator = HasRequiredPermission && PhDrawerLockState == PhDrawerLockState.Open;
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

    protected void SubscribeToNotifications()
    {
        NotificationProvider.WasteDrawerLockStateChanged += OnDrawerLockStateChanged;
        NotificationProvider.WasteDrawerContentsChanged += OnWasteDrawerContentsChanged;
        NotificationProvider.WasteBinPresenceChanged += OnWasteBinPresenceChanged;
        NotificationProvider.Error += OnError;
    }

    protected void UnsubscribeFromNotifications()
    {
        NotificationProvider.WasteDrawerLockStateChanged -= OnDrawerLockStateChanged;
        NotificationProvider.WasteDrawerContentsChanged -= OnWasteDrawerContentsChanged;
        NotificationProvider.WasteBinPresenceChanged -= OnWasteBinPresenceChanged;
        NotificationProvider.Error -= OnError;
    }

    private async void OnDrawerLockStateChanged(LockState lockState, bool isUnlockInProgress)
    {
        PhDrawerLockState = GetPhDrawerLockState(lockState, isUnlockInProgress);
        await GetWasteDrawerContents();
    }

    private async Task OnWasteDrawerContentsChanged()
    {
        await GetWasteDrawerContents();
    }

    private async Task OnWasteBinPresenceChanged()
    {
        await GetWasteDrawerContents();
    }

    public async Task UnlockSolidWasteDrawer()
    {
        await _wasteDrawerDataSource.UnlockSolidWasteDrawerAsync();
    }

    private void OnError(object? sender, Exception e)
    {
        NeedsReload = true;
    }

    public void Dispose()
    {
        UnsubscribeFromNotifications();
        GC.SuppressFinalize(this);
    }
}
