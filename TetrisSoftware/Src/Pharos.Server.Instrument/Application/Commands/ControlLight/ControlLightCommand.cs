using MediatR;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Instrument.Application.Interfaces;

namespace Pharos.Server.Instrument.Application.Commands.ControlLight;

internal record ControlLightCommand(string InstrumentNumber) : IRequest, IInstrumentCommand;

internal class ControlLightCommandHandler(ICommunicationSessionManager sessionManager) : InstrumentCommandHandlerBase<ControlLightCommand>(sessionManager)
{
    protected override Task Handle(ControlLightCommand request, CommunicationSession session, CancellationToken cancellationToken)
    {
        return session.Connection.ControlLightAsync(cancellationToken);
    }
}
