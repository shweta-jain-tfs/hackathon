using Microsoft.Extensions.Options;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Sample;
using Pharos.Server.Application.Settings.Sample;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Sample.Application.Interfaces;
using Pharos.Server.Sample.Domain;
using Tetris.Protocol.Common;
using Tetris.Protocol.Pharos;
using InstrumentState = Pharos.Server.Domain.Enums.InstrumentState;

namespace Pharos.Server.Sample.Application.Queries.GetSampleQcDestination;

internal class GetSampleQcDestinationQueryHandler(
    IInstrumentSampleStorage sampleStorage,
    IOptionsSnapshot<SamplesSettings> samplesSettings,
    IInstrumentManager instrumentManager,
    IMediator mediator) : IRequestHandler<GetSampleQcDestinationQuery, SampleQcDestinationResponse>
{
    public async Task<SampleQcDestinationResponse> Handle(GetSampleQcDestinationQuery request, CancellationToken cancellationToken)
    {
        var instrumentNumber = request.InstrumentNumber;
        var message = request.Message;

        SampleTubeDestination? wantedDestination = null;

        if (IsSampleInInternalStorage(message.CurrentPosition))
        {
            wantedDestination = await HandleSampleInInternalStorage(instrumentNumber, message, cancellationToken);
        }
        else if (IsSampleInDrawer(message.CurrentPosition))
        {
            wantedDestination = await HandleSampleInDrawer(instrumentNumber, message, cancellationToken);
        }

        return CreateResponse(wantedDestination);
    }

    private static bool IsSampleInInternalStorage(SampleTubePosition sampleTubePosition)
    {
        return sampleTubePosition.Area is EnumSampleArea.InternalStorage or EnumSampleArea.Shuttle1 or EnumSampleArea.Shuttle2;
    }

    private static bool IsSampleInDrawer(SampleTubePosition sampleTubePosition)
    {
        return sampleTubePosition.Area is EnumSampleArea.SampleDrawer1 or EnumSampleArea.SampleDrawer2;
    }

    private async Task<SampleTubeDestination?> HandleSampleInInternalStorage(string instrumentNumber, SampleQcDestinationRequest request, CancellationToken cancellationToken)
    {
        SampleTubeDestination? wantedDestination = null;

        var sampleTube = sampleStorage.GetTube(request.TubeId);
        if (sampleTube == null)
        {
            return wantedDestination;
        }

        if (IsInProgressTube(sampleTube) && CanMoveSampleTubeToInternalStorage(instrumentNumber) && IsInternalStorageAvailable(instrumentNumber, sampleTube))
        {
            return new SampleTubeDestination
            {
                Area = EnumSampleMoveArea.MoveInternalStorage
            };
        }
        else
        {
            return await GetWantedDrawerDestination(instrumentNumber, sampleTube, cancellationToken);
        }
    }

    private async Task<SampleTubeDestination?> HandleSampleInDrawer(string instrumentNumber, SampleQcDestinationRequest request, CancellationToken cancellationToken)
    {
        var sampleTube = sampleStorage.GetTube(request.TubeId);
        if (sampleTube == null)
        {
            return null;
        }

        if (CanMoveSampleTubeToInternalStorage(instrumentNumber))
        {
            if ( IsInProgressTube(sampleTube) && IsInternalStorageAvailable(instrumentNumber, sampleTube))
            {
                return new SampleTubeDestination
                {
                    Area = EnumSampleMoveArea.MoveInternalStorage
                };
            }
        }

        return await GetWantedDrawerDestination(instrumentNumber, sampleTube, cancellationToken);
    }

    private bool CanMoveSampleTubeToInternalStorage(string instrumentNumber)
    {
        return instrumentManager.GetInstrument(instrumentNumber).State is InstrumentState.Priming
            or InstrumentState.BlankMeasurement
            or InstrumentState.Assay
            or InstrumentState.AssayFinishOrdered;
    }

    private bool IsInternalStorageAvailable(string instrumentNumber, SampleTube sampleTube)
    {
        var totalTubesInInternalStorage = sampleStorage.TubesAsQueryable(instrumentNumber)
            .Count(x => x.Area == SampleArea.InternalStorage);

        if (sampleTube.SampleType == SampleType.QualityControl)
        {
            return totalTubesInInternalStorage < Constants.SamplesInternalStorageMaxCap;
        }
        else if (sampleTube.SampleType == SampleType.Sample)
        {
            var sampleTubesInInternalStorage = sampleStorage.TubesAsQueryable(instrumentNumber)
                .Count(x => x.Area == SampleArea.InternalStorage && x.SampleType == SampleType.Sample);

            return sampleTubesInInternalStorage < (Constants.SamplesInternalStorageMaxCap - Constants.InternalStoragePositionsReservedForQc);
        }

        return false;
    }

    private async Task<SampleTubeDestination?> GetWantedDrawerDestination(string instrumentNumber, SampleTube tube, CancellationToken cancellationToken)
    {
        SampleTubeDestination? wantedDestination;

        if (tube.IdentitySource == SampleTubeIdentitySource.External)
        {
            wantedDestination = GetReservedPosition(instrumentNumber, tube);
            if (wantedDestination == null)
            {
                //TODO: if the original position is occupied by other tube, raise a task card
            }
        }
        else
        {
            var drawerConfiguration = samplesSettings.Value.Drawer;

            var isInProgressTube = IsInProgressTube(tube);
            var isErrorTube = IsErrorTube(tube);

            var occupiedPositions = GetOccupiedDrawerPositions(instrumentNumber, tube);

            var reservedPositions = sampleStorage.TubesAsQueryable(instrumentNumber)
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

            if (drawerConfiguration.DrawerMode == DrawerMode.BothDrawersAsInputAndOutput)
            {
                if (isInProgressTube || isErrorTube)
                {
                    wantedDestination = FindFirstAvailablePosition(instrumentNumber, occupiedPositions, reservedPositions);
                }
                else
                {
                    wantedDestination = FindLastAvailablePosition(instrumentNumber, occupiedPositions, reservedPositions);
                }
                if (wantedDestination == null)
                {
                    await CreateLoadEmptyTrayTaskCard(TaskCardType.LoadEmptySampleTray, instrumentNumber, cancellationToken);
                }
            }
            else
            {
                if (isInProgressTube)
                {
                    wantedDestination = FindFirstAvailablePosition(instrumentNumber, occupiedPositions, reservedPositions, DrawerType.Sample1);
                    if (wantedDestination == null)
                    {
                        await CreateLoadEmptyTrayTaskCard(TaskCardType.LoadEmptyLeftSampleTray, instrumentNumber, cancellationToken);
                    }
                }
                else if (isErrorTube)
                {
                    wantedDestination = FindFirstAvailablePosition(instrumentNumber, occupiedPositions, reservedPositions, DrawerType.Sample2);
                    if (wantedDestination == null)
                    {
                        await CreateLoadEmptyTrayTaskCard(TaskCardType.LoadEmptyRightSampleTray, instrumentNumber, cancellationToken);
                    }
                }
                else
                {
                    wantedDestination = FindLastAvailablePosition(instrumentNumber, occupiedPositions, reservedPositions, DrawerType.Sample2);
                    if (wantedDestination == null)
                    {
                        await CreateLoadEmptyTrayTaskCard(TaskCardType.LoadEmptyRightSampleTray, instrumentNumber, cancellationToken);
                    }
                }
            }
        }

        return wantedDestination;
    }

    private List<SampleTube> GetOccupiedDrawerPositions(string instrumentNumber, SampleTube tube)
    {
        var query = sampleStorage.TubesAsQueryable(instrumentNumber)
            .Where(x => x.Area == SampleArea.SampleDrawer1 || x.Area == SampleArea.SampleDrawer2);

        //if the tube is in drawer (manual entry), we should exclude the current tube from occupied positions
        if (tube.Area is SampleArea.SampleDrawer1 or SampleArea.SampleDrawer2)
        {
            query = query.Where(x => x.TubeId != tube.TubeId);
        }

        return query.ToList();
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

    private SampleTubeDestination? FindLastAvailablePosition(string instrumentNumber, List<SampleTube> occupiedPositions, List<SampleTube> reservedPositions, DrawerType? specificDrawer = null)
    {
        var traysInDrawer = specificDrawer == null
                           ? sampleStorage.GetTrays(instrumentNumber).OrderByDescending(t => t.Drawer).ThenByDescending(t => t.TrayPosition).ToList()
                           : sampleStorage.GetTrays(instrumentNumber, specificDrawer.Value).OrderByDescending(t => t.TrayPosition).ToList();

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

            var orderedPositions = Enumerable.Range(1, Constants.MaxSampleTubesInTray).Reverse();

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

    private async Task CreateLoadEmptyTrayTaskCard(TaskCardType cardType, string instrumentNumber, CancellationToken cancellationToken)
    {
        await mediator.Send(new TaskCardCommand(cardType, TaskCardState.Warning, instrumentNumber), cancellationToken);
    }

    private static bool IsInProgressTube(SampleTube tube)
    {
        return tube.Status == SampleTubeStatus.InProgress && tube.Error == SampleTubeError.None && !tube.UnloadRequested;
    }

    private static bool IsErrorTube(SampleTube tube)
    {
        return tube.Error != SampleTubeError.None && !tube.UnloadRequested;
    }

    private SampleTubeDestination? GetReservedPosition(string instrumentNumber, SampleTube tube)
    {
        if (tube.OriginalPosition == null)
        {
            return null;
        }

        var originalPosition = tube.OriginalPosition;

        var isPositionOccupied = sampleStorage.TubesAsQueryable(instrumentNumber)
                                        .Any(x => x.Area == originalPosition.Area
                                                && x.TrayPosition == originalPosition.TrayPosition
                                                && x.TubePosition == originalPosition.TubePosition);
        if (isPositionOccupied)
        {
            return null;
        }

        return new SampleTubeDestination
        {
            Area = originalPosition.Area == SampleArea.SampleDrawer1 ? EnumSampleMoveArea.MoveDrawer1 : EnumSampleMoveArea.MoveDrawer2,
            TrayPosition = originalPosition.TrayPosition!.Value,
            TubePosition = originalPosition.TubePosition!.Value
        };
    }

    private static SampleQcDestinationResponse CreateResponse(SampleTubeDestination? wantedDestination = null)
    {
        var sampleQcDestinationResponse = new SampleQcDestinationResponse();

        wantedDestination ??= new SampleTubeDestination { Area = EnumSampleMoveArea.NoMove };
        sampleQcDestinationResponse.WantedDestination = wantedDestination;

        return sampleQcDestinationResponse;
    }
}
