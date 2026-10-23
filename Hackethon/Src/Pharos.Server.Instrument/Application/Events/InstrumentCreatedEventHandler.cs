using MediatR;
using Pharos.Contracts.Notifications;
using Pharos.Server.Application.Instrument;

namespace Pharos.Server.Instrument.Application.Events;

internal class InstrumentCreatedEventHandler(
    IInstrumentManager instrumentManager,
    IInstrumentHub instrumentHub) : INotificationHandler<InstrumentCreatedEvent>
{
    public Task Handle(InstrumentCreatedEvent notification, CancellationToken cancellationToken)
    {
        var entityId = (instrumentManager.GetInstrument(notification.InstrumentNumber)).Id;
        return instrumentHub.Created(entityId);
    }
}