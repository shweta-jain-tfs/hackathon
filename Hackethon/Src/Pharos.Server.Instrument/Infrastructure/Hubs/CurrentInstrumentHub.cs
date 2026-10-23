using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Pharos.Contracts.Notifications;
using Pharos.Contracts.Security;
using Pharos.Server.Application;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Infrastructure.Hubs;

namespace Pharos.Server.Instrument.Infrastructure.Hubs;

/// <summary>
/// For messages that relates to things that are visible on the instrument lock screen the authorize attribute 'Policies.InstrumentOnlyAllowAnonymous' is needed.
/// For the rest of the messages it is enough to use 'Policies.InstrumentOnly'.
/// </summary>

public class CurrentInstrumentHub : CurrentInstrumentHubBase<CurrentInstrumentHub, ICurrentInstrumentHub>, ICurrentInstrumentServerHub
{
    public CurrentInstrumentHub(
        ICurrentInstrumentService currentInstrumentService,
        IHubContext<CurrentInstrumentHub, ICurrentInstrumentHub> hubContext,
        ILogger<CurrentInstrumentHubBase<CurrentInstrumentHub, ICurrentInstrumentHub>> logger)
        : base(currentInstrumentService, hubContext, logger)
    {
    }

    [Authorize(Policies.InstrumentOnly)]
    public Task LeftSampleDrawerLockStateChanged(int instrumentId, int lockState, bool isUnlockInProgress)
    {
        return GetClientGroup(instrumentId).LeftSampleDrawerLockStateChanged(lockState, isUnlockInProgress);
    }

    [Authorize(Policies.InstrumentOnly)]
    public Task RightSampleDrawerLockStateChanged(int instrumentId, int lockState, bool isUnlockInProgress)
    {
        return GetClientGroup(instrumentId).RightSampleDrawerLockStateChanged(lockState, isUnlockInProgress);
    }

    [Authorize(Policies.InstrumentOnly)]
    public Task ReagentDrawerLockStateChanged(int instrumentId, int lockState, bool isUnlockInProgress)
    {
        return GetClientGroup(instrumentId).ReagentDrawerLockStateChanged(lockState, isUnlockInProgress);
    }

    [Authorize(Policies.InstrumentOnly)]
    public Task TaskCardInformationChanged(int instrumentId, int numberOfTaskCards, int highestTaskCardState)
    {
        return GetClientGroup(instrumentId).TaskCardInformationChanged(numberOfTaskCards, highestTaskCardState);
    }

    [Authorize(Policies.InstrumentOnly)]
    public Task TaskCardCreatedOrChanged(int instrumentId, int taskCardType, int taskCardState)
    {
        return GetClientGroup(instrumentId).TaskCardCreatedOrChanged(taskCardType, taskCardState);
    }

    [Authorize(Policies.InstrumentOnly)]
    public Task InstrumentDeleted(int instrumentId)
    {
        return GetClientGroup(instrumentId).InstrumentDeleted();
    }

    [Authorize(Policies.InstrumentOnlyAllowAnonymous)]
    public Task InstrumentEnabledChanged(int instrumentId, bool enabled)
    {
        return GetClientGroup(instrumentId).InstrumentEnabledChanged(enabled);
    }

    [Authorize(Policies.InstrumentOnlyAllowAnonymous)]
    public Task InstrumentNameChanged(int instrumentId, string name)
    {
        return GetClientGroup(instrumentId).InstrumentNameChanged(name);
    }

    [Authorize(Policies.InstrumentOnly)]
    public Task HoodLockStateChanged(int instrumentId, int hoodLockState, bool isUnlockInProgress)
    {
        return GetClientGroup(instrumentId).HoodLockStateChanged(hoodLockState, isUnlockInProgress);
    }

    [Authorize(Policies.InstrumentOnlyAllowAnonymous)]
    public Task InstrumentStateChanged(int instrumentId, int instrumentState)
    {
        return GetClientGroup(instrumentId).InstrumentStateChanged(instrumentState);
    }

