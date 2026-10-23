using MediatR;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Instrument.Application.Interfaces;

namespace Pharos.Server.Instrument.Application.Commands.SendQcScheduling;

internal record SendQcSchedulingCommand(string InstrumentNumber, Guid? SessionId) : IStateSynchronizationCommand;

internal class SendQcSchedulingCommandHandler(
    ICommunicationSessionManager sessionManager,
    IMediator mediator) : StateSynchronizationCommandHandlerBase<SendQcSchedulingCommand>(sessionManager, CommunicationSynchronizationState.QcSchedulingSent, mediator)
{
    protected override async Task DoHandle(SendQcSchedulingCommand request, CommunicationSession session, CancellationToken cancellationToken)
    {
        var schedule = await mediator.Send(new GetQualityControlSchedulingQuery(), cancellationToken);
        await session.Connection.SendQcSchedulingAsync(schedule, cancellationToken);
    }
}