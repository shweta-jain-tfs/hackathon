using Pharos.Client.App.Reagents.Models;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.Models;
using Pharos.Client.Common.Models.Notifications;
using Pharos.Client.Common.ViewModels;
using Pharos.Common.Web.Components.Drawer;
using Pharos.Contracts.Security;

namespace Pharos.Client.App.Reagents.ViewModels;
public class ReagentDrawerViewModel : LoadingViewModel, IDisposable
{
    private readonly IAuthorizationService _userInformationProvider;
    private readonly IReagentDrawerDataSource _reagentDrawerDataSource;
    private bool _isSubscribedToNotifications;


    public event Action? RemoveLegendSelection;
    protected ICurrentInstrumentNotificationProvider NotificationProvider { get; }
    public ReagentLoaderPanelViewModel LoaderPanelViewModel { get; }


    public ReagentDrawerViewModel(ICurrentInstrumentNotificationProvider notificationProvider,
        IReagentDrawerDataSource reagentDrawerDataSource,
        ReagentLoaderPanelViewModel reagentLoaderPanelViewModel,
        IAuthorizationService userInformationProvider)
    {
        NotificationProvider = notificationProvider;
        _reagentDrawerDataSource = reagentDrawerDataSource;
        LoaderPanelViewModel = reagentLoaderPanelViewModel;
        _userInformationProvider = userInformationProvider;
    }

    public void Dispose()
    {
        UnsubscribeFromNotifications();
        GC.SuppressFinalize(this);
    }

    public bool IsButtonVisible
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

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
            SetDrawerButtonVisibility(PhDrawerLockState);
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
    } = true;

    public bool IsScanningOngoing
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(IsScanningOngoing));
        }
    }

    public bool NeedEmptyLoader
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(NeedEmptyLoader));
        }
    }

    public List<ReagentLoader> ReagentLoaders
    {
        get;
        set
        {
            field = value ?? new();
            OnPropertyChanged(nameof(ReagentLoaders));
            if (LoaderPanelViewModel.SelectedReagentLoader != null)
            {
                var foundLoader = field.FirstOrDefault(loader => loader.Position == LoaderPanelViewModel.SelectedReagentLoader.Position);
                LoaderPanelViewModel.UpdateLoaderPanel(foundLoader);
                OnPropertyChanged(nameof(LoaderPanelViewModel.SelectedReagentLoader));
            }
        }
    } = new();

    public List<DilutionPlateStack> DilutionPlateStacks
    {
        get;
        set
        {
            field = value ?? new();
            OnPropertyChanged(nameof(DilutionPlateStacks));
        }
    } = new();

    public ReagentPositionStatus? SelectedLegendStatus
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(SelectedLegendStatus));
            LoaderPanelViewModel.UpdateLegendStatus(SelectedLegendStatus);
            OnPropertyChanged(nameof(LoaderPanelViewModel.SelectedLegendStatus));
        }
    }

    public async Task UnlockReagentDrawerAsync()
    {
        await _reagentDrawerDataSource.UnlockReagentDrawerAsync();
    }

    public event EventHandler<PhDrawerLockState>? LockStateChanged;

    public event EventHandler<bool>? ScanningStateChanged;

    public event EventHandler? DrawerContentsChanged;

    protected override async Task DoLoadAsync()
    {
        if (!_isSubscribedToNotifications)
        {
            SubscribeToNotifications();
            _isSubscribedToNotifications = true;
        }
        HasRequiredPermission = await _userInformationProvider.IsAuthorized(Permissions.ManageReagents);
    }

    protected void SubscribeToNotifications()
    {
        NotificationProvider.ReagentDrawerContentsUpdated += OnDrawerContentsChanged;
        NotificationProvider.ReagentDrawerLockStateChanged += OnDrawerLockStateChanged;
        NotificationProvider.ReagentDrawerScanningStatusChanged += OnScanStateChanged;
        NotificationProvider.Error += OnError;
        LoaderPanelViewModel.RemoveLegendSelection += OnRemoveLegendSelection;
    }



    protected void UnsubscribeFromNotifications()
    {
        NotificationProvider.ReagentDrawerContentsUpdated -= OnDrawerContentsChanged;
        NotificationProvider.ReagentDrawerLockStateChanged -= OnDrawerLockStateChanged;
        NotificationProvider.ReagentDrawerScanningStatusChanged -= OnScanStateChanged;
        NotificationProvider.Error -= OnError;
        LoaderPanelViewModel.RemoveLegendSelection -= OnRemoveLegendSelection;

    }

    internal void UpdateDrawer(ReagentDrawer drawerDetails)
    {
        var lockState = drawerDetails?.LockState ?? LockState.ClosedLocked;
        var isScanOngoing = drawerDetails?.IsScanOngoing ?? false;
        IsScanningOngoing = isScanOngoing;
        NeedEmptyLoader = drawerDetails?.NeedEmptyLoader ?? false;
        PhDrawerLockState = GetPhDrawerLockState(lockState, drawerDetails?.IsUnlockInProgress ?? false);
        ReagentLoaders = drawerDetails?.ReagentLoaders.ToList() ?? new();
        DilutionPlateStacks = drawerDetails?.DilutionPlateStacks.ToList() ?? new();
        DrawerContentsChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task OnDrawerContentsChanged()
    {
        var reagentDrawer = await _reagentDrawerDataSource.GetReagentDrawerDetailsAsync();
        UpdateDrawer(reagentDrawer);
    }

    private void OnDrawerLockStateChanged(LockState lockState, bool isUnlockInProgress)
    { 
        PhDrawerLockState = GetPhDrawerLockState(lockState, isUnlockInProgress);
        LockStateChanged?.Invoke(this, PhDrawerLockState);
    }

    private void OnScanStateChanged(bool isScanningOn)
    {
        IsScanningOngoing = isScanningOn;
        ScanningStateChanged?.Invoke(this, isScanningOn);
    }

    private void OnError(object? sender, Exception e)
    {
        NeedsReload = true;
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

    private void OnRemoveLegendSelection()
    {
        RemoveLegendSelection?.Invoke();
    }

    internal void UpdateLegendStatus(ReagentPositionStatus? selectedLegendStatus)
    {
        SelectedLegendStatus = selectedLegendStatus;

    }
}

public enum ReagentIconSize
{
    Small,
    Large
}