    [Authorize(Policies.InstrumentOnly)]
    public Task InstrumentEnvironmentChanged(int instrumentId)
    {
        return GetClientGroup(instrumentId).InstrumentEnvironmentChanged();
    }

    [Authorize(Policies.InstrumentOnly)]
    public Task InstrumentMethodsChanged(int instrumentId)
    {
        return GetClientGroup(instrumentId).InstrumentMethodsChanged();
    }

    [Authorize(Policies.InstrumentOnly)]
    public Task ProcessStatusUpdated(int instrumentId)
    {
        return GetClientGroup(instrumentId).ProcessStatusUpdated();
    }

    [Authorize(Policies.InstrumentOnly)]
    public Task TimeWhenNextStateReachedChanged(int instrumentId, DateTimeOffset? timeWhenNextStateReached)
    {
        return GetClientGroup(instrumentId).TimeWhenNextStateReachedChanged(timeWhenNextStateReached);
    }

    [Authorize(Policies.InstrumentOnly)]
    public Task NumberOfTestsUpdated(int instrumentId, int numberOfTests)
    {
        return GetClientGroup(instrumentId).NumberOfTestsUpdated(numberOfTests);
    }

    [Authorize(Policies.InstrumentOnly)]
    public Task ShowMessageBox(int instrumentId, string message)
    {
        return GetClientGroup(instrumentId).ShowMessageBox(message);
    }

    [Authorize(Policies.InstrumentOnly)]
    public Task LeftSampleDrawerScanningStatusChanged(int instrumentId, bool isScanningOnGoing)
    {
        return GetClientGroup(instrumentId).LeftSampleDrawerScanningStatusChanged(isScanningOnGoing);
    }

    [Authorize(Policies.InstrumentOnly)]
    public Task RightSampleDrawerScanningStatusChanged(int instrumentId, bool isScanningOnGoing)
    {
        return GetClientGroup(instrumentId).RightSampleDrawerScanningStatusChanged(isScanningOnGoing);
    }

    [Authorize(Policies.InstrumentOnlyAllowAnonymous)]
    public Task SignalTowerStateChanged(int instrumentId, int lightState, int soundState)
    {
        return GetClientGroup(instrumentId).SignalTowerStateChanged(lightState, soundState);
    }

    [Authorize(Policies.InstrumentOnly)]
    public Task LeftSampleDrawerContentsChanged(int instrumentId)
    {
        return GetClientGroup(instrumentId).LeftSampleDrawerContentsChanged();
    }

    [Authorize(Policies.InstrumentOnly)]
    public Task RightSampleDrawerContentsChanged(int instrumentId)
    {
        return GetClientGroup(instrumentId).RightSampleDrawerContentsChanged();
    }

    [Authorize(Policies.InstrumentOnly)]
    public Task SampleDrawerContentsReceived(int instrumentId, List<string> trayIds)
    {
        return GetClientGroup(instrumentId).SampleDrawerContentsReceived(trayIds);
    }

    [Authorize(Policies.InstrumentOnly)]
    public Task ReagentDrawerScanningStatusChanged(int instrumentId, bool isScanningOnGoing)
    {
        return GetClientGroup(instrumentId).ReagentDrawerScanningStatusChanged(isScanningOnGoing);
    }

    [Authorize(Policies.InstrumentOnly)]
    public Task ReagentDrawerContentsChanged(int instrumentId)
    {
        return GetClientGroup(instrumentId).ReagentDrawerContentsChanged();
    }

    [Authorize(Policies.InstrumentOnly)]
    public Task InstrumentFatalErrorChanged(int instrumentId, List<int> errorIds)
    {
        return GetClientGroup(instrumentId).InstrumentFatalError(errorIds);
    }
    [Authorize(Policies.InstrumentOnlyAllowAnonymous)]
    public Task UnlockDrawerAuthorizationRequest(string instrumentNumber, int instrumentId)
    {
        return GetClientGroup(instrumentId).UnlockDrawerAuthorizationRequest(instrumentNumber);
    }

