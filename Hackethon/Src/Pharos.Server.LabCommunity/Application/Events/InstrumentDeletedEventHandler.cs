using MediatR;
using Pharos.Server.Application.Instrument;
using Pharos.Server.LabCommunity.Application.Interfaces.InstrumentStatus;

namespace Pharos.Server.LabCommunity.Application.Events;

internal sealed class InstrumentDeletedEventHandler(
    IInstrumentMaintenanceActivityCache cache) : INotificationHandler<InstrumentDeletedEvent>
{
    public Task Handle(InstrumentDeletedEvent notification, CancellationToken cancellationToken)
    {
        cache.Remove(notification.InstrumentNumber);
        return Task.CompletedTask;
    }
}
