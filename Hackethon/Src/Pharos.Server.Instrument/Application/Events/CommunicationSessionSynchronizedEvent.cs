using MediatR;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Instrument.Application.Commands.ControlSignalTower;
using Pharos.Server.Instrument.Application.Commands.ProcessInstrumentState;
using Pharos.Server.Instrument.Application.Interfaces;

namespace Pharos.Server.Instrument.Application.Events;

internal record CommunicationSessionSynchronizedEvent(CommunicationSession Session) : INotification;

internal class CommunicationSessionSynchronizedEventHandler(ISender sender) : INotificationHandler<CommunicationSessionSynchronizedEvent>
{
    public async Task Handle(CommunicationSessionSynchronizedEvent notification, CancellationToken cancellationToken)
    {
        var session = notification.Session;
        var instrumentNumber = session.InstrumentNumber;
        await sender.Send(new ProcessInstrumentStateCommand(instrumentNumber, session.SessionId, session.CachedInstrumentState!), cancellationToken);

        var state = await sender.Send(new GetSignalTowerStateQuery(instrumentNumber), cancellationToken);
        await sender.Send(new ControlSignalTowerCommand(instrumentNumber, state), cancellationToken);
    }
}