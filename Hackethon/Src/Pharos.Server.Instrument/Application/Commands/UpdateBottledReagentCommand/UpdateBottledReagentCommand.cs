using Pharos.Server.Application.Instrument;
using Pharos.Server.Instrument.Application.Commands;
using Pharos.Server.Instrument.Application.Interfaces;

internal class UpdateBottledReagentCommandHandler(ICommunicationSessionManager sessionManager) : InstrumentCommandHandlerBase<UpdateBottledReagentCommand>(sessionManager)
{
    protected override Task Handle(UpdateBottledReagentCommand request, CommunicationSession session, CancellationToken cancellationToken)
    {
        return session.Connection.UpdateBottledReagentAsync(request.UpdateBottledReagent, cancellationToken);
    }
}