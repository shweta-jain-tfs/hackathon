using Pharos.Contracts.Notifications;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Request;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Sample.Application.Helpers;
using Pharos.Server.Sample.Application.Interfaces;

namespace Pharos.Server.Sample.Application.Events;

internal sealed class RequestUpdatedEventHandler(
    IInstrumentSampleStorage sampleStorage,
    ISampleDbContext sampleDbContext,
    ISampleTrayHub sampleTrayHub,
    ISampleTubeHub sampleTubeHub,
    ISender sender) : INotificationHandler<RequestUpdatedEvent>
{
    private readonly HashSet<string> _modifiedSampleTrayIdList = new();
    private readonly HashSet<string> _tubesToBeMoved = new();
    private bool _traySummaryChanged;

    public async Task Handle(RequestUpdatedEvent notification, CancellationToken cancellationToken)
    {
        if (notification.TestOrders.Count == 0)
        {
            return;
        }

        await UpdateTubesInSampleStorage(notification, cancellationToken);

        await UpdateSampleTubesInDatabase(notification, cancellationToken);

        if (_traySummaryChanged)
        {
            await sampleTrayHub.FreePositionAndErrorCountUpdated();
        }

        if (_modifiedSampleTrayIdList.Count > 0)
        {
            await sampleTrayHub.PositionStatusUpdated(_modifiedSampleTrayIdList.ToList());
        }

        // Send move commands for completed and partly completed tubes in internal storage- this happens for a tube with pending reflex but the result for the reflex test is rejected
        foreach (var tubeId in _tubesToBeMoved)
        {
            var tube = sampleStorage.GetTube(tubeId);
            if (tube?.InstrumentNumber != null)
            {
                await sender.Send(new SendMoveSampleQcCommand(tube.InstrumentNumber, tubeId, Guid.NewGuid(), false, false), cancellationToken);
            }
        }
    }

    private async Task UpdateSampleTubesInDatabase(RequestUpdatedEvent notification, CancellationToken cancellationToken)
    {
        // Fetch tubes from the database based on the RequestUid and ensure they are in trays
        var tubesInTrays = await sampleDbContext.SampleTubes
            .Where(tube => tube.SampleId == notification.RequestUid.SampleId && tube.RequestCreatedDate == notification.RequestUid.RequestCreatedDate
                && tube.TrayId != null && tube.TubePosition != null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var requestsToNotify = new HashSet<int>();

        foreach (var tube in tubesInTrays)
        {
            var testOrderStatues = notification.TestOrders.Where(x => x.TubeId == tube.TubeId || x.TubeId == null).ToList();
            var statusUpdated = tube.UpdateTubeCompletionStatus(testOrderStatues);
            if (!statusUpdated)
            {
                continue;
            }

            _modifiedSampleTrayIdList.Add(tube.TrayId!);

            var requestIdResponse = await sender.Send(new GetRequestIdQuery(tube.SampleId!, tube.RequestCreatedDate), cancellationToken);
            if (requestIdResponse.RequestExists)
            {
                requestsToNotify.Add(requestIdResponse.RequestId);
            }
        }

        if (sampleDbContext.HasChanges())
        {
            await sampleDbContext.SaveChangesAsync(cancellationToken);
        }

        foreach (var requestId in requestsToNotify)
        {
            await sampleTubeHub.SampleTubeUpdated(requestId);
        }
    }


    private async Task UpdateTubesInSampleStorage(RequestUpdatedEvent notification, CancellationToken cancellationToken)
    {
        var instrumentsWithUpdatedTubeStatus = new HashSet<string>();
        var requestsToNotify = new HashSet<int>();
        foreach (var item in notification.TestOrders)
        {
            if (item.TubeId == null)
            {
                continue;
            }

            var tube = sampleStorage.GetTube(item.TubeId);
            if (tube == null)
            {
                continue;
            }

            var testOrderStatues = notification.TestOrders.Where(x => x.TubeId == item.TubeId || x.TubeId == null).ToList();
            tube.RequestCreatedDate = notification.RequestUid.RequestCreatedDate;
            var statusUpdated = tube.UpdateTubeCompletionStatus(testOrderStatues);
            if (!statusUpdated)
            {
                continue;
            }

            instrumentsWithUpdatedTubeStatus.Add(tube.InstrumentNumber!);

            if (!string.IsNullOrEmpty(tube.TrayId))
            {
                _modifiedSampleTrayIdList.Add(tube.TrayId);
            }

            // Store tubes in internal storage that has Completed or Partly Completed status- these have to be moved- this happens for a tube with pending reflex but the result for the reflex test is rejected
            if (tube.Area == SampleArea.InternalStorage && (tube.Status == SampleTubeStatus.Completed || tube.Status == SampleTubeStatus.PartlyCompleted))
            {
                _tubesToBeMoved.Add(item.TubeId);
            }

            var requestIdResponse = await sender.Send(new GetRequestIdQuery(tube.SampleId!, tube.RequestCreatedDate), cancellationToken);
            if (requestIdResponse.RequestExists)
            {
                requestsToNotify.Add(requestIdResponse.RequestId);
            }
        }

        foreach (var requestId in requestsToNotify)
        {
            await sampleTubeHub.SampleTubeUpdated(requestId);
        }

        foreach (var instrumentNumber in instrumentsWithUpdatedTubeStatus)
        {
            // Update tray summaries for the instrument when missing test order tubes status is updated
            _traySummaryChanged |= sampleStorage.UpdateTraysSummary(instrumentNumber);
            await UpdateTaskCardsForInstruments(instrumentNumber, cancellationToken);
        }
    }

    private async Task UpdateTaskCardsForInstruments(string instrumentNumber, CancellationToken cancellationToken)
    {
        var tubesInDrawer = sampleStorage.TubesAsQueryable(instrumentNumber)
                                .Where(x => x.Area == SampleArea.SampleDrawer1 || x.Area == SampleArea.SampleDrawer2)
                                .ToList();

        var hasPartlyCompletedTubes = tubesInDrawer.Any(x => x.Status == SampleTubeStatus.PartlyCompleted
                                                            && x.Error == SampleTubeError.None);

        var taskCardExists = await sender.Send(new DoesTaskCardExistQuery(TaskCardType.SamplePartlyCompleted, instrumentNumber), cancellationToken);

        if (hasPartlyCompletedTubes)
        {
            if (!taskCardExists)
            {
                await sender.Send(new TaskCardCommand(TaskCardType.SamplePartlyCompleted,
                                    [
                                        new EscalationRule(TaskCardState.Hidden, TimeSpan.FromMinutes(0)),
                                        new EscalationRule(TaskCardState.Warning, TimeSpan.FromSeconds(3))
                                    ],
                                    instrumentNumber), cancellationToken);
            }
        }
        else
        {
            await sender.Send(new RemoveTaskCardCommand(TaskCardType.SamplePartlyCompleted, instrumentNumber), cancellationToken);
        }

        var hasErrorTubes = tubesInDrawer.Any(x => x.Error != SampleTubeError.None);
        if (!hasErrorTubes)
        {
            await sender.Send(new RemoveTaskCardCommand(TaskCardType.SampleTubeError, instrumentNumber), cancellationToken);
        }
    }
}