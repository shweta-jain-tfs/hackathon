using Pharos.Contracts.Notifications;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Sample;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Sample.Application.Interfaces;

namespace Pharos.Server.Sample.Application.Commands;

internal class UnloadSampleTubesCommandHandler(ISender sender, IInstrumentSampleStorage sampleStorage, ISampleTubeHub sampleTubeHub) : IRequestHandler<UnloadSampleTubesCommand>
{
    public async Task Handle(UnloadSampleTubesCommand request, CancellationToken cancellationToken)
    {
        // TODO, The OperationId needs to be saved somewhere to be able to cancel the operation if needed

        var instrumentsToUpdate = new HashSet<string>();
        var shouldNotifyRequest = false;
        var tubes = sampleStorage.GetTubesBySampleId(request.RequestUid.SampleId, includeTubesWithError: true);
        foreach (var tube in tubes)
        {
            if (tube != null && tube.InstrumentNumber != null && tube.TubeId != null)
            {
                var operationId = Guid.NewGuid();
                await sender.Send(new SendMoveSampleQcCommand(tube.InstrumentNumber, tube.TubeId, operationId, false, false), cancellationToken);

                sampleStorage.AddOrUpdateTube(tube, oldTube =>
                {
                    oldTube.UnloadRequested = true;
                    return oldTube;
                });

                shouldNotifyRequest = true;
                instrumentsToUpdate.Add(tube.InstrumentNumber);
            }
        }

        if (shouldNotifyRequest)
        {
            await sampleTubeHub.SampleTubeUpdated(request.RequestId);
        }

        foreach (var instrument in instrumentsToUpdate)
        {
            await HandleRemoveSamplesFromDrawerTaskCard(instrument, cancellationToken);
        }
    }

    private async Task HandleRemoveSamplesFromDrawerTaskCard(string instrumentNumber, CancellationToken cancellationToken)
    {
        var hasSampleWithUnloadRequestedStatus = sampleStorage.TubesAsQueryable(instrumentNumber)
                                           .Any(x => IsSampleDrawerArea(x.Area) && x.UnloadRequested == true);

        var taskCardExists = await sender.Send(new DoesTaskCardExistQuery(TaskCardType.RemoveSamplesFromDrawer, instrumentNumber), cancellationToken);
        if (hasSampleWithUnloadRequestedStatus)
        {
            if (!taskCardExists)
            {
                await sender.Send(new TaskCardCommand(TaskCardType.RemoveSamplesFromDrawer, TaskCardState.Information, instrumentNumber), cancellationToken);
            }
        }
    }

    private static bool IsSampleDrawerArea(SampleArea area)
    {
        return area is SampleArea.SampleDrawer1 or SampleArea.SampleDrawer2;
    }
}