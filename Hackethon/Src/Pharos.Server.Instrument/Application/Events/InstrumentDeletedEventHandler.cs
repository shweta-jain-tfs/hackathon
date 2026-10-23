using MediatR;
using Pharos.Contracts.Notifications;
using Pharos.Server.Application.Instrument;

namespace Pharos.Server.Instrument.Application.Events;

internal class InstrumentDeletedEventHandler(
    IInstrumentHub instrumentHub,
    ICurrentInstrumentServerHub currentInstrumentHub) : INotificationHandler<InstrumentDeletedEvent>
{
    public async Task Handle(InstrumentDeletedEvent notification, CancellationToken cancellationToken)
    {
        await instrumentHub.Deleted(notification.EntityId);
        await currentInstrumentHub.InstrumentDeleted(notification.EntityId);
    }
}