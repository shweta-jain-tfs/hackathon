using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Pharos.Contracts;
using Pharos.Contracts.Notifications;
using Pharos.Server.Infrastructure.Hubs;

namespace Pharos.Server.Instrument.Infrastructure.Hubs;

[Authorize]
public class InstrumentHub(
    IHubContext<InstrumentHub, IInstrumentHub> hubContext,
    ILogger<InstrumentHub> logger) : EntityHub<InstrumentHub, IInstrumentHub>(hubContext, logger), IInstrumentHub
{
    protected override string DefaultGroup => "instrument_state_default";

    public Task AssaySchedulerChanged(int instrumentId, int assaySchedulerId, bool assaySchedulerIsActive, DateTimeOffset? assaySchedulerPausedUntilDate)
    {
        return HubContext.Clients.Group(DefaultGroup).AssaySchedulerChanged(instrumentId, assaySchedulerId, assaySchedulerIsActive, assaySchedulerPausedUntilDate);
    }

    public Task AssaySchedulerDayChanged(int instrumentId, int assaySchedulerId, AssaySchedulerDayDto assaySchedulerDay, bool anyDayHasTime)
    {
        return HubContext.Clients.Group(DefaultGroup).AssaySchedulerDayChanged(instrumentId, assaySchedulerId, assaySchedulerDay, anyDayHasTime);
    }

    public Task AssaySchedulerNextRunChanged(int instrumentId, DateTimeOffset? nextRun, int nextRunType)
    {
        return HubContext.Clients.Group(DefaultGroup).AssaySchedulerNextRunChanged(instrumentId, nextRun, nextRunType);
    }

    public Task InstrumentStateChanged(int instrumentId, int instrumentState)
    {
        return HubContext.Clients.Group(DefaultGroup).InstrumentStateChanged(instrumentId, instrumentState);
    }

    public Task TimeWhenNextStateReachedChanged(int instrumentId, DateTimeOffset? timeWhenNextStateReached)
    {
        return HubContext.Clients.Group(DefaultGroup).TimeWhenNextStateReachedChanged(instrumentId, timeWhenNextStateReached);
    }

    public Task InstrumentTechnologiesChanged(int instrumentId)
    {
        return HubContext.Clients.Group(DefaultGroup).InstrumentTechnologiesChanged(instrumentId);
    }

    public Task NumberOfTestsUpdated(int instrumentId, int numberOfTests)
    {
        return HubContext.Clients.Group(DefaultGroup).NumberOfTestsUpdated(instrumentId, numberOfTests);
    }

    public Task SignalTowerStateChanged(int instrumentId, int signalTowerState)
    {
        return HubContext.Clients.Group(DefaultGroup).SignalTowerStateChanged(instrumentId, signalTowerState);
    }
}
