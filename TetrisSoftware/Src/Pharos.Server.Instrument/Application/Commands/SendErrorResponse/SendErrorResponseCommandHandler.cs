using Pharos.Server.Application.Instrument;
using Pharos.Server.Instrument.Application.Interfaces;
using Tetris.Protocol.Instrument;

namespace Pharos.Server.Instrument.Application.Commands.SendErrorResponse;

internal class SendErrorResponseCommandHandler(ICommunicationSessionManager sessionManager) : InstrumentCommandHandlerBase<SendErrorResponseCommand>(sessionManager)
{
    protected override async Task Handle(SendErrorResponseCommand request, CommunicationSession session, CancellationToken cancellationToken)
    {
        await session.Connection.SendErrorResponseAsync(new ErrorResponse
        {
            ErrorId = request.ErrorId.ToString(),
            ResponseAction = request.ResponseAction
        }, cancellationToken);
    }
}