using Pharos.Server.Application.Instrument;
using Pharos.Server.Instrument.Application.Interfaces;
using Tetris.Protocol.Instrument;

namespace Pharos.Server.Instrument.Application.Commands.MeasureSampleVolume;

internal class MeasureSampleVolumeCommandHandler(ICommunicationSessionManager sessionManager)
    : InstrumentCommandHandlerBase<MeasureSampleVolumeCommand>(sessionManager)
{
    protected override Task Handle(MeasureSampleVolumeCommand request, CommunicationSession session, CancellationToken cancellationToken)
    {
        return session.Connection.SendPreMeasureSampleTubeVolumeRequest(new PreMeasureSampleTubeVolumeRequest
        {
            TubeId = request.TubeId,
            TubeTypeIndex = request.TubeTypeIndex
        }, cancellationToken);
    }
}
