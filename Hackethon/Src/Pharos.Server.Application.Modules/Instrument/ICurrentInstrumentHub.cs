namespace Pharos.Server.Application.Instrument;

/// <summary>
/// The server-side service for sending SignalR notifications about the current instrument state changes.
/// </summary>
public interface ICurrentInstrumentServerHub
{
    Task HoodLockStateChanged(int instrumentId, int hoodLockState, bool isUnlockInProgress);
    Task InstrumentStateChanged(int instrumentId, int instrumentState);
    Task InstrumentEnvironmentChanged(int instrumentId);
    Task InstrumentNameChanged(int instrumentId, string name);
    Task InstrumentDeleted(int instrumentId);
    Task InstrumentEnabledChanged(int instrumentId, bool enabled);
    Task InstrumentMethodsChanged(int instrumentId);
    Task AssaySchedulerNextRunChanged(int instrumentId, int nextRunType, DateTimeOffset? nextRun);
    Task NumberOfTestsUpdated(int instrumentId, int numberOfTests);
    Task ProcessStatusUpdated(int instrumentId);
    Task TimeWhenNextStateReachedChanged(int instrumentId, DateTimeOffset? timeWhenNextStateReached);
    Task ShowMessageBox(int instrumentId, string message);
    Task SignalTowerStateChanged(int instrumentId, int lightState, int soundState);

    Task LeftSampleDrawerContentsChanged(int instrumentId);
    Task LeftSampleDrawerLockStateChanged(int instrumentId, int lockState, bool isUnlockInProgress);
    Task LeftSampleDrawerScanningStatusChanged(int instrumentId, bool isScanningOnGoing);

    Task RightSampleDrawerContentsChanged(int instrumentId);
    Task RightSampleDrawerLockStateChanged(int instrumentId, int lockState, bool isUnlockInProgress);
    Task RightSampleDrawerScanningStatusChanged(int instrumentId, bool isScanningOnGoing);

    Task SampleDrawerContentsReceived(int instrumentId, List<string> trayIds);

    Task ReagentDrawerContentsChanged(int instrumentId);
    Task ReagentDrawerLockStateChanged(int instrumentId, int lockState, bool isUnlockInProgress);
    Task ReagentDrawerScanningStatusChanged(int instrumentId, bool isScanningOnGoing);

    Task TaskCardInformationChanged(int instrumentId, int numberOfTaskCards, int highestTaskCardState);
    Task TaskCardCreatedOrChanged(int instrumentId, int taskCardType, int taskCardState);
    Task InstrumentFatalErrorChanged(int instrumentId, List<int> errorIds);
    Task UnlockDrawerAuthorizationRequest(string instrumentNumber, int instrumentId);
    Task BottleDrawerContentsChanged(int instrumentId);
    Task BottleDrawerLockStateChanged(int instrumentId, int isLocked);

    Task LiquidDrawerContentsChanged(int instrumentId);
    Task LiquidDrawerLockStateChanged(int instrumentId, int lockState);

    Task WasteDrawerContentsChanged(int instrumentId);
    Task WasteDrawerLockStateChanged(int instrumentId, int lockState, bool isUnlockInProgress);

    Task ReagentBarcodeScanned(int instrumentId, string barcode);
    Task UnknownBarcodeScanned(int instrumentId, string barcode);
    Task BottlePresenceChanged(int instrumentId, int articleType, int position, bool isPresent);
    Task LiquidPresenceChanged(int instrumentId);
    Task WasteBinPresenceChanged(int instrumentId);
    Task IncorrectBarcodeScannedForBottles(int instrumentId);
    Task ModuleInformationReceived(int instrumentId);
    Task MaintenanceInfoChanged(int instrumentId, bool maintenanceNeeded);
    Task SendScannedBarcode(int instrumentId, string scannedBarcode);
}
