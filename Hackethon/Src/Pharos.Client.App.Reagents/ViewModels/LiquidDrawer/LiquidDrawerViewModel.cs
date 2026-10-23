using System.ComponentModel;
using Pharos.Client.App.Reagents.Models.LiquidDrawer;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.Models;
using Pharos.Client.Common.Models.Notifications;
using Pharos.Client.Common.ViewModels;
using Pharos.Common.Web.Components.Drawer;
using Pharos.Common.Web.Components.Reagent;
using Pharos.Contracts.Security;

namespace Pharos.Client.App.Reagents.ViewModels.LiquidDrawer;

public class LiquidDrawerViewModel : LoadingViewModel, IDisposable
{
    private readonly ILiquidDrawerDataSource _liquidDrawerDataSource;
    private readonly IAuthorizationService _userInformationProvider;
    private bool _isSubscribedToNotifications;

    protected ICurrentInstrumentNotificationProvider NotificationProvider { get; }

    public LiquidDrawerViewModel(
        ICurrentInstrumentNotificationProvider notificationProvider,
        ILiquidDrawerDataSource liquidDrawerDataSource,
        IAuthorizationService userInformationProvider,
        LiquidLegendViewModel legendViewModel)
    {
        NotificationProvider = notificationProvider;
        _liquidDrawerDataSource = liquidDrawerDataSource;
        _userInformationProvider = userInformationProvider;
        LegendViewModel = legendViewModel;
        LegendViewModel.PropertyChanged += OnLegendViewModelPropertyChanged;
    }

    public ICollection<LiquidContainer> Containers
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(Containers));
            UpdateContainerProperties();
        }
    } = [];

    public PhDrawerLockState PhDrawerLockState
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(PhDrawerLockState));
            OnPropertyChanged(nameof(ShowIndicator));
        }
    } = PhDrawerLockState.ClosedLocked;

    public bool HasRequiredPermission
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(HasRequiredPermission));
            OnPropertyChanged(nameof(ShowIndicator));
        }
    } = true;

    public bool ShowIndicator => HasRequiredPermission && PhDrawerLockState == PhDrawerLockState.Open;

    public LiquidLegendViewModel LegendViewModel { get; }

    public LiquidBottleStatus? SelectedLegendStatus => LegendViewModel.SelectedLegendStatus;

    public async Task OnLiquidDrawerContentsChanged()
    {
        await GetLiquidDrawerContents();
    }

    private async Task GetLiquidDrawerContents()
    {
        var liquidDrawerDetails = await _liquidDrawerDataSource.GetLiquidDrawerDetailsAsync();
        UpdateDrawerDetails(liquidDrawerDetails);
    }

    public void Dispose()
    {
        UnsubscribeFromNotifications();
        LegendViewModel.PropertyChanged -= OnLegendViewModelPropertyChanged;
        GC.SuppressFinalize(this);
    }

    protected override async Task DoLoadAsync()
    {
        if (!_isSubscribedToNotifications)
        {
            SubscribeToNotifications();
            _isSubscribedToNotifications = true;
        }

        HasRequiredPermission = await _userInformationProvider.IsAuthorized(Permissions.ManageReagents);

        await OnLiquidDrawerContentsChanged();
    }

    protected void SubscribeToNotifications()
    {
        NotificationProvider.LiquidDrawerLockStateChanged += OnDrawerLockStateChanged;
        NotificationProvider.LiquidDrawerContentsChanged += OnLiquidDrawerContentsChanged;
        NotificationProvider.LiquidPresenceChanged += OnLiquidPresenceChanged;
        NotificationProvider.Error += OnError;
    }

    protected void UnsubscribeFromNotifications()
    {
        NotificationProvider.LiquidDrawerLockStateChanged -= OnDrawerLockStateChanged;
        NotificationProvider.LiquidDrawerContentsChanged -= OnLiquidDrawerContentsChanged;
        NotificationProvider.LiquidPresenceChanged -= OnLiquidPresenceChanged;
        NotificationProvider.Error -= OnError;
    }

    private void UpdateDrawerDetails(LiquidDrawerDetails liquidDrawerDetails)
    {
        if (liquidDrawerDetails?.Containers != null)
        {
            Containers = liquidDrawerDetails.Containers;
        }
        PhDrawerLockState = GetPhDrawerLockState(liquidDrawerDetails?.LockState ?? LockState.ClosedLocked);
    }

    private void UpdateContainerProperties()
    {
        LegendViewModel.UpdateLegend(Containers);
    }

    private async void OnDrawerLockStateChanged(LockState lockState)
    {
        PhDrawerLockState = GetPhDrawerLockState(lockState);
        await GetLiquidDrawerContents();
    }

    private async Task OnLiquidPresenceChanged()
    {
        await GetLiquidDrawerContents();
    }

    private void OnError(object? sender, Exception e)
    {
        NeedsReload = true;
    }

    public void ClearLegendSelection()
    {
        LegendViewModel.ClearLegendSelection();
    }

    private void OnLegendViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LiquidLegendViewModel.SelectedLegendStatus))
        {
            OnPropertyChanged(nameof(SelectedLegendStatus));
        }
    }

    private static PhDrawerLockState GetPhDrawerLockState(LockState lockState)
    {
        return lockState switch
        {
            LockState.ClosedLocked => PhDrawerLockState.ClosedLocked,
            LockState.Open => PhDrawerLockState.Open,
            _ => PhDrawerLockState.ClosedLocked
        };
    }
}
