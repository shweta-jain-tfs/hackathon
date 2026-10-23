using Microsoft.Extensions.Options;
using Pharos.Contracts.Notifications;
using Pharos.Server.Application.Request;
using Pharos.Server.Application.Settings.Sample;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Sample.Application.Interfaces;
using Pharos.Server.Sample.Domain;

namespace Pharos.Server.Sample.Application.Events;

internal class RequestCreatedEventHandler(
    IInstrumentSampleStorage sampleStorage,
    ISampleDbContext sampleDbContext,
    ISampleTrayHub sampleTrayHub,
    ISender sender,
    IOptionsMonitor<SamplesSettings> samplesSettings)
    : INotificationHandler<RequestCreatedEvent>
{
    public async Task Handle(RequestCreatedEvent notification, CancellationToken cancellationToken)
    {
        var traysToUpdate = new HashSet<string>();
        var instrumentsToUpdate = new HashSet<string>();

        //update tubes in the in-memory sample storage
        var tubesInStorage = sampleStorage.GetTubesBySampleId(notification.RequestUid.SampleId);
        foreach (var tube in tubesInStorage)
        {
            if (tube.RequestCreatedDate == null)
            {
                tube.RequestCreatedDate = notification.RequestUid.RequestCreatedDate;
                ClearMissingTestOrdersError(notification, tube, traysToUpdate, instrumentsToUpdate);
            }
        }

        // assume that all tubes with this sample id that do not yet have a request created date belong to the same request
        var sampleTubes = await GetUnassignedSampleTubes(notification.RequestUid.SampleId, cancellationToken);
        foreach (var sampleTube in sampleTubes)
        {
            sampleTube.RequestCreatedDate = notification.RequestUid.RequestCreatedDate;
            ClearMissingTestOrdersError(notification, sampleTube, traysToUpdate, instrumentsToUpdate);
        }

        await sampleDbContext.SaveChangesAsync(cancellationToken);

        // Update tray summaries and check task cards for instruments with cleared errors
        foreach (var instrumentNumber in instrumentsToUpdate)
        {
            sampleStorage.UpdateTraysSummary(instrumentNumber);
            await HandleSampleTubeErrorTaskCard(instrumentNumber, cancellationToken);
        }

        await NotifyClient(traysToUpdate);
    }

    private async Task<List<SampleTube>> GetUnassignedSampleTubes(string sampleId, CancellationToken cancellationToken)
    {
        var query = sampleDbContext.SampleTubes.Where(x => x.RequestCreatedDate == null);
        var caseInsensitive = samplesSettings.CurrentValue.CaseInsensitiveSampleId;
        List<SampleTube> candidates;

        if (caseInsensitive)
        {
            var sampleIdUpper = sampleId.ToUpperInvariant();
#pragma warning disable CA1862
            candidates = await query
                .Where(x => x.SampleId != null && x.SampleId.ToUpper() == sampleIdUpper)
                .ToListAsync(cancellationToken);
#pragma warning restore CA1862
        }
        else
        {
            candidates = await query
                .Where(x => x.SampleId == sampleId)
                .ToListAsync(cancellationToken);
        }

        var comparison = caseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return candidates.Where(x => string.Equals(x.SampleId, sampleId, comparison)).ToList();
    }

    private static void ClearMissingTestOrdersError(
    RequestCreatedEvent notification,
    SampleTube tube,
    HashSet<string> traysToUpdate,
    HashSet<string> instrumentsToUpdate)
    {
        var hasTestOrders = notification.TestOrders.Count > 0;
        if (hasTestOrders && tube.InternalError == SampleTubeError.MissingTestOrders)
        {
            tube.InternalError = SampleTubeError.None;

            if (tube.InstrumentNumber != null)
            {
                instrumentsToUpdate.Add(tube.InstrumentNumber);
            }

            if (!string.IsNullOrEmpty(tube.TrayId))
            {
                traysToUpdate.Add(tube.TrayId);
            }
        }
    }

    private async Task HandleSampleTubeErrorTaskCard(string instrumentNumber, CancellationToken cancellationToken)
    {
        var tubesInDrawer = sampleStorage.TubesAsQueryable(instrumentNumber)
            .Where(x => x.Area == SampleArea.SampleDrawer1 || x.Area == SampleArea.SampleDrawer2)
            .ToList();

        var hasErrorTubes = tubesInDrawer.Any(x => x.Error != SampleTubeError.None);
        if (!hasErrorTubes)
        {
            await sender.Send(new RemoveTaskCardCommand(TaskCardType.SampleTubeError, instrumentNumber), cancellationToken);
        }
    }

    private async Task NotifyClient(HashSet<string> traysToUpdate)
    {
        if (traysToUpdate.Count > 0)
        {
            await sampleTrayHub.PositionStatusUpdated(traysToUpdate.ToList());
            await sampleTrayHub.FreePositionAndErrorCountUpdated();
        }
    }
}
