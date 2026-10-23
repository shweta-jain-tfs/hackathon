using Microsoft.AspNetCore.Components.Authorization;
using Pharos.Client.Common.App.CommonViews.Errors;
using Pharos.Client.Common.App.State.CurrentInstrument.Models;
using Pharos.Common.Web.Components.Modal;
using Pharos.Contracts.Security;

namespace Pharos.Client.Common.App.State.CurrentInstrument;

internal record CurrentInstrumentViewModel : ICurrentInstrumentViewModel
{
    public event Action? OnCurrentInstrumentChange;
    public event Action? OnProcessStatusChanged;
    public event Action? OnSignalTowerChange;
    public event Action? OnTaskCardChange;
    public event Action<List<int>>? OnFatalErrorChanged;
    public event Action? OnCurrentInstrumentSampleDrawerOpen;
    public event Action? OnCurrentInstrumentReagentDrawerOpen;
    public event Action? OnCurrentInstrumentBottleDrawerOpen;
    public event Action? OnCurrentInstrumentLiquidDrawerOpen;
    public event Action? OnCurrentInstrumentSolidWasteDrawerOpen;
    public event Action<TaskCardType, TaskCardState>? OnTaskCardCreatedOrChanged;
    public event Action<string>? OnCurrentInstrumentUnknownBarcodeScanned;


    private readonly ICurrentInstrumentDataService _currentInstrumentDataService;
    private readonly ICurrentInstrumentNotificationProvider _notificationProvider;
    private readonly IModalService _modalService;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly NavigationManager _navigationManager;
    private readonly ScannedBarcodeViewModel _barcodeScannedViewModel;

    public CurrentInstrumentViewModel(ICurrentInstrumentDataService currentInstrumentDataService,
        ICurrentInstrumentNotificationProvider notificationProvider,
        IModalService modalService,
        AuthenticationStateProvider authStateProvider,
        NavigationManager navigationManager,
        ScannedBarcodeViewModel barcodeScannedViewModel)
    {
        _currentInstrumentDataService = currentInstrumentDataService;
        _notificationProvider = notificationProvider;
        _modalService = modalService;
        _authStateProvider = authStateProvider;
        _navigationManager = navigationManager;
        _barcodeScannedViewModel = barcodeScannedViewModel;
        SignalTower = new SignalTowerViewModel();

        if (!_isInitialized)
        {
            _ = LoadData();

            _isInitialized = true;
        }

        SubscribeToNotifications();
    }

    private bool _isSubscribedToNotifications;
    private bool _isInitialized;
    private bool _isAuthenticated;

