using Pharos.Contracts.Notifications;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Lis;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Sample.Application.Interfaces;

namespace Pharos.Server.Sample.Application.Events;

internal class MissingTestOrdersEventHandler(
    IInstrumentSampleStorage sampleStorage,
    ISender sender,
    ISampleDbContext sampleDbContext,
    ISampleTrayHub sampleTrayHub) : INotificationHandler<MissingTestOrdersEvent>
{
    public async Task Handle(MissingTestOrdersEvent notification, CancellationToken cancellationToken)
    {
        var traysToUpdate = new HashSet<string>();
        var sampleId = notification.SampleId;
        if (string.IsNullOrEmpty(sampleId))
        {
            return;
        }

        var tubes = sampleStorage.GetTubesBySampleId(sampleId, includeTubesWithError: true);
        if (tubes == null || tubes.Count == 0)
        {
            return;
        }

        var instrumentsToUpdate = new HashSet<string>();

        foreach (var tube in tubes)
        {
            sampleStorage.AddOrUpdateTube(tube, oldTube =>
            {
                oldTube.InternalError = SampleTubeError.MissingTestOrders;
                return oldTube;
            });

            var instrumentNumber = tube.InstrumentNumber!;

            // Tube in internal storage, move to drawer
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

        // Update tray summaries and raise task cards for tubes in drawer
        foreach (var instrumentNumber in instrumentsToUpdate)
        {
            sampleStorage.UpdateTraysSummary(instrumentNumber);

            await sender.Send(new TaskCardCommand(TaskCardType.SampleTubeError, TaskCardState.Warning, instrumentNumber), cancellationToken);
        }

        await UpdateTubeErrorsInDatabase(sampleId, traysToUpdate, cancellationToken);

        // Notify UI to refresh tray positions and error counts for tubes in closed drawers
        if (traysToUpdate.Count > 0)
        {
            await sampleTrayHub.PositionStatusUpdated(traysToUpdate.ToList());
            await sampleTrayHub.FreePositionAndErrorCountUpdated();
        }
    }

    private async Task UpdateTubeErrorsInDatabase(string sampleId, HashSet<string> traysToUpdate, CancellationToken cancellationToken)
    {
        // Tubes will only be in DB if drawer was opened/unloaded before this event fired
        // Tubes in closed drawers are already handled in the main processing loop above
        var sampleTubes = await sampleDbContext.SampleTubes
            .Where(x => x.TubeId != null && x.SampleId == sampleId && x.RequestCreatedDate == null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (sampleTubes.Count == 0)
        {
            return;
        }

        foreach (var tube in sampleTubes)
        {
            tube.InternalError = SampleTubeError.MissingTestOrders;
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