using Pharos.Contracts.Notifications;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Request;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Sample.Application.Interfaces;

namespace Pharos.Server.Sample.Application.Events;

internal class NoActiveMethodsForTubeEventHandler(
    IInstrumentSampleStorage sampleStorage,
    ISender sender,
    ISampleDbContext sampleDbContext,
    ISampleTrayHub sampleTrayHub) : INotificationHandler<NoActiveMethodsForTubeEvent>
{
    public async Task Handle(NoActiveMethodsForTubeEvent notification, CancellationToken cancellationToken)
    {
        var traysToUpdate = new HashSet<string>();
        var instrumentNumber = notification.InstrumentNumber;

        await UpdateTubeErrorsInInstrumentStorage(notification.TubeIdList, instrumentNumber, traysToUpdate, cancellationToken);

        await UpdateTubeErrorsInDatabase(notification.TubeIdList, traysToUpdate, cancellationToken);

        if (traysToUpdate.Count > 0)
        {
            await sampleTrayHub.PositionStatusUpdated(traysToUpdate.ToList());
            await sampleTrayHub.FreePositionAndErrorCountUpdated();
        }
    }

    private async Task UpdateTubeErrorsInInstrumentStorage(
        IReadOnlyList<string> tubeIds,
        string instrumentNumber,
        HashSet<string> traysToUpdate,
        CancellationToken cancellationToken)
    {
        var instrumentsToUpdate = new HashSet<string>();
        foreach (var tubeId in tubeIds)
        {
            var tube = sampleStorage.GetTube(tubeId);
            if (tube == null)
            {
                continue;
            }

            sampleStorage.AddOrUpdateTube(tube, oldTube =>
            {
                oldTube.InternalError = SampleTubeError.MethodsNotActive;
                return oldTube;
            });

            if (!string.IsNullOrEmpty(tube.TubeId) && tube.Area is SampleArea.InternalStorage)
            {
                await sender.Send(new SendMoveSampleQcCommand(instrumentNumber, tube.TubeId, Guid.NewGuid(), false, false), cancellationToken);
            }
            else if (tube.Area is SampleArea.SampleDrawer1 or SampleArea.SampleDrawer2)
            {
                instrumentsToUpdate.Add(instrumentNumber);

                if (!string.IsNullOrEmpty(tube.TrayId))
                {
                    traysToUpdate.Add(tube.TrayId);
                }
            }
        }

        foreach (var insrumentNumber in instrumentsToUpdate)
        {
            sampleStorage.UpdateTraysSummary(insrumentNumber);

            await sender.Send(new TaskCardCommand(TaskCardType.SampleTubeError, TaskCardState.Warning, insrumentNumber), cancellationToken);
        }
    }
    private async Task UpdateTubeErrorsInDatabase(IReadOnlyList<string> tubeIds, HashSet<string> traysToUpdate, CancellationToken cancellationToken)
    {
        var sampleTubes = await sampleDbContext.SampleTubes
            .Where(x => x.TubeId != null && tubeIds.Contains(x.TubeId) && x.RequestCreatedDate == null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (sampleTubes.Count == 0)
        {
            return;
        }

        foreach (var tube in sampleTubes)
        {
            tube.InternalError = SampleTubeError.MethodsNotActive;

            if (!string.IsNullOrEmpty(tube.TrayId))
            {
                traysToUpdate.Add(tube.TrayId);
            }
        }

        if (sampleDbContext.HasChanges())
        {
            await sampleDbContext.SaveChangesAsync(cancellationToken);
        }
    }
}