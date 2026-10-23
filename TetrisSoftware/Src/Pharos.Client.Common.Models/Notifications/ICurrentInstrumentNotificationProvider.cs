namespace Pharos.Client.Common.Models.Notifications;

public interface ICurrentInstrumentNotificationProvider : INotificationProvider
{
    event EventHandler<string> InstrumentNameChanged;
    event EventHandler InstrumentDeleted;
    event EventHandler<bool> InstrumentEnabledChanged;
    event Action<LockState, bool> HoodLockStateChanged;
    event Action<InstrumentState> InstrumentStateChanged;

    event Action<AssaySchedulerNextRunType, DateTimeOffset?> AssaySchedulerNextRunChanged;
    event EventHandler<int> NumberOfTestsUpdated;
    event Func<string, EventArgs, Task> ShowMessageBox;
    event EventHandler<SignalTower> SignalTowerStateChanged;
    event EventHandler InstrumentEnvironmentChanged;
    event EventHandler MethodsChanged;
    event EventHandler ProcessListUpdated;
    event EventHandler<DateTimeOffset?> TimeWhenNextStateReachedChanged;

    event Func<Task> LeftSampleDrawerContentsChanged;
    event Action<LockState, bool> LeftSampleDrawerLockStateChanged;
    event Action<bool> LeftSampleDrawerScanningStatusChanged;

    event Func<Task> RightSampleDrawerContentsChanged;
    event Action<LockState, bool> RightSampleDrawerLockStateChanged;
    event Action<bool> RightSampleDrawerScanningStatusChanged;

    event EventHandler<List<string>> SampleDrawerContentsReceived;

    event Func<Task> ReagentDrawerContentsUpdated;
    event Action<LockState, bool> ReagentDrawerLockStateChanged;
    event Action<bool> ReagentDrawerScanningStatusChanged;

    event Action<int, int> TaskCardInformationChanged;
    event Action<TaskCardType, TaskCardState> TaskCardCreatedOrChanged;

    event EventHandler<List<int>> InstrumentFatalErrorChanged;
    event EventHandler<string> UnlockDrawerAuthorizationRequest;

    event Func<Task> BottleDrawerContentsChanged;
    event Action<LockState> BottleDrawerLockStateChanged;

    event Func<Task> LiquidDrawerContentsChanged;
    event Action<LockState> LiquidDrawerLockStateChanged;

    event Func<Task> WasteDrawerContentsChanged;
    event Action<LockState, bool> WasteDrawerLockStateChanged;
    event Func<Task> WasteBinPresenceChanged;

    event Func<string, Task> ReagentBarcodeScanned;
    event Action<string> UnknownBarcodeScanned;

    event Action<int, int, bool> BottlePresenceChanged;
    event Func<Task> LiquidPresenceChanged;
    event Action IncorrectBarcodeScannedForBottles;
    event EventHandler ModuleInformationReceived;

    event EventHandler<bool> MaintenanceInfoChanged;

    event Action<string> ScannedBarcode;
}