    [Authorize(Policies.InstrumentOnly)]
    public Task BottleDrawerContentsChanged(int instrumentId)
    {
        return GetClientGroup(instrumentId).BottleDrawerContentsChanged();
    }

    [Authorize(Policies.InstrumentOnly)]
    public Task BottleDrawerLockStateChanged(int instrumentId, int isLocked)
    {
        return GetClientGroup(instrumentId).BottleDrawerLockStateChanged(isLocked);
    }

    [Authorize(Policies.InstrumentOnly)]
    public Task LiquidDrawerContentsChanged(int instrumentId)
    {
        return GetClientGroup(instrumentId).LiquidDrawerContentsChanged();
    }

    [Authorize(Policies.InstrumentOnly)]
    public Task LiquidDrawerLockStateChanged(int instrumentId, int lockState)
    {
        return GetClientGroup(instrumentId).LiquidDrawerLockStateChanged(lockState);
    }

    [Authorize(Policies.InstrumentOnly)]
    public Task WasteDrawerContentsChanged(int instrumentId)
    {
        return GetClientGroup(instrumentId).WasteDrawerContentsChanged();
    }

    [Authorize(Policies.InstrumentOnly)]
    public Task WasteDrawerLockStateChanged(int instrumentId, int lockState, bool isUnlockInProgress)
    {
        return GetClientGroup(instrumentId).WasteDrawerLockStateChanged(lockState, isUnlockInProgress);
    }

    [Authorize(Policies.InstrumentOnly)]
    public Task ReagentBarcodeScanned(int instrumentId, string barcode)
    {
        return GetClientGroup(instrumentId).ReagentBarcodeScanned(barcode);
    }

    [Authorize(Policies.InstrumentOnly)]
    public Task UnknownBarcodeScanned(int instrumentId, string barcode)
    {
        return GetClientGroup(instrumentId).UnknownBarcodeScanned(barcode);
    }

    [Authorize(Policies.InstrumentOnly)]
    public Task BottlePresenceChanged(int instrumentId, int articleType, int position, bool isPresent)
    {
        return GetClientGroup(instrumentId).BottlePresenceChanged(articleType, position, isPresent);
    }

    [Authorize(Policies.InstrumentOnly)]
    public Task IncorrectBarcodeScannedForBottles(int instrumentId)
    {
        return GetClientGroup(instrumentId).IncorrectBarcodeScannedForBottles();
    }

    [Authorize(Policies.InstrumentOnly)]
    public Task ModuleInformationReceived(int instrumentId)
    {
        return GetClientGroup(instrumentId).ModuleInformationReceived();
    }

    [Authorize(Policies.InstrumentOnlyAllowAnonymous)]
    public Task MaintenanceInfoChanged(int instrumentId, bool maintenanceNeeded)
    {
        return GetClientGroup(instrumentId).MaintenanceInfoChanged(maintenanceNeeded);
    }

    [Authorize(Policies.InstrumentOnly)]
    public Task LiquidPresenceChanged(int instrumentId)
    {
        return GetClientGroup(instrumentId).LiquidPresenceChanged();
    }

    [Authorize(Policies.InstrumentOnly)]
    public Task WasteBinPresenceChanged(int instrumentId)
    {
        return GetClientGroup(instrumentId).WasteBinPresenceChanged();
    }

    [Authorize(Policies.InstrumentOnly)]
    public Task SendScannedBarcode(int instrumentId, string scannedBarcode)
    {
        return GetClientGroup(instrumentId).SendScannedBarcode(scannedBarcode);
    }

    [Authorize(Policies.InstrumentOnlyAllowAnonymous)]
    public Task AssaySchedulerNextRunChanged(int instrumentId, int nextRunType, DateTimeOffset? nextRun)
    {
        return GetClientGroup(instrumentId).AssaySchedulerNextRunChanged(nextRunType, nextRun);
    }
}