    public string SerialNumber { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public bool Enabled { get; private set; }
    public LockState HoodLockState { get; private set; }
    public bool IsHoodUnlockInProgress { get; private set; }

    public InstrumentState State { get; private set; }
    public bool IsStateChangeRequested { get; set; }
    public DateTimeOffset? TimeWhenNextStateReached { get; private set; }
    public AssaySchedulerNextRunType AssaySchedulerNextRunType { get; private set; }
    public DateTimeOffset? AssaySchedulerNextRun { get; private set; }
    public int NumberOfTests { get; private set; }
    public SignalTowerViewModel SignalTower { get; }
    public int NumberOfTaskCards { get; private set; }
    public TaskCardState HighestUrgencyLevel { get; private set; }
    public bool PlaySound { get; set; }
    public bool MaintenanceNeeded { get; set; }

    public async Task LoadData()
    {
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        _isAuthenticated = authState.User.Identity?.IsAuthenticated ?? false;
        await LoadCurrentInstrumentData();
        await LoadTaskCardInformation();
        await LoadFatalErrors();
    }

    public async Task MuteSound()
    {
        await _currentInstrumentDataService.MuteSound();
    }

    private async Task LoadTaskCardInformation()
    {
        var taskCardInfo = await _currentInstrumentDataService.GetTaskCardsInformation();
        NumberOfTaskCards = taskCardInfo.NoOfTaskCards;
        HighestUrgencyLevel = taskCardInfo.HighestTaskCardState;
        NotifyTaskCardStateChanged();
    }

    private async Task LoadFatalErrors()
    {
        if (!_isAuthenticated)
        {
            return;
        }

        var systemFatalErrorIds = await _currentInstrumentDataService.GetFatalErrors();

        OnInstrumentFatalErrorChanged(null, systemFatalErrorIds);
    }

    private async Task LoadCurrentInstrumentData()
    {
        var instrument = await _currentInstrumentDataService.GetInstrument();

        Name = instrument.DisplayName;
        Enabled = instrument.Enabled;
        SerialNumber = instrument.SerialNumber;
        HoodLockState = instrument.HoodLockState.LockState;
        IsHoodUnlockInProgress = instrument.HoodLockState.IsUnlockInProgress;
        State = instrument.State;
        TimeWhenNextStateReached = instrument.TimeWhenNextStateReached;
        AssaySchedulerNextRunType = instrument.AssaySchedulerNextRunType;
        AssaySchedulerNextRun = instrument.AssaySchedulerNextRun;
        NumberOfTests = instrument.NumberOfTests;
        SignalTower.SetState(instrument.SignalTower);
        MaintenanceNeeded = instrument.MaintenanceNeeded;

        NotifyCurrentInstrumentStateChanged();
    }

    private void SubscribeToNotifications()
    {
        if (_isSubscribedToNotifications)
        {
            return;
        }

        _notificationProvider.Error += OnError;
        _notificationProvider.AssaySchedulerNextRunChanged += OnAssaySchedulerNextRunChanged;
        _notificationProvider.BottleDrawerLockStateChanged += OnBottleDrawerLockStateChanged;
        _notificationProvider.HoodLockStateChanged += OnHoodLockStateChanged;
        _notificationProvider.InstrumentEnabledChanged += OnInstrumentEnabledChanged;
        _notificationProvider.InstrumentFatalErrorChanged += OnInstrumentFatalErrorChanged;
        _notificationProvider.InstrumentNameChanged += OnInstrumentDisplayNameChanged;
        _notificationProvider.InstrumentStateChanged += OnInstrumentStateChanged;
        _notificationProvider.LeftSampleDrawerLockStateChanged += OnSampleDrawerLockStateChanged;
        _notificationProvider.LiquidDrawerLockStateChanged += OnLiquidDrawerLockStateChanged;
        _notificationProvider.MaintenanceInfoChanged += OnMaintenanceInfoChanged;
        _notificationProvider.NumberOfTestsUpdated += OnNumberOfTestsUpdated;
        _notificationProvider.ProcessListUpdated += OnProcessStatusUpdated;
        _notificationProvider.ReagentDrawerLockStateChanged += OnReagentDrawerLockStateChanged;
        _notificationProvider.RightSampleDrawerLockStateChanged += OnSampleDrawerLockStateChanged;
        _notificationProvider.ScannedBarcode += OnScannedBarcode;
        _notificationProvider.ShowMessageBox += OnShowMessageBox;
        _notificationProvider.SignalTowerStateChanged += OnSignalTowerStateChanged;
        _notificationProvider.TaskCardInformationChanged += OnTaskCardInformationChanged;
        _notificationProvider.TaskCardCreatedOrChanged += TaskCardCreatedOrChanged;
        _notificationProvider.TimeWhenNextStateReachedChanged += OnTimeWhenNextStateReachedChanged;
        _notificationProvider.UnknownBarcodeScanned += OnUnknownBarcodeScanned;
        _notificationProvider.UnlockDrawerAuthorizationRequest += OnUnlockDrawerAuthorizationRequest;
        _notificationProvider.WasteDrawerLockStateChanged += OnSolidWasteDrawerLockStateChanged;

        _isSubscribedToNotifications = true;
    }

    private void OnTimeWhenNextStateReachedChanged(object? sender, DateTimeOffset? timeWhenNextStateReached)
    {
        TimeWhenNextStateReached = timeWhenNextStateReached;
        NotifyCurrentInstrumentStateChanged();
    }

    private void OnMaintenanceInfoChanged(object? sender, bool needed)
    {
        MaintenanceNeeded = needed;
        NotifyCurrentInstrumentStateChanged();
    }

    private void OnProcessStatusUpdated(object? sender, EventArgs args)
    {
        OnProcessStatusChanged?.Invoke();
    }

    private async void OnScannedBarcode(string barcode)
    {
        if (!_isAuthenticated)
        {
            return;
        }
        await _barcodeScannedViewModel.ProcessScannedBarcode(barcode);
    }

    //TODO: Need to remove this method after integrating the barcode handling
    private void OnUnknownBarcodeScanned(string barcode)
    {
        OnCurrentInstrumentUnknownBarcodeScanned?.Invoke(barcode);
    }

    private void OnSampleDrawerLockStateChanged(LockState state, bool isUnlockInProgress)
    {
        if (state is LockState.Open)
        {
            NotifyOnCurrentInstrumentSampleDrawerOpen();
        }
    }

    private void OnReagentDrawerLockStateChanged(LockState state, bool isUnlockInProgress)
    {
        if (state is LockState.Open)
        {
            NotifyOnCurrentInstrumentReagentDrawerOpen();
        }
    }

    private void OnBottleDrawerLockStateChanged(LockState state)
    {
        if (state is LockState.Open)
        {
            NotifyOnCurrentInstrumentBottleDrawerOpen();
        }
    }

    private void OnLiquidDrawerLockStateChanged(LockState state)
    {
        if (state is LockState.Open)
        {
            NotifyOnCurrentInstrumentLiquidDrawerOpen();
        }
    }

    private void OnSolidWasteDrawerLockStateChanged(LockState state, bool isUnlockInProgress)
    {
        if (state is LockState.Open)
        {
            NotifyOnCurrentInstrumentSolidWasteDrawerOpen();
        }
    }

    private void OnInstrumentFatalErrorChanged(object? sender, List<int> errorIds)
    {
        if (!_isAuthenticated)
        {
            return;
        }

        OnFatalErrorChanged?.Invoke(errorIds);
    }

    private void OnError(object? sender, Exception e)
    {
        _isInitialized = false;
    }

    private void OnInstrumentDisplayNameChanged(object? sender, string name)
    {
        Name = name;
        NotifyCurrentInstrumentStateChanged();
    }

    private void OnHoodLockStateChanged(LockState hoodLockState, bool isUnlockInProgress)
    {
        HoodLockState = hoodLockState;
        IsHoodUnlockInProgress = isUnlockInProgress;
        NotifyCurrentInstrumentStateChanged();
    }

    private void OnInstrumentStateChanged(InstrumentState instrumentState)
    {
        if (instrumentState == State)
        {
            return;
        }

        // We are not waiting for a state change anymore.
        IsStateChangeRequested = false;

        State = instrumentState;
        NotifyCurrentInstrumentStateChanged();
    }

    private void OnAssaySchedulerNextRunChanged(AssaySchedulerNextRunType nextRunType, DateTimeOffset? nextRun)
    {
        AssaySchedulerNextRunType = nextRunType;
        AssaySchedulerNextRun = nextRun;
        NotifyCurrentInstrumentStateChanged();
    }

    private void OnNumberOfTestsUpdated(object? sender, int numberOfTests)
    {
        NumberOfTests = numberOfTests;
        NotifyCurrentInstrumentStateChanged();
    }

    private void OnInstrumentEnabledChanged(object? sender, bool enabled)
    {
        Enabled = enabled;
        NotifyCurrentInstrumentStateChanged();
    }

    private async Task OnShowMessageBox(object? sender, EventArgs args)
    {
        ToggleSound(true);
        var messageText = sender?.ToString();
        var parameters = new ModalParameters
           {
               { nameof(InstrumentMessageFromServerView.MessageText), messageText }
           };

        var modal = _modalService.Show<InstrumentMessageFromServerView>(new ModalOptions { Title = "Message from server" }, parameters);

        //Wait for the modal to close.  
        await modal.Result;

        ToggleSound(false);
    }

    private void ToggleSound(bool sound)
    {
        PlaySound = sound;
        NotifyCurrentInstrumentStateChanged();
    }

    private void OnSignalTowerStateChanged(object? sender, SignalTower signalTower)
    {
        SignalTower.SetState(signalTower);
        NotifySignalTowerChanged();
        NotifyCurrentInstrumentStateChanged();
    }

    private void NotifySignalTowerChanged()
    {
        OnSignalTowerChange?.Invoke();
    }

    private void OnTaskCardInformationChanged(int noOfTaskCards, int highestTaskCardState)
    {
        var notify = false;

        if (NumberOfTaskCards != noOfTaskCards)
        {
            NumberOfTaskCards = noOfTaskCards;
            notify = true;
        }

        var highestCardState = (TaskCardState)highestTaskCardState;
        if (HighestUrgencyLevel != highestCardState)
        {
            HighestUrgencyLevel = highestCardState;
            notify = true;
        }

        if (notify)
        {
            NotifyTaskCardStateChanged();
        }
    }

    private void TaskCardCreatedOrChanged(TaskCardType taskCardType, TaskCardState taskCardState)
    {
        OnTaskCardCreatedOrChanged?.Invoke(taskCardType, taskCardState);
    }

    private async void OnUnlockDrawerAuthorizationRequest(object? sender, string instrumentSerialNumber)
    {
        if (SerialNumber == instrumentSerialNumber && !_isAuthenticated)
        {
            _navigationManager.NavigateTo(AccountRoutes.Login, forceLoad: true);
        }
        else
        {
            await _currentInstrumentDataService.UnlockDrawerAuthorizationResponse();
        }
    }

    private void UnsubscribeFromNotifications()
    {
        if (!_isSubscribedToNotifications)
        {
            return;
        }

        _notificationProvider.Error -= OnError;
        _notificationProvider.ProcessListUpdated -= OnProcessStatusUpdated;
        _notificationProvider.AssaySchedulerNextRunChanged -= OnAssaySchedulerNextRunChanged;
        _notificationProvider.BottleDrawerLockStateChanged -= OnBottleDrawerLockStateChanged;
        _notificationProvider.HoodLockStateChanged -= OnHoodLockStateChanged;
        _notificationProvider.InstrumentEnabledChanged -= OnInstrumentEnabledChanged;
        _notificationProvider.InstrumentFatalErrorChanged -= OnInstrumentFatalErrorChanged;
        _notificationProvider.InstrumentNameChanged -= OnInstrumentDisplayNameChanged;
        _notificationProvider.InstrumentStateChanged -= OnInstrumentStateChanged;
        _notificationProvider.LeftSampleDrawerLockStateChanged -= OnSampleDrawerLockStateChanged;
        _notificationProvider.LiquidDrawerLockStateChanged -= OnLiquidDrawerLockStateChanged;
        _notificationProvider.MaintenanceInfoChanged -= OnMaintenanceInfoChanged;
        _notificationProvider.NumberOfTestsUpdated -= OnNumberOfTestsUpdated;
        _notificationProvider.ReagentDrawerLockStateChanged -= OnReagentDrawerLockStateChanged;
        _notificationProvider.RightSampleDrawerLockStateChanged -= OnSampleDrawerLockStateChanged;
        _notificationProvider.ScannedBarcode -= OnScannedBarcode;
        _notificationProvider.ShowMessageBox -= OnShowMessageBox;
        _notificationProvider.SignalTowerStateChanged -= OnSignalTowerStateChanged;
        _notificationProvider.TaskCardInformationChanged -= OnTaskCardInformationChanged;
        _notificationProvider.TaskCardCreatedOrChanged -= TaskCardCreatedOrChanged;
        _notificationProvider.TimeWhenNextStateReachedChanged -= OnTimeWhenNextStateReachedChanged;
        _notificationProvider.UnknownBarcodeScanned -= OnUnknownBarcodeScanned;
        _notificationProvider.UnlockDrawerAuthorizationRequest -= OnUnlockDrawerAuthorizationRequest;
        _notificationProvider.WasteDrawerLockStateChanged -= OnSolidWasteDrawerLockStateChanged;
        _isSubscribedToNotifications = false;
    }

    private void NotifyCurrentInstrumentStateChanged()
    {
        OnCurrentInstrumentChange?.Invoke();
    }

    private void NotifyTaskCardStateChanged()
    {
        OnTaskCardChange?.Invoke();
    }

    private void NotifyOnCurrentInstrumentSampleDrawerOpen()
    {
        OnCurrentInstrumentSampleDrawerOpen?.Invoke();
    }

    private void NotifyOnCurrentInstrumentReagentDrawerOpen()
    {
        OnCurrentInstrumentReagentDrawerOpen?.Invoke();
    }

    private void NotifyOnCurrentInstrumentBottleDrawerOpen()
    {
        OnCurrentInstrumentBottleDrawerOpen?.Invoke();
    }

    private void NotifyOnCurrentInstrumentLiquidDrawerOpen()
    {
        OnCurrentInstrumentLiquidDrawerOpen?.Invoke();
    }

    private void NotifyOnCurrentInstrumentSolidWasteDrawerOpen()
    {
        OnCurrentInstrumentSolidWasteDrawerOpen?.Invoke();
    }

    public void Dispose()
    {
        UnsubscribeFromNotifications();
        GC.SuppressFinalize(this);
    }
}
