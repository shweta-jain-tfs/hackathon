using MediatR;
using Pharos.Contracts.Notifications;
using Pharos.Server.Application.Instrument;

namespace Pharos.Server.Instrument.Application.Events;

internal sealed class ProcessStatusUpdatedEventHandler(IInstrumentManager instrumentManager,
    IInstrumentHub instrumentHub,
    ICurrentInstrumentServerHub currentInstrumentHub) :
    INotificationHandler<ProcessStatusUpdatedEvent>

{
    public async Task Handle(ProcessStatusUpdatedEvent request, CancellationToken cancellationToken)
    {
        var instrument = (Domain.Instrument)instrumentManager.GetInstrument(request.InstrumentNumber);

        var newStatus = request.ProcessStatus;
        var oldStatus = instrument.ProcessStatus;

        // Update process status.
        instrument.ProcessStatus = newStatus;
        await currentInstrumentHub.ProcessStatusUpdated(instrument.Id);

        if (oldStatus == null || oldStatus.RemainingResults != newStatus.RemainingResults)
        {
            await currentInstrumentHub.NumberOfTestsUpdated(instrument.Id, newStatus.RemainingResults);
            await instrumentHub.NumberOfTestsUpdated(instrument.Id, newStatus.RemainingResults);
        }

        // Send the last remaining time to clients if the instrument is in a valid state.
        var (isValid, timeSource) = instrument.IsValidStateForProcessStatusTimeMessage();
        if (isValid && timeSource != Domain.Instrument.ProcessStatusTimeSource.None)
        {
            DateTimeOffset lastRemainingTime;
            if (timeSource == Domain.Instrument.ProcessStatusTimeSource.LastRemainingResultTime)
            {
                lastRemainingTime = newStatus.LastRemainingResultTime?.ToDateTimeOffset().ToLocalTime() ?? DateTimeOffset.MinValue;
            }
            else
            {
                lastRemainingTime = newStatus.LastRemainingStartTime?.ToDateTimeOffset().ToLocalTime() ?? DateTimeOffset.MinValue;
            }

            if (instrument.TimeWhenNextStateReached != lastRemainingTime)
            {
                instrument.TimeWhenNextStateReached = lastRemainingTime;

                // Send time (when state is expected to change) to clients.
                await currentInstrumentHub.TimeWhenNextStateReachedChanged(instrument.Id, instrument.TimeWhenNextStateReached);
                await instrumentHub.TimeWhenNextStateReachedChanged(instrument.Id, instrument.TimeWhenNextStateReached);
            }
        }
    }
}