using MediatR;
using Pharos.Contracts.Notifications;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Instrument.Application.Services;

namespace Pharos.Server.Instrument.Application.Events;

internal record InstrumentStateChangedEvent(int InstrumentId, InstrumentState State, DateTimeOffset? TimeWhenNextStateReached = null) : INotification;

internal class InstrumentStateChangedEventHandler(
    IInstrumentManager instrumentManager,
    IInstrumentHub instrumentHub,
    ICurrentInstrumentServerHub currentInstrumentHub,
    ISender sender,
    IAutomaticPowerSaveScheduler automaticPowerSaveScheduler) : INotificationHandler<InstrumentStateChangedEvent>
{
    public async Task Handle(InstrumentStateChangedEvent notification, CancellationToken cancellationToken)
    {
        // Send status to clients.
        await currentInstrumentHub.InstrumentStateChanged(notification.InstrumentId, (int)notification.State);
        await instrumentHub.InstrumentStateChanged(notification.InstrumentId, (int)notification.State);

        // Send time (when state is expected to change) only for states that support state-message-driven timing.
        if (Domain.Instrument.IsValidStateForStateTimeMessage(notification.State))
        {
            await instrumentHub.TimeWhenNextStateReachedChanged(notification.InstrumentId, notification.TimeWhenNextStateReached);
            await currentInstrumentHub.TimeWhenNextStateReachedChanged(notification.InstrumentId, notification.TimeWhenNextStateReached);
        }

        await automaticPowerSaveScheduler.OnInstrumentStateChangedAsync(
            notification.InstrumentId,
            notification.State,
            cancellationToken);

        var instrument = instrumentManager.GetInstrument(notification.InstrumentId);

        if (instrument.Enabled && notification.State == InstrumentState.Offline)
        {
            await sender.Send(new TaskCardCommand(TaskCardType.InstrumentSoftwareOffline, TaskCardState.Error, instrument.SerialNumber), cancellationToken);
        }
        else
        {
            await sender.Send(new RemoveTaskCardCommand(TaskCardType.InstrumentSoftwareOffline, instrument.SerialNumber), cancellationToken);
        }
    }
}
