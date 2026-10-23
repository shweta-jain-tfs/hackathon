using Microsoft.AspNetCore.SignalR.Client;
using Pharos.Contracts.Notifications;

namespace Pharos.Client.Common.Models.Notifications;

internal sealed class CurrentInstrumentNotificationProvider(
    IHubConnectionFactory hubConnectionFactory) : HubNotificationProvider(hubConnectionFactory), ICurrentInstrumentNotificationProvider
{
    protected override string HubRoute => HubRoutes.CurrentInstrument;

    protected override void OnHubConnectionCreated(HubConnection hubConnection)
    {
        hubConnection.On(nameof(ICurrentInstrumentHub.InstrumentDeleted), OnInstrumentDeleted);
        hubConnection.On<bool>(nameof(ICurrentInstrumentHub.InstrumentEnabledChanged), OnInstrumentEnabledChanged);
        hubConnection.On<string>(nameof(ICurrentInstrumentHub.InstrumentNameChanged), OnInstrumentNameChanged);
        hubConnection.On<LockState, bool>(nameof(ICurrentInstrumentHub.HoodLockStateChanged), OnHoodLockStateChanged);
        hubConnection.On<InstrumentState>(nameof(ICurrentInstrumentHub.InstrumentStateChanged), OnInstrumentStateChanged);
        hubConnection.On<int, DateTimeOffset?>(nameof(ICurrentInstrumentHub.AssaySchedulerNextRunChanged), OnAssaySchedulerNextRunChanged);
        hubConnection.On<int>(nameof(ICurrentInstrumentHub.NumberOfTestsUpdated), OnNumberOfTestsUpdated);
        hubConnection.On<string>(nameof(ICurrentInstrumentHub.ShowMessageBox), OnShowMessageBox);
        hubConnection.On<SignalTowerLightState, SignalTowerSoundState>(nameof(ICurrentInstrumentHub.SignalTowerStateChanged), OnSignalTowerStateChanged);
        hubConnection.On(nameof(ICurrentInstrumentHub.InstrumentEnvironmentChanged), OnInstrumentEnvironmentChanged);
        hubConnection.On(nameof(ICurrentInstrumentHub.InstrumentMethodsChanged), OnMethodsChanged);
        hubConnection.On(nameof(ICurrentInstrumentHub.ProcessStatusUpdated), OnProcessListUpdated);
        hubConnection.On<DateTimeOffset?>(nameof(ICurrentInstrumentHub.TimeWhenNextStateReachedChanged), OnTimeWhenNextStateReachedChanged);

        hubConnection.On(nameof(ICurrentInstrumentHub.LeftSampleDrawerContentsChanged), OnLeftSampleDrawerContentsChanged);
        hubConnection.On<LockState, bool>(nameof(ICurrentInstrumentHub.LeftSampleDrawerLockStateChanged), OnLeftSampleDrawerLockStateChanged);
        hubConnection.On<bool>(nameof(ICurrentInstrumentHub.LeftSampleDrawerScanningStatusChanged), OnLeftSampleDrawerScanningStatusChanged);

        hubConnection.On(nameof(ICurrentInstrumentHub.RightSampleDrawerContentsChanged), OnRightSampleDrawerContentsChanged);
        hubConnection.On<LockState, bool>(nameof(ICurrentInstrumentHub.RightSampleDrawerLockStateChanged), OnRightSampleDrawerLockStateChanged);
        hubConnection.On<bool>(nameof(ICurrentInstrumentHub.RightSampleDrawerScanningStatusChanged), OnRightSampleDrawerScanningStatusChanged);

        hubConnection.On<List<string>>(nameof(ICurrentInstrumentHub.SampleDrawerContentsReceived), OnSampleDrawerContentsReceived);

        hubConnection.On(nameof(ICurrentInstrumentHub.ReagentDrawerContentsChanged), OnReagentDrawerContentsChanged);
        hubConnection.On<LockState, bool>(nameof(ICurrentInstrumentHub.ReagentDrawerLockStateChanged), OnReagentDrawerLockStateChanged);
        hubConnection.On<bool>(nameof(ICurrentInstrumentHub.ReagentDrawerScanningStatusChanged), OnReagentDrawerScanningStatusChanged);

        hubConnection.On<int, int>(nameof(ICurrentInstrumentHub.TaskCardInformationChanged), OnTaskCardInformationChanged);
        hubConnection.On<int, int>(nameof(ICurrentInstrumentHub.TaskCardCreatedOrChanged), OnTaskCardCreatedOrChanged);

        hubConnection.On<List<int>>(nameof(ICurrentInstrumentHub.InstrumentFatalError), OnInstrumentFatalErrorChanged);
        hubConnection.On<string>(nameof(ICurrentInstrumentHub.UnlockDrawerAuthorizationRequest), OnUnlockDrawerAuthorizationRequest);

        hubConnection.On(nameof(ICurrentInstrumentHub.BottleDrawerContentsChanged), OnBottleDrawerContentsChanged);
        hubConnection.On<LockState>(nameof(ICurrentInstrumentHub.BottleDrawerLockStateChanged), OnBottleDrawerLockStateChanged);

        hubConnection.On<string>(nameof(ICurrentInstrumentHub.ReagentBarcodeScanned), OnReagentBarcodeScanned);
        hubConnection.On<string>(nameof(ICurrentInstrumentHub.UnknownBarcodeScanned), OnUnknownBarcodeScanned);

        hubConnection.On<int, int, bool>(nameof(ICurrentInstrumentHub.BottlePresenceChanged), OnBottlePresenceChanged);

        hubConnection.On<LockState>(nameof(ICurrentInstrumentHub.LiquidDrawerLockStateChanged), OnLiquidDrawerLockStateChanged);
        hubConnection.On(nameof(ICurrentInstrumentHub.LiquidDrawerContentsChanged), OnLiquidDrawerContentsChanged);

        hubConnection.On(nameof(ICurrentInstrumentHub.LiquidPresenceChanged), OnLiquidPresenceChanged);

        hubConnection.On<LockState, bool>(nameof(ICurrentInstrumentHub.WasteDrawerLockStateChanged), OnWasteDrawerLockStateChanged);
        hubConnection.On(nameof(ICurrentInstrumentHub.WasteDrawerContentsChanged), OnWasteDrawerContentsChanged);

        hubConnection.On(nameof(ICurrentInstrumentHub.WasteBinPresenceChanged), OnWasteBinPresenceChanged);

        hubConnection.On(nameof(ICurrentInstrumentHub.IncorrectBarcodeScannedForBottles), OnIncorrectBarcodeScannedForBottles);
        hubConnection.On(nameof(ICurrentInstrumentHub.ModuleInformationReceived), OnModuleInformationReceived);

        hubConnection.On<bool>(nameof(ICurrentInstrumentHub.MaintenanceInfoChanged), OnMaintenanceInfoChanged);

        hubConnection.On<string>(nameof(ICurrentInstrumentHub.SendScannedBarcode), OnScannedBarcode);
    }

    public event EventHandler<DateTimeOffset?> TimeWhenNextStateReachedChanged
    {
        add => Subscribe(nameof(TimeWhenNextStateReachedChanged), value);
        remove => Unsubscribe(nameof(TimeWhenNextStateReachedChanged), value);
    }

    public event EventHandler? ModuleInformationReceived
    {
        add => Subscribe(nameof(ModuleInformationReceived), value);
        remove => Unsubscribe(nameof(ModuleInformationReceived), value);
    }

    public event EventHandler<string>? InstrumentNameChanged
    {
        add => Subscribe(nameof(InstrumentNameChanged), value);
        remove => Unsubscribe(nameof(InstrumentNameChanged), value);
    }

    public event EventHandler<bool>? InstrumentEnabledChanged
    {
        add => Subscribe(nameof(InstrumentEnabledChanged), value);
        remove => Unsubscribe(nameof(InstrumentEnabledChanged), value);
    }

    public event EventHandler InstrumentDeleted
    {
        add => Subscribe(nameof(InstrumentDeleted), value);
        remove => Unsubscribe(nameof(InstrumentDeleted), value);
    }

    public event Action<LockState, bool>? HoodLockStateChanged
    {
        add => Subscribe(nameof(HoodLockStateChanged), value);
        remove => Unsubscribe(nameof(HoodLockStateChanged), value);
    }

    public event Action<InstrumentState>? InstrumentStateChanged
    {
        add => Subscribe(nameof(InstrumentStateChanged), value);
        remove => Unsubscribe(nameof(InstrumentStateChanged), value);
    }

    public event Action<AssaySchedulerNextRunType, DateTimeOffset?>? AssaySchedulerNextRunChanged
    {
        add => Subscribe(nameof(AssaySchedulerNextRunChanged), value);
        remove => Unsubscribe(nameof(AssaySchedulerNextRunChanged), value);
    }

    public event EventHandler<int>? NumberOfTestsUpdated
    {
        add => Subscribe(nameof(NumberOfTestsUpdated), value);
        remove => Unsubscribe(nameof(NumberOfTestsUpdated), value);
    }

    public event Func<string, EventArgs, Task>? ShowMessageBox
    {
        add => Subscribe(nameof(ShowMessageBox), value);
        remove => Unsubscribe(nameof(ShowMessageBox), value);
    }

    public event EventHandler<SignalTower> SignalTowerStateChanged
    {
        add => Subscribe(nameof(SignalTowerStateChanged), value);
        remove => Unsubscribe(nameof(SignalTowerStateChanged), value);
    }

    public event EventHandler InstrumentEnvironmentChanged
    {
        add => Subscribe(nameof(InstrumentEnvironmentChanged), value);
        remove => Unsubscribe(nameof(InstrumentEnvironmentChanged), value);
    }

    public event EventHandler MethodsChanged
    {
        add => Subscribe(nameof(MethodsChanged), value);
        remove => Unsubscribe(nameof(MethodsChanged), value);
    }

    public event EventHandler ProcessListUpdated
    {
        add => Subscribe(nameof(ProcessListUpdated), value);
        remove => Unsubscribe(nameof(ProcessListUpdated), value);
    }

    public event Action<int, int> TaskCardInformationChanged
    {
        add => Subscribe(nameof(TaskCardInformationChanged), value);
        remove => Unsubscribe(nameof(TaskCardInformationChanged), value);
    }

    public event Action<TaskCardType, TaskCardState> TaskCardCreatedOrChanged
    {
        add => Subscribe(nameof(TaskCardCreatedOrChanged), value);
        remove => Unsubscribe(nameof(TaskCardCreatedOrChanged), value);
    }

    public event EventHandler<List<int>> InstrumentFatalErrorChanged
    {
        add => Subscribe(nameof(InstrumentFatalErrorChanged), value);
        remove => Unsubscribe(nameof(InstrumentFatalErrorChanged), value);
    }

    public event EventHandler<string> UnlockDrawerAuthorizationRequest
    {
        add => Subscribe(nameof(UnlockDrawerAuthorizationRequest), value);
        remove => Unsubscribe(nameof(UnlockDrawerAuthorizationRequest), value);
    }

    public event EventHandler<bool> MaintenanceInfoChanged
    {
        add => Subscribe(nameof(MaintenanceInfoChanged), value);
        remove => Unsubscribe(nameof(MaintenanceInfoChanged), value);
    }

    #region Left Sample Drawer

    public event Func<Task>? LeftSampleDrawerContentsChanged
    {
        add => Subscribe(nameof(LeftSampleDrawerContentsChanged), value);
        remove => Unsubscribe(nameof(LeftSampleDrawerContentsChanged), value);
    }

    public event Action<LockState, bool>? LeftSampleDrawerLockStateChanged
    {
        add => Subscribe(nameof(LeftSampleDrawerLockStateChanged), value);
        remove => Unsubscribe(nameof(LeftSampleDrawerLockStateChanged), value);
    }

    public event Action<bool>? LeftSampleDrawerScanningStatusChanged
    {
        add => Subscribe(nameof(LeftSampleDrawerScanningStatusChanged), value);
        remove => Unsubscribe(nameof(LeftSampleDrawerScanningStatusChanged), value);
    }

    private void OnLeftSampleDrawerContentsChanged()
    {
        GetHandler<Func<Task>>(nameof(LeftSampleDrawerContentsChanged))?.Invoke();
    }

    private void OnLeftSampleDrawerLockStateChanged(LockState lockState, bool isUnlockInProgress)
    {
        GetHandler<Action<LockState, bool>>(nameof(LeftSampleDrawerLockStateChanged))?.Invoke(lockState, isUnlockInProgress);
    }


    private void OnLeftSampleDrawerScanningStatusChanged(bool isScanningOnGoing)
    {
        GetHandler<Action<bool>>(nameof(LeftSampleDrawerScanningStatusChanged))?.Invoke(isScanningOnGoing);
    }

    #endregion

    #region Right Sample Drawer

    public event Func<Task>? RightSampleDrawerContentsChanged
    {
        add => Subscribe(nameof(RightSampleDrawerContentsChanged), value);
        remove => Unsubscribe(nameof(RightSampleDrawerContentsChanged), value);
    }

    public event Action<LockState, bool>? RightSampleDrawerLockStateChanged
    {
        add => Subscribe(nameof(RightSampleDrawerLockStateChanged), value);
        remove => Unsubscribe(nameof(RightSampleDrawerLockStateChanged), value);
    }

    public event Action<bool>? RightSampleDrawerScanningStatusChanged
    {
        add => Subscribe(nameof(RightSampleDrawerScanningStatusChanged), value);
        remove => Unsubscribe(nameof(RightSampleDrawerScanningStatusChanged), value);
    }

    private void OnRightSampleDrawerContentsChanged()
    {
        GetHandler<Func<Task>>(nameof(RightSampleDrawerContentsChanged))?.Invoke();
    }

    private void OnRightSampleDrawerLockStateChanged(LockState lockState, bool isUnlockInProgress)
    {
        GetHandler<Action<LockState, bool>>(nameof(RightSampleDrawerLockStateChanged))?.Invoke(lockState, isUnlockInProgress);
    }


    private void OnRightSampleDrawerScanningStatusChanged(bool isScanningOnGoing)
    {
        GetHandler<Action<bool>>(nameof(RightSampleDrawerScanningStatusChanged))?.Invoke(isScanningOnGoing);
    }

    #endregion

    public event EventHandler<List<string>> SampleDrawerContentsReceived
    {
        add => Subscribe(nameof(SampleDrawerContentsReceived), value);
        remove => Unsubscribe(nameof(SampleDrawerContentsReceived), value);
    }

    private void OnTimeWhenNextStateReachedChanged(DateTimeOffset? timeWhenNextStateReached)
    {
        GetHandler<EventHandler<DateTimeOffset?>>(nameof(TimeWhenNextStateReachedChanged))?.Invoke(this, timeWhenNextStateReached);
    }

    private void OnSampleDrawerContentsReceived(List<string> trayIds)
    {
        GetHandler<EventHandler<List<string>>>(nameof(SampleDrawerContentsReceived))?.Invoke(this, trayIds);
    }

    #region Reagent Drawer

    public event Action<LockState, bool>? ReagentDrawerLockStateChanged
    {
        add => Subscribe(nameof(ReagentDrawerLockStateChanged), value);
        remove => Unsubscribe(nameof(ReagentDrawerLockStateChanged), value);
    }

    public event Action<bool>? ReagentDrawerScanningStatusChanged
    {
        add => Subscribe(nameof(ReagentDrawerScanningStatusChanged), value);
        remove => Unsubscribe(nameof(ReagentDrawerScanningStatusChanged), value);
    }

    public event Func<Task> ReagentDrawerContentsUpdated
    {
        add => Subscribe(nameof(ReagentDrawerContentsUpdated), value);
        remove => Unsubscribe(nameof(ReagentDrawerContentsUpdated), value);
    }

    private void OnReagentDrawerContentsChanged()
    {
        GetHandler<Func<Task>>(nameof(ReagentDrawerContentsUpdated))?.Invoke();
    }

    private void OnReagentDrawerLockStateChanged(LockState lockState, bool isUnlockInProgress)
    {
        GetHandler<Action<LockState, bool>>(nameof(ReagentDrawerLockStateChanged))?.Invoke(lockState, isUnlockInProgress);
    }

    private void OnReagentDrawerScanningStatusChanged(bool isScanningOnGoing)
    {
        GetHandler<Action<bool>>(nameof(ReagentDrawerScanningStatusChanged))?.Invoke(isScanningOnGoing);
    }

    #endregion

    private void OnSignalTowerStateChanged(SignalTowerLightState lightState, SignalTowerSoundState soundState)
    {
        GetHandler<EventHandler<SignalTower>>(nameof(SignalTowerStateChanged))?.Invoke(this, new SignalTower
        {
            SoundState = soundState,
            LightState = lightState
        });
    }

    private void OnShowMessageBox(string message)
    {
        GetHandler<Func<string, EventArgs, Task>>(nameof(ShowMessageBox))?.Invoke(message, EventArgs.Empty);
    }

    private void OnInstrumentDeleted()
    {
        GetHandler<EventHandler>(nameof(InstrumentDeleted))?.Invoke(this, EventArgs.Empty);
    }

    private void OnInstrumentEnabledChanged(bool enabled)
    {
        GetHandler<EventHandler<bool>>(nameof(InstrumentEnabledChanged))?.Invoke(this, enabled);
    }

    private void OnInstrumentNameChanged(string name)
    {
        GetHandler<EventHandler<string>>(nameof(InstrumentNameChanged))?.Invoke(this, name);
    }

    private void OnModuleInformationReceived()
    {
        GetHandler<EventHandler>(nameof(ModuleInformationReceived))?.Invoke(this, EventArgs.Empty);
    }

    private void OnHoodLockStateChanged(LockState hoodLockState, bool isUnlockInProgress)
    {
        GetHandler<Action<LockState, bool>>(nameof(HoodLockStateChanged))?.Invoke(hoodLockState, isUnlockInProgress);
    }

    private void OnInstrumentStateChanged(InstrumentState status)
    {
        GetHandler<Action<InstrumentState>>(nameof(InstrumentStateChanged))?.Invoke(status);
    }

    private void OnAssaySchedulerNextRunChanged(int nextRunType, DateTimeOffset? nextRun)
    {
        var nextRunTypeEnum = Enum.IsDefined(typeof(AssaySchedulerNextRunType), nextRunType)
            ? (AssaySchedulerNextRunType)nextRunType
            : AssaySchedulerNextRunType.None;

        GetHandler<Action<AssaySchedulerNextRunType, DateTimeOffset?>>(nameof(AssaySchedulerNextRunChanged))?.Invoke(nextRunTypeEnum, nextRun);
    }

    private void OnNumberOfTestsUpdated(int numberOfTests)
    {
        GetHandler<EventHandler<int>>(nameof(NumberOfTestsUpdated))?.Invoke(this, numberOfTests);
    }

    private void OnInstrumentEnvironmentChanged()
    {
        GetHandler<EventHandler>(nameof(InstrumentEnvironmentChanged))?.Invoke(this, EventArgs.Empty);
    }

    private void OnMethodsChanged()
    {
        GetHandler<EventHandler>(nameof(MethodsChanged))?.Invoke(this, EventArgs.Empty);
    }

    private void OnProcessListUpdated()
    {
        GetHandler<EventHandler>(nameof(ProcessListUpdated))?.Invoke(this, EventArgs.Empty);
    }

    private void OnTaskCardInformationChanged(int taskCardCount, int highestTaskCardState)
    {
        GetHandler<Action<int, int>>(nameof(TaskCardInformationChanged))?.Invoke(taskCardCount, highestTaskCardState);
    }

    private void OnTaskCardCreatedOrChanged(int taskCardType, int taskCardState)
    {
        GetHandler<Action<TaskCardType, TaskCardState>>(nameof(TaskCardCreatedOrChanged))
            ?.Invoke((TaskCardType)taskCardType, (TaskCardState)taskCardState);
    }

    private void OnInstrumentFatalErrorChanged(List<int> errorCode)
    {
        GetHandler<EventHandler<List<int>>>(nameof(InstrumentFatalErrorChanged))?.Invoke(this, errorCode);
    }
    private void OnUnlockDrawerAuthorizationRequest(string instrumentNumber)
    {
        GetHandler<EventHandler<string>>(nameof(UnlockDrawerAuthorizationRequest))?.Invoke(this, instrumentNumber);
    }

    #region Bottle Drawer

    public event Func<Task> BottleDrawerContentsChanged
    {
        add => Subscribe(nameof(BottleDrawerContentsChanged), value);
        remove => Unsubscribe(nameof(BottleDrawerContentsChanged), value);
    }

    public event Action<LockState> BottleDrawerLockStateChanged
    {
        add => Subscribe(nameof(BottleDrawerLockStateChanged), value);
        remove => Unsubscribe(nameof(BottleDrawerLockStateChanged), value);
    }

    private void OnBottleDrawerContentsChanged()
    {
        GetHandler<Func<Task>>(nameof(BottleDrawerContentsChanged))?.Invoke();
    }

    private void OnBottleDrawerLockStateChanged(LockState lockState)
    {
        GetHandler<Action<LockState>>(nameof(BottleDrawerLockStateChanged))?.Invoke(lockState);
    }

    #endregion

    #region Liquid Drawer

    public event Func<Task> LiquidDrawerContentsChanged
    {
        add => Subscribe(nameof(LiquidDrawerContentsChanged), value);
        remove => Unsubscribe(nameof(LiquidDrawerContentsChanged), value);
    }

    public event Action<LockState> LiquidDrawerLockStateChanged
    {
        add => Subscribe(nameof(LiquidDrawerLockStateChanged), value);
        remove => Unsubscribe(nameof(LiquidDrawerLockStateChanged), value);
    }

    public event Func<Task> LiquidPresenceChanged
    {
        add => Subscribe(nameof(LiquidPresenceChanged), value);
        remove => Unsubscribe(nameof(LiquidPresenceChanged), value);
    }

    private void OnLiquidDrawerContentsChanged()
    {
        GetHandler<Func<Task>>(nameof(LiquidDrawerContentsChanged))?.Invoke();
    }

    private void OnLiquidDrawerLockStateChanged(LockState lockState)
    {
        GetHandler<Action<LockState>>(nameof(LiquidDrawerLockStateChanged))?.Invoke(lockState);
    }

    private void OnLiquidPresenceChanged()
    {
        GetHandler<Func<Task>>(nameof(LiquidPresenceChanged))?.Invoke();
    }

    #endregion

    #region Waste Drawer

    public event Func<Task> WasteDrawerContentsChanged
    {
        add => Subscribe(nameof(WasteDrawerContentsChanged), value);
        remove => Unsubscribe(nameof(WasteDrawerContentsChanged), value);
    }

    public event Action<LockState, bool> WasteDrawerLockStateChanged
    {
        add => Subscribe(nameof(WasteDrawerLockStateChanged), value);
        remove => Unsubscribe(nameof(WasteDrawerLockStateChanged), value);
    }

    public event Func<Task> WasteBinPresenceChanged
    {
        add => Subscribe(nameof(WasteBinPresenceChanged), value);
        remove => Unsubscribe(nameof(WasteBinPresenceChanged), value);
    }

    private void OnWasteDrawerContentsChanged()
    {
        GetHandler<Func<Task>>(nameof(WasteDrawerContentsChanged))?.Invoke();
    }

    private void OnWasteDrawerLockStateChanged(LockState lockState, bool isUnlockInProgress)
    {
        GetHandler<Action<LockState, bool>>(nameof(WasteDrawerLockStateChanged))?.Invoke(lockState, isUnlockInProgress);
    }

    private void OnWasteBinPresenceChanged()
    {
        GetHandler<Func<Task>>(nameof(WasteBinPresenceChanged))?.Invoke();
    }

    #endregion

    public event Func<string, Task> ReagentBarcodeScanned
    {
        add => Subscribe(nameof(ReagentBarcodeScanned), value);
        remove => Unsubscribe(nameof(ReagentBarcodeScanned), value);
    }

    private void OnReagentBarcodeScanned(string barcode)
    {
        GetHandler<Func<string, Task>>(nameof(ReagentBarcodeScanned))?.Invoke(barcode);
    }

    public event Action<string> UnknownBarcodeScanned
    {
        add => Subscribe(nameof(UnknownBarcodeScanned), value);
        remove => Unsubscribe(nameof(UnknownBarcodeScanned), value);
    }

    private void OnUnknownBarcodeScanned(string barcode)
    {
        GetHandler<Action<string>>(nameof(UnknownBarcodeScanned))?.Invoke(barcode);
    }

    public event Action<int, int, bool> BottlePresenceChanged
    {
        add => Subscribe(nameof(BottlePresenceChanged), value);
        remove => Unsubscribe(nameof(BottlePresenceChanged), value);
    }

    private void OnBottlePresenceChanged(int articleType, int position, bool isPresent)
    {
        GetHandler<Action<int, int, bool>>(nameof(BottlePresenceChanged))?.Invoke(articleType, position, isPresent);
    }

    public event Action IncorrectBarcodeScannedForBottles
    {
        add => Subscribe(nameof(IncorrectBarcodeScannedForBottles), value);
        remove => Unsubscribe(nameof(IncorrectBarcodeScannedForBottles), value);
    }

    private void OnIncorrectBarcodeScannedForBottles()
    {
        GetHandler<Action>(nameof(IncorrectBarcodeScannedForBottles))?.Invoke();
    }

    private void OnMaintenanceInfoChanged(bool maintenanceNeeded)
    {
        GetHandler<EventHandler<bool>>(nameof(MaintenanceInfoChanged))?.Invoke(this, maintenanceNeeded);
    }

    public event Action<string> ScannedBarcode
    {
        add => Subscribe(nameof(ScannedBarcode), value);
        remove => Unsubscribe(nameof(ScannedBarcode), value);
    }

    private void OnScannedBarcode(string scannedBarcode)
    {
        GetHandler<Action<string>>(nameof(ScannedBarcode))?.Invoke(scannedBarcode);
    }
}
