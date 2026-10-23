using Microsoft.Extensions.Options;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Request;
using Pharos.Server.Application.Sample;
using Pharos.Server.Application.Settings.Sample;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Sample.Application.Commands.RemoveTubeFromUnloadedTray;
using Pharos.Server.Sample.Application.Commands.UpdateSampleTrayPosition;
using Pharos.Server.Sample.Application.Helpers;
using Pharos.Server.Sample.Application.Interfaces;
using Pharos.Server.Sample.Domain;
using Tetris.Protocol.Common;
using Tetris.Protocol.Pharos;

namespace Pharos.Server.Sample.Application.Commands.ProcessScannedTube;

internal sealed class ProcessScannedTubeCommandHandler(
    IInstrumentSampleStorage sampleStorage,
    ISampleQcIdProvider sampleQcIdProvider,
    IPublisher publisher,
    IInstrumentManager instrumentManager,
    IOptionsSnapshot<SamplesSettings> samplesSettings,
    IMediator mediator)
    : IRequestHandler<ProcessScannedTubeCommand, ScannedSampleQcResponse>
{
    public async Task<ScannedSampleQcResponse> Handle(ProcessScannedTubeCommand request, CancellationToken cancellationToken)
    {
        var instrumentNumber = request.InstrumentNumber;
        var message = request.Message;
        var position = message.CurrentPosition;

        if (message.CannotReadBarcode)
        {
            return await HandleUnreadableTubeBarcode(position, instrumentNumber);
        }

        var tubeKey = await sampleQcIdProvider.GetSampleQcTubeKey(message.TubeBarcode, instrumentNumber);

        if (tubeKey.UnreadableBarcode)
        {
            return HandleInvalidTubeBarcode(position, instrumentNumber, tubeKey);
        }

        var duplicateTubeBarcodeResponse = await HandleDuplicateTubeBarcode(position, instrumentNumber, tubeKey, cancellationToken);
        if (duplicateTubeBarcodeResponse != null)
        {
            return duplicateTubeBarcodeResponse;
        }

        // Handle valid tube
        var scannedTube = CreateSampleTube(position, instrumentNumber, tubeKey);
        await UpdateTubeStatus(scannedTube, cancellationToken);

        sampleStorage.AddOrUpdateTube(scannedTube, oldTube =>
        {
            oldTube.SampleId = scannedTube.SampleId;
            oldTube.SampleIdSuffix = scannedTube.SampleIdSuffix;
            oldTube.TubeBarcode = scannedTube.TubeBarcode;
            oldTube.SampleType = scannedTube.SampleType;
            oldTube.Status = scannedTube.Status;
            oldTube.RequestCreatedDate = scannedTube.RequestCreatedDate;
            return oldTube;
        });

        if (tubeKey.SampleType == SampleType.Sample)
        {
            await publisher.Publish(new TubesIdentifiedEvent(instrumentNumber, new[] { new SampleTubeInformation(new SampleTubeId(tubeKey.TubeId, tubeKey.Id), scannedTube.IdentitySource) }), cancellationToken);
        }

        return new ScannedSampleQcResponse
        {
            SampleType = scannedTube.SampleType == SampleType.Sample ? EnumSampleType.Sample : EnumSampleType.QualityControl,
            SampleId = scannedTube.SampleId,
            TubeId = scannedTube.TubeId,
            LotNumber = tubeKey.QcLotNumber, //will be string.Empty for non QC samples
            TubeTypeIndex = tubeKey.TubeTypeId, //will be -1 for non QC samples
            WantedDestination = GetDestinationForValidTube(instrumentNumber, tubeKey.TubeId, tubeKey.SampleType, scannedTube)
        };
    }

    private async Task<ScannedSampleQcResponse> HandleUnreadableTubeBarcode(SampleTubePosition position, string instrumentNumber)
    {
        // create the tube with empty tube ID in the storage, don't update if it exists
        var tubeKey = await sampleQcIdProvider.GetSampleQcTubeKey(string.Empty);
        var unreadableTube = CreateSampleTube(position, instrumentNumber, tubeKey);
        sampleStorage.AddOrUpdateTube(unreadableTube, oldTube => oldTube);

        return new ScannedSampleQcResponse
        {
            TubeId = unreadableTube.TubeId,
            SampleId = string.Empty,
            SampleType = EnumSampleType.UnspecifiedSampleType,
            Error = EnumBarcodeReadingError.UnreadableTubeBarcode,
            WantedDestination = GetDestinationForTubeWithError(instrumentNumber, unreadableTube.TubeId!)
        };
    }

    private ScannedSampleQcResponse HandleInvalidTubeBarcode(SampleTubePosition position, string instrumentNumber, SampleQcTubeKey tubeKey)
    {
        // create the tube in the storage, don't update if it exists
        var tubeWithInvalidBarcode = CreateSampleTube(position, instrumentNumber, tubeKey);
        sampleStorage.AddOrUpdateTube(tubeWithInvalidBarcode, oldTube => oldTube);

        return new ScannedSampleQcResponse
        {
            TubeId = tubeWithInvalidBarcode.TubeId,
            SampleId = string.Empty,
            SampleType = EnumSampleType.UnspecifiedSampleType,
            Error = EnumBarcodeReadingError.InvalidTubeBarcode,
            WantedDestination = GetDestinationForTubeWithError(instrumentNumber, tubeWithInvalidBarcode.TubeId!)
        };
    }

    private async Task<ScannedSampleQcResponse?> HandleDuplicateTubeBarcode(SampleTubePosition position, string instrumentNumber, SampleQcTubeKey tubeKey, CancellationToken cancellationToken)
    {
        // Check if the tube already exists in storage
        var tube = sampleStorage.GetTube(tubeKey.TubeId);

        // Remove scanned tube from unloaded tray if present
        await mediator.Send(new RemoveTubeFromUnloadedTrayCommand(tubeKey.TubeId), cancellationToken);

        if (tube == null)
        {
            return null;
        }

        var drawer = tube.Area == SampleArea.SampleDrawer1 ? DrawerType.Sample1 : DrawerType.Sample2;
        var drawerLockState = instrumentManager.GetInstrument(tube.InstrumentNumber!).DrawersLockState[drawer].LockState;

        // Duplicate tube in closed drawer
        if (drawerLockState != LockState.Open)
        {
            var tubeWithDuplicatedBarcode = CreateSampleTube(position, instrumentNumber, tubeKey);

            // Generate new tube ID for the tube with duplicated barcode
            var newTubeKey = await sampleQcIdProvider.GetSampleQcTubeKey(string.Empty);
            tubeWithDuplicatedBarcode.TubeId = newTubeKey.TubeId;

            sampleStorage.AddOrUpdateTube(tubeWithDuplicatedBarcode, oldTube =>
            {
                oldTube.SampleId = tubeWithDuplicatedBarcode.SampleId;
                oldTube.SampleIdSuffix = tubeWithDuplicatedBarcode.SampleIdSuffix;
                oldTube.TubeBarcode = tubeWithDuplicatedBarcode.TubeBarcode;
                oldTube.SampleType = tubeWithDuplicatedBarcode.SampleType;
                return oldTube;
            });

            return new ScannedSampleQcResponse
            {
                TubeId = tubeWithDuplicatedBarcode.TubeId,
                SampleId = tubeWithDuplicatedBarcode.SampleId,
                SampleType = tubeWithDuplicatedBarcode.SampleType == SampleType.Sample ? EnumSampleType.Sample : EnumSampleType.QualityControl,
                Error = EnumBarcodeReadingError.DuplicateTubeBarcode,
                LotNumber = tubeKey.QcLotNumber, //will be string.Empty for non QC samples
                TubeTypeIndex = tubeKey.TubeTypeId, //will be -1 for non QC samples
                WantedDestination = GetDestinationForTubeWithError(instrumentNumber, tubeWithDuplicatedBarcode.TubeId)
            };
        }

        // Tube is scanned in the current position, but it was already in another position, so tube should be
        // dynamically moved to the current position by clearing the old position.
        sampleStorage.RemoveTube(tube);
        await mediator.Send(new UpdateSampleTrayPositionCommand
        {
            TrayId = tube.TrayId!,
            TubePosition = tube.TubePosition!.Value,
            TubeBarcode = string.Empty,
            HighPriority = false
        }, cancellationToken);

        return null;
    }

    private async Task UpdateTubeStatus(SampleTube scannedTube, CancellationToken cancellationToken)
    {
        scannedTube.Status = SampleTubeStatus.InProgress;
        //Determine the status of the tube based on the Request for the sample. This is needed if the unloaded tube was loaded back to the instrument
        var requestTestOrderStates = await mediator.Send(new GetTestOrderStatesQuery(new HashSet<string> { scannedTube.SampleId! }), cancellationToken);
        if (!requestTestOrderStates.TryGetValue(scannedTube.SampleId!, out var request))
        {
            return;
        }

        scannedTube.RequestCreatedDate = request.RequestUid.RequestCreatedDate;
        if (request.TestOrderStates.Count > 0)
        {
            scannedTube.UpdateTubeCompletionStatus(request.TestOrderStates);
        }
    }

    private SampleTubeDestination GetDestinationForTubeWithError(string instrumentNumber, string tubeId)
    {
        var destination = GetDrawerDestination(instrumentNumber, tubeId, isErrorTube: true);

        return destination ?? new SampleTubeDestination
        {
            Area = EnumSampleMoveArea.NoMove
        };
    }

    private SampleTubeDestination GetDestinationForValidTube(string instrumentNumber, string tubeId, SampleType sampleType, SampleTube scannedTube)
    {
        if (IsInProgressTube(scannedTube) && instrumentManager.GetInstrument(instrumentNumber).IsInAssayMode)
        {
            var totalTubesInInternalStorage = sampleStorage.TubesAsQueryable(instrumentNumber)
                .Count(x => x.Area == SampleArea.InternalStorage);

            if (sampleType == SampleType.QualityControl)
            {
                var isInternalStorageSpaceAvailable = totalTubesInInternalStorage < Constants.SamplesInternalStorageMaxCap;

                if (isInternalStorageSpaceAvailable)
                {
                    return new SampleTubeDestination
                    {
                        Area = EnumSampleMoveArea.MoveInternalStorage
                    };
                }
            }
            else if (sampleType == SampleType.Sample)
            {
                var sampleTubesInInternalStorage = sampleStorage.TubesAsQueryable(instrumentNumber)
                    .Count(x => x.Area == SampleArea.InternalStorage && x.SampleType == SampleType.Sample);

                var isInternalStorageSpaceAvailable = sampleTubesInInternalStorage < (Constants.SamplesInternalStorageMaxCap - Constants.InternalStoragePositionsReservedForQc);

                if (isInternalStorageSpaceAvailable)
                {
                    return new SampleTubeDestination
                    {
                        Area = EnumSampleMoveArea.MoveInternalStorage
                    };
                }
            }
        }

        var destination = GetDrawerDestination(instrumentNumber, tubeId, isErrorTube: false);

        return destination ?? new SampleTubeDestination
        {
            Area = EnumSampleMoveArea.NoMove
        };
    }

    private static bool IsInProgressTube(SampleTube tube)
    {
        return tube.Status == SampleTubeStatus.InProgress && tube.Error == SampleTubeError.None;
    }

    private SampleTubeDestination? GetDrawerDestination(string instrumentNumber, string tubeId, bool isErrorTube)
    {
        var drawerConfiguration = samplesSettings.Value.Drawer;

        var occupiedPositions = GetOccupiedDrawerPositions(instrumentNumber, tubeId);
        var reservedPositions = GetReservedDrawerPositions(instrumentNumber);

        //TODO: Need to handle the case for Partly Completed and Completed tubes when left Drawer as input and right drawer as output is configured.
        return drawerConfiguration.DrawerMode == DrawerMode.BothDrawersAsInputAndOutput
            ? FindFirstAvailablePosition(instrumentNumber, occupiedPositions, reservedPositions)
            : FindFirstAvailablePosition(instrumentNumber, occupiedPositions, reservedPositions, isErrorTube ? DrawerType.Sample2 : DrawerType.Sample1);
    }

    private SampleTubeDestination? FindFirstAvailablePosition(string instrumentNumber, List<SampleTube> occupiedPositions, List<SampleTube> reservedPositions, DrawerType? specificDrawer = null)
    {
        var traysInDrawer = specificDrawer == null
                            ? sampleStorage.GetTrays(instrumentNumber).OrderBy(t => t.Drawer).ThenBy(t => t.TrayPosition).ToList()
                            : sampleStorage.GetTrays(instrumentNumber, specificDrawer.Value).OrderBy(t => t.TrayPosition).ToList();

        foreach (var tray in traysInDrawer)
        {
            if (tray.Error != SampleTrayError.None)
            {
                continue;
            }

            var trayPosition = tray.TrayPosition!.Value;
            var area = tray.Drawer == DrawerType.Sample1 ? SampleArea.SampleDrawer1 : SampleArea.SampleDrawer2;

            var occupiedPositionInTrays = occupiedPositions.Where(x => x.Area == area && x.TrayPosition == trayPosition).ToList();
            var reservedPositionsInTrays = reservedPositions.Where(x => x.Area == area && x.TrayPosition == trayPosition).ToList();

            var orderedPositions = Enumerable.Range(1, Constants.MaxSampleTubesInTray);

            foreach (var tubePosition in orderedPositions)
            {
                if (occupiedPositionInTrays.Any(x => x.TubePosition == tubePosition) || reservedPositionsInTrays.Any(x => x.TubePosition == tubePosition))
                {
                    continue;
                }

                return new SampleTubeDestination
                {
                    Area = tray.Drawer == DrawerType.Sample1 ? EnumSampleMoveArea.MoveDrawer1 : EnumSampleMoveArea.MoveDrawer2,
                    TrayPosition = trayPosition,
                    TubePosition = tubePosition
                };
            }
        }

        return null;
    }

    private List<SampleTube> GetOccupiedDrawerPositions(string instrumentNumber, string tubeId)
    {
        return sampleStorage.TubesAsQueryable(instrumentNumber)
            .Where(x => (x.Area == SampleArea.SampleDrawer1 || x.Area == SampleArea.SampleDrawer2)
                     && x.TubeId != tubeId)
            .ToList();
    }

    private List<SampleTube> GetReservedDrawerPositions(string instrumentNumber)
    {
        return sampleStorage.TubesAsQueryable(instrumentNumber)
            .Where(x => x.IdentitySource == SampleTubeIdentitySource.External
                     && x.OriginalPosition != null
                     && x.Area != SampleArea.SampleDrawer1
                     && x.Area != SampleArea.SampleDrawer2)
            .Select(x => new SampleTube
            {
                Area = x.OriginalPosition!.Area,
                TrayPosition = x.OriginalPosition.TrayPosition,
                TubePosition = x.OriginalPosition.TubePosition
            })
            .ToList();
    }

    private static SampleTube CreateSampleTube(SampleTubePosition position, string instrumentNumber, SampleQcTubeKey tubeKey)
    {
        var area = position.Area == EnumSampleArea.SampleDrawer1 ? SampleArea.SampleDrawer1 : SampleArea.SampleDrawer2;
        return new SampleTube
        {
            Area = area,
            InstrumentNumber = instrumentNumber,
            TrayId = position.TrayBarcode,
            TrayPosition = position.TrayPosition,
            TubePosition = position.TubePosition,
            TubeId = tubeKey.TubeId,
            SampleId = tubeKey.Id,
            SampleIdSuffix = tubeKey.IdSuffix,
            SampleType = tubeKey.SampleType,
            TubeBarcode = tubeKey.Barcode ?? ""
        };
    }
}