using Pharos.Server.Application.Instrument;
using Pharos.Server.Instrument.Application.Interfaces;

namespace Pharos.Server.Instrument.Application.Commands.SendUnloadReagentRequest;

internal class SendUnloadReagentRequestCommandHandler(ICommunicationSessionManager sessionManager) : InstrumentCommandHandlerBase<SendUnloadReagentRequestCommand>(sessionManager)
{
    protected override Task Handle(SendUnloadReagentRequestCommand request, CommunicationSession session, CancellationToken cancellationToken)
    {
        return session.Connection.SendUnloadReagentRequestAsync(request.UnloadReagents, cancellationToken);
    }
}