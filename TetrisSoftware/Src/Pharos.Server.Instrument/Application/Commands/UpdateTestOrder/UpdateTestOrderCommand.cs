using Pharos.Server.Application.Instrument;
using Pharos.Server.Instrument.Application.Interfaces;

namespace Pharos.Server.Instrument.Application.Commands.UpdateTestOrder;

internal class UpdateTestOrderCommandHandler(ICommunicationSessionManager sessionManager)
    : InstrumentCommandHandlerBase<UpdateTestOrderCommand, bool>(sessionManager)
{
    protected override bool ThrowIfNotAllowed => false;

    protected override async Task<bool> Handle(UpdateTestOrderCommand request, CommunicationSession session, CancellationToken cancellationToken)
    {
        await session.Connection.UpdateTestOrderAsync(request.TestOrderRequest, cancellationToken);
        return true;
    }
}