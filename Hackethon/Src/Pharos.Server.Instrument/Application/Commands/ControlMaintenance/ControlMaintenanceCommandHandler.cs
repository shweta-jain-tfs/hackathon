using Pharos.Server.Application.Instrument;
using Pharos.Server.Instrument.Application.Interfaces;
using Tetris.Protocol.Instrument.Control;

namespace Pharos.Server.Instrument.Application.Commands.ControlMaintenance;

internal class ControlMaintenanceCommandHandler(ICommunicationSessionManager sessionManager)
    : InstrumentCommandHandlerBase<ControlMaintenanceCommand, ControlMaintenanceResponse>(sessionManager)
{
    protected override Task<ControlMaintenanceResponse> Handle(ControlMaintenanceCommand request, CommunicationSession session, CancellationToken cancellationToken)
    {
        return session.Connection.ControlMaintenanceAsync(request.Message, cancellationToken);
    }
}