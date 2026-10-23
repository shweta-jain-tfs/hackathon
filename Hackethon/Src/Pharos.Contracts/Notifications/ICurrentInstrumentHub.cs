namespace Pharos.Contracts.Notifications;

/// <summary>
/// The strongly typed server-to-client RPC interface for notifications bound to the "current instrument".
/// See remarks for more information.
/// </summary>
/// <remarks>
/// The "current instrument" term means that the client app is loaded on the instrument.
/// </remarks>
public interface ICurrentInstrumentHub : IHub
{
    Task HoodLockStateChanged(int hoodLockState, bool isUnlockInProgress);
    Task InstrumentStateChanged(int state);
    Task InstrumentEnvironmentChanged();
    Task InstrumentNameChanged(string name);
    Task InstrumentDeleted();
    Task InstrumentEnabledChanged(bool enabled);
    Task InstrumentMethodsChanged();
    Task TimeWhenNextStateReachedChanged(DateTimeOffset? timeWhenNextStateReached);
    Task AssaySchedulerNextRunChanged(int nextRunType, DateTimeOffset? nextRun);
    Task NumberOfTestsUpdated(int numberOfTests);
    Task ProcessStatusUpdated();
    Task ShowMessageBox(string message);
    Task SignalTowerStateChanged(int lightState, int soundState);

    Task LeftSampleDrawerContentsChanged();
    Task LeftSampleDrawerLockStateChanged(int lockState, bool isUnlockInProgress);
    Task LeftSampleDrawerScanningStatusChanged(bool isScanningOnGoing);

    Task RightSampleDrawerContentsChanged();
    Task RightSampleDrawerLockStateChanged(int lockState, bool isUnlockInProgress);
    Task RightSampleDrawerScanningStatusChanged(bool isScanningOnGoing);

    Task SampleDrawerContentsReceived(List<string> trayIds);

    Task ReagentDrawerContentsChanged();
    Task ReagentDrawerLockStateChanged(int lockState, bool isUnlockInProgress);
    Task ReagentDrawerScanningStatusChanged(bool isScanningOnGoing);

    Task TaskCardInformationChanged(int numberOfTaskCards, int highestTaskCardState);
    Task TaskCardCreatedOrChanged(int taskCardType, int taskCardState);

    Task InstrumentFatalError(List<int> errorIds);
    Task UnlockDrawerAuthorizationRequest(string instrumentNumber);

    Task BottleDrawerContentsChanged();
    Task BottleDrawerLockStateChanged(int lockState);

    Task LiquidDrawerContentsChanged();
    Task LiquidDrawerLockStateChanged(int lockState);

    Task WasteDrawerContentsChanged();
    Task WasteDrawerLockStateChanged(int lockState, bool isUnlockInProgress);

    Task ReagentBarcodeScanned(string barcode);
    Task UnknownBarcodeScanned(string barcode);
    Task BottlePresenceChanged(int articleType, int position, bool isPresent);
    Task LiquidPresenceChanged();
    Task WasteBinPresenceChanged();
    Task IncorrectBarcodeScannedForBottles();
    Task ModuleInformationReceived();
    Task MaintenanceInfoChanged(bool maintenanceNeeded);
    Task SendScannedBarcode(string scannedBarcode);
}
