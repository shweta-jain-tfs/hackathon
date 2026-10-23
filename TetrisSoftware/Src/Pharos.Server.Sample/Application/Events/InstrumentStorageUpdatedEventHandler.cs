using Pharos.Contracts.Notifications;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Request;
using Pharos.Server.Application.Sample;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Sample.Application.Helpers;
using Pharos.Server.Sample.Application.Interfaces;
using Pharos.Server.Sample.Application.Mapping;
using Tetris.Protocol.Common;
using Tetris.Protocol.Pharos.Storage;
using SampleTube = Pharos.Server.Sample.Domain.SampleTube;

namespace Pharos.Server.Sample.Application.Events;

internal class InstrumentStorageUpdatedEventHandler(
    IInstrumentSampleStorage sampleStorage,
    IInstrumentManager instrumentManager,
    ICurrentInstrumentServerHub currentInstrumentHub,
    ISender sender,
    IPublisher publisher,
    ISampleTrayHub sampleTrayHub, ISampleTubeHub sampleTubeHub,
    ISampleQcIdProvider sampleQcIdProvider) : INotificationHandler<InstrumentStorageUpdatedEvent>
{
    private bool _leftSampleDrawerChanged;
    private bool _rightSampleDrawerChanged;
    private bool _traySummaryChanged;
    private readonly HashSet<string> _modifiedSampleTrayIdList = [];

    public async Task Handle(InstrumentStorageUpdatedEvent notification, CancellationToken cancellationToken)
    {
        var instrumentNumber = notification.InstrumentNumber;
        var newStorage = notification.UpdatedStorage;

        if (newStorage.IsFullMessage)
        {
            sampleStorage.Clear(instrumentNumber);
        }

        UpdateTrays(instrumentNumber, newStorage);

        var unloadedTubes = new List<UnloadedTube>();
        var requestsToNotify = new HashSet<int>();
        await UpdateTubes(instrumentNumber, newStorage, unloadedTubes, requestsToNotify);

        _traySummaryChanged |= sampleStorage.UpdateTraysSummary(instrumentNumber);

        var instrumentId = instrumentManager.GetInstrument(notification.InstrumentNumber).Id;

        if (instrumentId > 0)
        {
            if (_leftSampleDrawerChanged)
            {
                await currentInstrumentHub.LeftSampleDrawerContentsChanged(instrumentId);
            }
            if (_rightSampleDrawerChanged)
            {
                await currentInstrumentHub.RightSampleDrawerContentsChanged(instrumentId);
            }
        }

        if (_traySummaryChanged)
        {
            await sampleTrayHub.FreePositionAndErrorCountUpdated();
        }

        if (_modifiedSampleTrayIdList.Count > 0)
        {
            await sampleTrayHub.PositionStatusUpdated(_modifiedSampleTrayIdList.ToList());
        }

        await HandleTaskCards(instrumentNumber, cancellationToken);

        if (unloadedTubes.Count > 0)
        {
            await publisher.Publish(new TubesUnloadedEvent(notification.InstrumentNumber, unloadedTubes), cancellationToken);
        }

        foreach (var requestId in requestsToNotify)
        {
            await sampleTubeHub.SampleTubeUpdated(requestId);
        }
    }

    private void UpdateTrays(string instrumentNumber, InstrumentStorage newStorage)
    {
        foreach (var sampleDrawer in newStorage.SampleDrawers)
        {
            var drawer = sampleDrawer.ToSampleDrawer();
            for (var i = 0; i < drawer.Trays.Count && i < sampleDrawer.Trays.Count; i++)
            {
                if (string.IsNullOrEmpty(drawer.Trays[i].Id))
                {
                    drawer.Trays[i].Id = sampleDrawer.Trays[i].TrayBarcode;
                }
            }

            var oldDrawer = sampleStorage.GetDrawer(instrumentNumber, drawer.DrawerType);
            foreach (var tray in oldDrawer.Trays)
            {
                _modifiedSampleTrayIdList.Add(tray.Id);
            }

            sampleStorage.UpdateDrawer(instrumentNumber, drawer);

            foreach (var tray in drawer.Trays)
            {
                _modifiedSampleTrayIdList.Add(tray.Id);
            }

            _traySummaryChanged = true;

            _leftSampleDrawerChanged |= drawer.DrawerType == DrawerType.Sample1;
            _rightSampleDrawerChanged |= drawer.DrawerType == DrawerType.Sample2;
        }
    }

    private async Task UpdateTubes(string instrumentNumber, InstrumentStorage newStorage, List<UnloadedTube> unloadedTubes, HashSet<int> requestsToNotify)
    {
        //TODO: Revisit whether unloaded tubes need to be handled for full message. Currently, this is handled for partial messages.
        if (newStorage.IsFullMessage)
        {
            var sampleIdList = new HashSet<string>();
            var identifiedTubes = new List<SampleTube>();
            // add sample tubes from the message
            foreach (var sample in newStorage.Samples)
            {
                var sampleTube = sample.ToSampleTube();
                // SampleIdSuffix is not available in the InstrumentStorage message, so we need to retrieve it from SampleQcIdProvider using the tube barcode
                sampleTube.SampleIdSuffix = await GetSampleIdSuffix(sampleTube.TubeBarcode, instrumentNumber);

                if (sample.SampleQcStatus == EnumSampleQcTubeStatus.SampleQcTubeStatusNotNeeded)
                {
                    sampleTube.InternalError = sample.SampleType == EnumSampleType.Sample
                        ? SampleTubeError.MethodsNotActive
                        : SampleTubeError.QcNotNeeded;
                }

                sampleTube.InstrumentNumber = instrumentNumber;
                sampleStorage.AddOrUpdateTube(sampleTube, _ => sampleTube);
                var requestId = await GetRequestIdForSampleTube(sampleTube);
                if (requestId.HasValue)
                {
                    requestsToNotify.Add(requestId.Value);
                }

                UpdateDrawerModificationFlags(sample, previousTube: null);

                if (string.IsNullOrEmpty(sampleTube.SampleId))
                {
                    sampleTube.Status = SampleTubeStatus.NotIdentified;
                }
                else if (sample.SampleType != EnumSampleType.QualityControl)
                {
                    sampleTube.Status = SampleTubeStatus.InProgress;

                    // We collect sample IDs to restore tube statuses
                    sampleIdList.Add(sampleTube.SampleId);
                    identifiedTubes.Add(sampleTube);
                }
            }

            if (sampleIdList.Count > 0)
            {
                var testOrderStates = await sender.Send(new GetTestOrderStatesQuery(sampleIdList), CancellationToken.None);
                foreach (var tube in identifiedTubes)
                {
                    if (testOrderStates.TryGetValue(tube.SampleId!, out var request))
                    {
                        // there is a request for this tube
                        tube.RequestCreatedDate = request.RequestUid.RequestCreatedDate;
                        if (request.TestOrderStates.Count > 0)
                        {
                            // there are test orders in the request
                            tube.UpdateTubeCompletionStatus(request.TestOrderStates);
                        }
                    }
                    else
                    {
                        // TODO: handle QC-related internal errors
                        tube.InternalError = SampleTubeError.MissingTestOrders;
                    }
                    var requestId = await GetRequestIdForSampleTube(tube);
                    if (requestId.HasValue)
                    {
                        requestsToNotify.Add(requestId.Value);
                    }
                }
            }

            return;
        }

        // update sample tubes from a partial message
        foreach (var sample in newStorage.Samples)
        {
            var sampleTube = sample.ToSampleTube();

            if (sample.SampleQcStatus == EnumSampleQcTubeStatus.SampleQcTubeStatusNotNeeded)
            {
                sampleTube.InternalError = sample.SampleType == EnumSampleType.Sample
                    ? SampleTubeError.MethodsNotActive
                    : SampleTubeError.QcNotNeeded;
            }

            sampleTube.InstrumentNumber = instrumentNumber;
            SampleTube? previousTube = null;

            if (sample.ChangeType == EnumChangeType.AddedObject)
            {
                sampleTube.Status = string.IsNullOrEmpty(sampleTube.SampleId) ? SampleTubeStatus.NotIdentified : SampleTubeStatus.InProgress;
                // The mapper is used here to update the properties of an existing sampleTube instance, 
                // because in cases of manual input, the tube is already exist with a TubeId and other properties. 
                sampleStorage.AddOrUpdateTube(
                               sampleTube,
                               oldTube =>
                               {
                                   sample.UpdateSampleTube(oldTube);
                                   if (sampleTube.InternalError != SampleTubeError.None)
                                   {
                                       oldTube.InternalError = sampleTube.InternalError;
                                   }
                                   return oldTube;
                               });
            }
            else if (sample.ChangeType == EnumChangeType.UpdatedObject)
            {
                sampleStorage.AddOrUpdateTube(
                    sampleTube,
                    oldTube =>
                    {
                        previousTube = new SampleTube
                        {
                            Area = oldTube.Area,
                            TrayId = oldTube.TrayId
                        };
                        sample.UpdateSampleTube(oldTube);
                        if (sampleTube.InternalError != SampleTubeError.None)
                        {
                            oldTube.InternalError = sampleTube.InternalError;
                        }
                        return oldTube;
                    });
            }
            else if (sample.ChangeType == EnumChangeType.DeletedObject)
            {
                var unloadedTube = GetUnloadedTube(sampleTube);
                if (unloadedTube != null)
                {
                    unloadedTubes.Add(new UnloadedTube(unloadedTube.TubeId!, unloadedTube.SampleId!, unloadedTube.Error != SampleTubeError.None));
                }

                sampleStorage.RemoveTube(sampleTube);
            }
            else
            {
                throw new FluentValidation.ValidationException("Invalid change type for a sample tube.");
            }

            var requestId = await GetRequestIdForSampleTube(sampleTube);
            if (requestId.HasValue)
            {
                requestsToNotify.Add(requestId.Value);
            }

            UpdateDrawerModificationFlags(sample, previousTube);
        }
    }

    private SampleTube? GetUnloadedTube(SampleTube sampleTube)
    {
        if (string.IsNullOrEmpty(sampleTube.TubeId))
        {
            return null;
        }

        return sampleStorage.TubesAsQueryable(sampleTube.InstrumentNumber!)
            .FirstOrDefault(x => x.TubeId == sampleTube.TubeId &&
                            x.Area == sampleTube.Area &&
                            x.TubePosition == sampleTube.TubePosition);
    }

    private async Task<string> GetSampleIdSuffix(string? tubeBarcode, string instrumentNumber)
    {
        var tubeKey = await sampleQcIdProvider.GetSampleQcTubeKey(tubeBarcode, instrumentNumber);
        return tubeKey.IdSuffix;
    }

    private async Task<int?> GetRequestIdForSampleTube(SampleTube sampleTube)
    {
        var requestIdResponse = await sender.Send(new GetRequestIdQuery(sampleTube.SampleId!, sampleTube.RequestCreatedDate));
        return requestIdResponse.RequestExists ? requestIdResponse.RequestId : null;
    }

    private void UpdateDrawerModificationFlags(Tetris.Protocol.Pharos.Storage.SampleTube modifiedTube, SampleTube? previousTube)
    {
        // Current tube position
        var currentArea = modifiedTube.Position.Area.ToSampleArea();
        if (IsSampleDrawerArea(currentArea))
        {
            _modifiedSampleTrayIdList.Add(modifiedTube.Position.TrayBarcode);
            _leftSampleDrawerChanged |= currentArea == SampleArea.SampleDrawer1;
            _rightSampleDrawerChanged |= currentArea == SampleArea.SampleDrawer2;
        }

        // Tube's previous position when moved between trays or drawers
        if (previousTube != null)
        {
            var previousArea = previousTube.Area;
            if (IsSampleDrawerArea(previousArea))
            {
                if (!string.IsNullOrEmpty(previousTube.TrayId) && previousTube.TrayId != modifiedTube.Position.TrayBarcode)
                {
                    _modifiedSampleTrayIdList.Add(previousTube.TrayId);
                }

                if (previousArea != currentArea)
                {
                    _leftSampleDrawerChanged |= previousArea == SampleArea.SampleDrawer1;
                    _rightSampleDrawerChanged |= previousArea == SampleArea.SampleDrawer2;
                }
            }
        }
    }

    private async Task HandleTaskCards(string instrumentNumber, CancellationToken cancellationToken)
    {
        await HandleSampleTubeErrorTaskCard(instrumentNumber, cancellationToken);
        await HandleTaskCardForSampleTubeStatus(instrumentNumber, SampleTubeStatus.PartlyCompleted, TaskCardType.SamplePartlyCompleted, cancellationToken);
        await HandleTaskCardForUnloadRequested(instrumentNumber, cancellationToken);
    }

    private async Task HandleSampleTubeErrorTaskCard(string instrumentNumber, CancellationToken cancellationToken)
    {
        var errorCount = sampleStorage.GetTrays(instrumentNumber).Count(x => x.TubeErrorCount > 0);

        if (errorCount > 0)
        {
            await sender.Send(new TaskCardCommand(TaskCardType.SampleTubeError, TaskCardState.Warning, instrumentNumber), cancellationToken);
        }
        else
        {
            await sender.Send(new RemoveTaskCardCommand(TaskCardType.SampleTubeError, instrumentNumber), cancellationToken);
        }
    }

    private async Task HandleTaskCardForSampleTubeStatus(string instrumentNumber, SampleTubeStatus status, TaskCardType taskCardType, CancellationToken cancellationToken)
    {
        var hasSampleTubeWithStatus = sampleStorage.TubesAsQueryable(instrumentNumber)
                                                   .Any(x => IsSampleDrawerArea(x.Area)
                                                           && x.Status == status
                                                           && x.Error == SampleTubeError.None);

        var taskCardExists = await sender.Send(new DoesTaskCardExistQuery(taskCardType, instrumentNumber), cancellationToken);
        if (hasSampleTubeWithStatus)
        {
            if (!taskCardExists)
            {
                await sender.Send(new TaskCardCommand(taskCardType, TaskCardState.Information, instrumentNumber), cancellationToken);
            }
        }
        else
        {
            await sender.Send(new RemoveTaskCardCommand(taskCardType, instrumentNumber), cancellationToken);
        }
    }

    private async Task HandleTaskCardForUnloadRequested(string instrumentNumber, CancellationToken cancellationToken)
    {
        var hasUnloadRequestedTube = sampleStorage.TubesAsQueryable(instrumentNumber).Any(x => IsSampleDrawerArea(x.Area) && x.UnloadRequested);

        var taskCardExists = await sender.Send(new DoesTaskCardExistQuery(TaskCardType.RemoveSamplesFromDrawer, instrumentNumber), cancellationToken);
        if (hasUnloadRequestedTube)
        {
            if (!taskCardExists)
            {
                await sender.Send(new TaskCardCommand(TaskCardType.RemoveSamplesFromDrawer, TaskCardState.Information, instrumentNumber), cancellationToken);
            }
        }
        else
        {
            await sender.Send(new RemoveTaskCardCommand(TaskCardType.RemoveSamplesFromDrawer, instrumentNumber), cancellationToken);
        }
    }

    private static bool IsSampleDrawerArea(SampleArea area)
    {
        return area is SampleArea.SampleDrawer1 or SampleArea.SampleDrawer2;
    }
}