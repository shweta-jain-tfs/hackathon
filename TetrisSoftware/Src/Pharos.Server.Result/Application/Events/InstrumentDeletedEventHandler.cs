using MediatR;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Result.Application.Commands;

namespace Pharos.Server.Result.Application.Events;

internal sealed class InstrumentDeletedEventHandler(ISender sender) : INotificationHandler<InstrumentDeletedEvent>
{
    public async Task Handle(InstrumentDeletedEvent notification, CancellationToken cancellationToken)
    {
        var command = new DeleteResultsForInstrumentCommand(notification.InstrumentNumber);
        await sender.Send(command, cancellationToken);
    }
}