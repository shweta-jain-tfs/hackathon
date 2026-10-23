using Pharos.Server.Application.Instrument;
using Pharos.Server.Instrument.Application.Interfaces;
using Tetris.Protocol.Instrument.Move;

namespace Pharos.Server.Instrument.Application.Commands.SendMoveSampleQc;

internal class SendMoveSampleQcCommandHandler(ICommunicationSessionManager sessionManager) : InstrumentCommandHandlerBase<SendMoveSampleQcCommand>(sessionManager)
{
    protected override bool ThrowIfNotAllowed => false;

    protected override Task Handle(SendMoveSampleQcCommand request, CommunicationSession session, CancellationToken cancellationToken)
    {
        return session.Connection.SendMoveSampleQcRequestAsync(new MoveSampleQcRequest
        {
            TubeId = request.TubeId,
            OperationId = request.OperationId.ToString(),
            ForcedMove = request.ForcedMove,
            Abort = request.Abort
        }, cancellationToken);
    }
}