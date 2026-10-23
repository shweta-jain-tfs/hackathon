using Pharos.Server.Application.Instrument;
using Pharos.Server.Instrument.Application.Interfaces;

namespace Pharos.Server.Instrument.Application.Commands.UpdateTestOrder;

internal class DeleteTestOrderCommandHandler(ICommunicationSessionManager sessionManager)
    : InstrumentCommandHandlerBase<DeleteTestOrderCommand>(sessionManager)
{
    protected override bool ThrowIfNotAllowed => false;

    protected override Task Handle(DeleteTestOrderCommand request, CommunicationSession session, CancellationToken cancellationToken)
    {
        return session.Connection.DeleteTestOrderAsync(request.TestOrderRequest, cancellationToken);
    }
}