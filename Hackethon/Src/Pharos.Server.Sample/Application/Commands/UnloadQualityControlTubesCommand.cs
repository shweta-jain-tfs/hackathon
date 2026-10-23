using Pharos.Contracts.Notifications;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Sample.Application.Interfaces;

namespace Pharos.Server.Sample.Application.Commands;

public record UnloadQualityControlTubesCommand(List<string> TubeIds) : IRequest;

internal class UnloadQualityControlTubesCommandHandler(ISender sender, IInstrumentSampleStorage sampleStorage, IQualityControlTubeHub qualityControlTubeHub) : IRequestHandler<UnloadQualityControlTubesCommand>
{
    public async Task Handle(UnloadQualityControlTubesCommand request, CancellationToken cancellationToken)
    {
        foreach (var tubeId in request.TubeIds)
        {
            var tube = sampleStorage.GetTube(tubeId);

            if (tube is not { SampleType: SampleType.QualityControl })
            {
                throw new InvalidOperationException($"Tube with ID {tubeId} not found.");
            }

            if(string.IsNullOrEmpty(tube.InstrumentNumber))
            {
                throw new InvalidOperationException($"Tube with ID {tubeId} does not have a valid InstrumentNumber.");
            }

            // TODO, The OperationId needs to be saved somewhere to be able to cancel the operation if needed
            var operationId = Guid.NewGuid();
            await sender.Send(new SendMoveSampleQcCommand(tube.InstrumentNumber, tube.TubeId!, operationId, false, false), cancellationToken);

            sampleStorage.AddOrUpdateTube(tube, oldTube =>
            {
                oldTube.UnloadRequested = true;
                return oldTube;
            });

            await qualityControlTubeHub.TubeUpdated(tubeId);
        }
    }
}