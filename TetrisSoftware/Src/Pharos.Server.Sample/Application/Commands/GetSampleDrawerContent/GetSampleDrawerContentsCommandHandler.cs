using Google.Protobuf.Collections;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Request;
using Pharos.Server.Application.Sample;
using Pharos.Server.Application.Settings.Sample;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Infrastructure;
using Pharos.Server.Sample.Application.Events;
using Pharos.Server.Sample.Application.Interfaces;
using Pharos.Server.Sample.Application.Mapping;
using Pharos.Server.Sample.Domain;
using Tetris.Protocol.Common;
using Tetris.Protocol.Pharos.Tray;

namespace Pharos.Server.Sample.Application.Commands.GetSampleDrawerContent;

internal sealed class GetSampleDrawerContentsCommandHandler(
    IInstrumentSampleStorage sampleStorage,
    ISampleDbContext dbContext,
    IDateTime dateTime,
    IInstrumentManager instrumentManager,
    ICurrentInstrumentServerHub currentInstrumentHub,
    IOptionsSnapshot<SamplesSettings> samplesSettings,
    ISender sender,
    IPublisher publisher)
    : IRequestHandler<GetSampleDrawerContentsCommand, SampleDrawerContentsResponse>
{
    public async Task<SampleDrawerContentsResponse> Handle(GetSampleDrawerContentsCommand request, CancellationToken cancellationToken)
    {
        var instrumentNumber = request.InstrumentNumber;
        var instrumentDrawer = request.Message.Drawer.ToDrawerType();
        var instrumentTrays = request.Message.SampleTrays;

        var errorInstrumentTrays = await ValidateInstrumentTrays(instrumentNumber, instrumentDrawer, instrumentTrays, cancellationToken);

        var validInstrumentTrays = instrumentTrays.Where(tray => !errorInstrumentTrays.Contains(tray)).ToList();

        await HandleUnConfirmedManualInputTubes(validInstrumentTrays, cancellationToken);

        var tubesPlacedInWrongDrawerTrays = GetSampleTubesPlacedInWrongDrawerTrays(validInstrumentTrays, instrumentNumber, instrumentDrawer);

        var traysWithPredefinedPositions = await GetPredefinedPositions(instrumentNumber, instrumentDrawer, validInstrumentTrays, tubesPlacedInWrongDrawerTrays, cancellationToken);

        var traysWithRescanTubes = await GetRescanTubes(validInstrumentTrays, cancellationToken);

        await HandleTraysAddedToInstrument(instrumentNumber, instrumentDrawer, validInstrumentTrays, cancellationToken);

        await HandleTraysRemovedFromInstrument(instrumentNumber, instrumentDrawer, instrumentTrays, cancellationToken);

        await HandleLoadEmptyTrayTaskCard(instrumentNumber, instrumentDrawer, validInstrumentTrays, cancellationToken);

        if (dbContext.HasChanges())
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await NotifyPredefinedTubesIdentified(traysWithPredefinedPositions, instrumentNumber, cancellationToken);

        await NotifySampleDrawerContentsReceived(instrumentNumber, instrumentTrays);

        return await CreateSampleDrawerContentsResponseAsync(traysWithPredefinedPositions, errorInstrumentTrays, tubesPlacedInWrongDrawerTrays, traysWithRescanTubes, cancellationToken);
    }

    private async Task HandleLoadEmptyTrayTaskCard(string instrumentNumber, DrawerType instrumentDrawer, List<DetectedSampleTray> validTrays, CancellationToken cancellationToken)
    {
        var drawerConfiguration = samplesSettings.Value.Drawer;
        var externalReservedPositions = sampleStorage.TubesAsQueryable(instrumentNumber)
                                            .Where(x => x.IdentitySource == SampleTubeIdentitySource.External
                                                        && x.OriginalPosition != null
                                                        && x.Area != SampleArea.SampleDrawer1
                                                        && x.Area != SampleArea.SampleDrawer2)
                                            .ToList();

        // Check the current drawer for empty positions
        var hasFreePositions = validTrays.Any(tray => HasFreeSamplePositions(tray.TrayBarcode, tray.DetectedTubes.Count, externalReservedPositions));

        if (drawerConfiguration.DrawerMode == DrawerMode.LeftDrawerAsInputAndRightDrawerAsOutput)
        {
            if (hasFreePositions)
            {
                var taskCardTypeToRemove = instrumentDrawer == DrawerType.Sample1
                    ? TaskCardType.LoadEmptyLeftSampleTray
                    : TaskCardType.LoadEmptyRightSampleTray;

                await sender.Send(new RemoveTaskCardCommand(taskCardTypeToRemove, instrumentNumber), cancellationToken);
                await sender.Send(new RemoveTaskCardCommand(TaskCardType.LoadEmptySampleTray, instrumentNumber), cancellationToken);
            }
        }
        else
        {
            // Check the other drawer for empty positions
            if (!hasFreePositions)
            {
                var trays = sampleStorage.GetTrays(instrumentNumber).Where(x => x.Error == SampleTrayError.None && x.Drawer != instrumentDrawer).ToList();
                hasFreePositions = trays.Any(tray => HasFreeSamplePositions(tray.Id, tray.SampleTubes.Count, externalReservedPositions));
            }

            if (hasFreePositions)
            {
                await sender.Send(new RemoveTaskCardCommand(TaskCardType.LoadEmptySampleTray, instrumentNumber), cancellationToken);
                await sender.Send(new RemoveTaskCardCommand(TaskCardType.LoadEmptyLeftSampleTray, instrumentNumber), cancellationToken);
                await sender.Send(new RemoveTaskCardCommand(TaskCardType.LoadEmptyRightSampleTray, instrumentNumber), cancellationToken);
            }
        }
    }

    private static bool HasFreeSamplePositions(string trayId, int occupiedPositions, List<SampleTube> externalReservedPositions)
    {
        var reservedPositions = externalReservedPositions.Count(x => x.OriginalPosition!.TrayId == trayId);

        return (occupiedPositions + reservedPositions) < Constants.MaxSampleTubesInTray;
    }

    private async Task NotifySampleDrawerContentsReceived(string instrumentNumber, RepeatedField<DetectedSampleTray> instrumentDetectedTrays)
    {
        var instrumentId = instrumentManager.FindInstrument(instrumentNumber)?.Id;
        if (instrumentId.HasValue)
        {
            var trayIds = instrumentDetectedTrays.Select(x => x.TrayBarcode).ToList();
            await currentInstrumentHub.SampleDrawerContentsReceived(instrumentId.Value, trayIds);
        }
    }

    private async Task<List<DetectedSampleTray>> ValidateInstrumentTrays(string instrumentNumber, DrawerType instrumentDrawer, RepeatedField<DetectedSampleTray> instrumentDetectedTrays, CancellationToken cancellationToken)
    {
        var errorTrays = new List<DetectedSampleTray>();

        var unknownObjects = instrumentDetectedTrays.Where(x => x.SampleTrayError == EnumSampleTrayError.SampleTrayErrorUnknownObject).ToList();
        errorTrays.AddRange(unknownObjects);

        var traysWithUnreadableTrayId = instrumentDetectedTrays.Where(x => x.SampleTrayError == EnumSampleTrayError.SampleTrayErrorUnreadableBarcode).ToList();
        errorTrays.AddRange(traysWithUnreadableTrayId);

        var traysToValidate = instrumentDetectedTrays.Except(errorTrays).ToList();
        foreach (var tray in traysToValidate)
        {
            if (!SampleTray.IsValidTrayId(tray.TrayBarcode))
            {
                tray.SampleTrayError = EnumSampleTrayError.SampleTrayErrorInvalidBarcode;
                errorTrays.Add(tray);
            }
        }

        traysToValidate = instrumentDetectedTrays.Except(errorTrays).ToList();
        // Duplicate trays detected in the same drawer at the same time
        var duplicateTrayGroups = traysToValidate.GroupBy(tray => tray.TrayBarcode).Where(group => group.Count() > 1);

        foreach (var trayGroup in duplicateTrayGroups)
        {
            // Check if any tray is already present in the same instrument and drawer
            var existingTray = sampleStorage.GetTray(trayGroup.Key);
            var isTrayInInstrument = existingTray != null && existingTray.InstrumentNumber == instrumentNumber && existingTray.Drawer == instrumentDrawer;

            if (isTrayInInstrument)
            {
                foreach (var tray in trayGroup)
                {
                    // If this tray's position matches the existing tray, it's the already present one, skip marking as duplicate
                    if (tray.TrayPosition != existingTray?.TrayPosition)
                    {
                        tray.SampleTrayError = EnumSampleTrayError.SampleTrayErrorDuplicateBarcode;
                        errorTrays.Add(tray);
                    }
                }
            }
            else
            {
                foreach (var tray in trayGroup)
                {
                    tray.SampleTrayError = EnumSampleTrayError.SampleTrayErrorDuplicateBarcode;
                    errorTrays.Add(tray);
                }
            }
        }

        traysToValidate = instrumentDetectedTrays.Except(errorTrays).ToList();
        // Duplicate trays detected in the same instrument or across multiple instruments
        foreach (var detectedTray in traysToValidate)
        {
            var trayInInstrument = sampleStorage.GetTray(detectedTray.TrayBarcode);

            if (trayInInstrument is { InstrumentNumber: not null } && (trayInInstrument.InstrumentNumber != instrumentNumber || trayInInstrument.Drawer != instrumentDrawer))
            {
                // The tray is already present in another instrument.
                // Now we need to check if the sample drawer is open.
                // If the drawer is open we assume that the tray has been moved.
                var drawerState = instrumentManager
                    .GetInstrument(trayInInstrument.InstrumentNumber)
                        .DrawersLockState
                            .First(ds => ds.Drawer == (trayInInstrument.Drawer == DrawerType.Sample1 ? DrawerType.Sample1 : DrawerType.Sample2)).LockState;
                if (drawerState != LockState.Open)
                {
                    detectedTray.SampleTrayError = EnumSampleTrayError.SampleTrayErrorDuplicateBarcode;
                    errorTrays.Add(detectedTray);
                }
            }
        }

        await HandleSampleTrayErrorTaskCard(instrumentNumber, instrumentDrawer, errorTrays.Count > 0, cancellationToken);

        return errorTrays;
    }

    private async Task HandleSampleTrayErrorTaskCard(string instrumentNumber, DrawerType instrumentDrawer, bool hasTrayErrors, CancellationToken cancellationToken)
    {
        if (hasTrayErrors)
        {
            await sender.Send(new TaskCardCommand(TaskCardType.SampleTrayError, TaskCardState.Warning, instrumentNumber), cancellationToken);
        }
        else
        {
            // Remove the SampleTrayError task card if other drawer does not contain error trays
            var trays = sampleStorage.GetTrays(instrumentNumber);
            if (!trays.Any(t => t.Error != SampleTrayError.None && t.Drawer != instrumentDrawer))
            {
                await sender.Send(new RemoveTaskCardCommand(TaskCardType.SampleTrayError, instrumentNumber), cancellationToken);
            }
        }
    }

    private async Task HandleTraysAddedToInstrument(string instrumentNumber, DrawerType instrumentDrawer, List<DetectedSampleTray> validInstrumentTrays, CancellationToken cancellationToken)
    {
        foreach (var instrumentTray in validInstrumentTrays)
        {
            var sampleTrayEntity = await dbContext.SampleTrays.Where(tray => tray.Id == instrumentTray.TrayBarcode)
                                                         .Include(pos => pos.SampleTubes)
                                                         .FirstOrDefaultAsync(cancellationToken)
                                                         .ConfigureAwait(false);

            if (sampleTrayEntity == null)
            {
                sampleTrayEntity = new SampleTray
                {
                    Id = instrumentTray.TrayBarcode,
                };

                sampleTrayEntity.AddDomainEvent(new SampleTrayCreatedEvent(sampleTrayEntity));

                dbContext.SampleTrays.Add(sampleTrayEntity);
            }

            sampleTrayEntity.Drawer = instrumentDrawer;
            sampleTrayEntity.InstrumentNumber = instrumentNumber;
            sampleTrayEntity.TrayPosition = instrumentTray.TrayPosition;
            sampleTrayEntity.LastModified = dateTime.Now;
        }
    }

    private async Task HandleTraysRemovedFromInstrument(string instrumentNumber, DrawerType instrumentDrawer, RepeatedField<DetectedSampleTray> instrumentDetectedTrays, CancellationToken cancellationToken)
    {
        var instrumentDetectedTrayIds = instrumentDetectedTrays.Select(x => x.TrayBarcode).ToList();

        var sampleTraysRemovedFromInstrument = await dbContext.SampleTrays
            .Where(tray => tray.InstrumentNumber == instrumentNumber && tray.Drawer == instrumentDrawer && !instrumentDetectedTrayIds.Contains(tray.Id))
            .Include(pos => pos.SampleTubes)
            .ToListAsync(cancellationToken);

        foreach (var tray in sampleTraysRemovedFromInstrument)
        {
            tray.Drawer = null;
            tray.InstrumentNumber = null;
            tray.TrayPosition = null;
            tray.LastModified = dateTime.Now;

            tray.AddDomainEvent(new SampleTrayUpdatedEvent(tray));
        }
    }

    private async Task<List<SampleTray>> GetPredefinedPositions(string instrumentNumber, DrawerType instrumentDrawer, List<DetectedSampleTray> instrumentTrays, List<SampleTray> tubesPlacedInWrongDrawerTrays, CancellationToken cancellationToken)
    {
        var traysWithPredefinedPositions = new List<SampleTray>();

        foreach (var instrumentTray in instrumentTrays)
        {
            var persistedSampleTray = await dbContext.SampleTrays.Where(tray => tray.Id == instrumentTray.TrayBarcode)
                                                                  .Include(pos => pos.SampleTubes)
                                                                  .FirstOrDefaultAsync(cancellationToken)
                                                                  .ConfigureAwait(false);
            if (persistedSampleTray != null)
            {
                var persistedPositions = persistedSampleTray.SampleTubes.Where(x => x.IdentitySource is SampleTubeIdentitySource.ManualInput
                                                                        || x.IdentitySource is SampleTubeIdentitySource.External
                                                                        || x.HighPriority).ToList();
                if (persistedPositions.Count > 0)
                {
                    var predefinedPositions = new List<SampleTube>();

                    var tubesInWrongDrawer = tubesPlacedInWrongDrawerTrays.Where(x => x.Id == instrumentTray.TrayBarcode).SelectMany(x => x.SampleTubes).ToList();

                    foreach (var tube in persistedPositions)
                    {
                        if (instrumentTray.DetectedTubes.Any(x => x.SamplePosition == tube.TubePosition)
                            && !tubesInWrongDrawer.Any(x => x.TubePosition == tube.TubePosition))
                        {
                            tube.TrayPosition = instrumentTray.TrayPosition;
                            tube.InstrumentNumber = instrumentNumber;
                            tube.Area = instrumentDrawer == DrawerType.Sample1 ? SampleArea.SampleDrawer1 : SampleArea.SampleDrawer2;

                            predefinedPositions.Add(tube);

                            UpdateStorage(tube);
                        }
                    }

                    if (predefinedPositions.Count > 0)
                    {
                        traysWithPredefinedPositions.Add(new SampleTray
                        {
                            Id = instrumentTray.TrayBarcode,
                            TrayPosition = instrumentTray.TrayPosition,
                            SampleTubes = predefinedPositions
                        });
                    }
                }
            }
        }

        return traysWithPredefinedPositions;
    }

    private void UpdateStorage(SampleTube predefinedTube)
    {
        // Prepare a new SampleTube instance with predefined details
        var tube = new SampleTube
        {
            InstrumentNumber = predefinedTube.InstrumentNumber,
            Area = predefinedTube.Area,
            TrayPosition = predefinedTube.TrayPosition,
            TrayId = predefinedTube.TrayId,
            TubePosition = predefinedTube.TubePosition,
            TubeId = predefinedTube.TubeId,
            SampleId = predefinedTube.SampleId,
            SampleIdSuffix = predefinedTube.SampleIdSuffix,
            TubeBarcode = predefinedTube.TubeBarcode,
            HighPriority = predefinedTube.HighPriority,
            SampleType = predefinedTube.SampleType,
            Status = string.IsNullOrEmpty(predefinedTube.TubeId) ? SampleTubeStatus.NotIdentified : predefinedTube.Status,
            RequestCreatedDate = predefinedTube.RequestCreatedDate
        };

        sampleStorage.AddOrUpdateTube(
            tube,
            oldTube =>
            {
                oldTube.SampleId = tube.SampleId;
                oldTube.SampleIdSuffix = tube.SampleIdSuffix;
                oldTube.TubeBarcode = tube.TubeBarcode;
                oldTube.HighPriority = tube.HighPriority;
                oldTube.SampleType = tube.SampleType;
                oldTube.Status = tube.Status;
                oldTube.RequestCreatedDate = tube.RequestCreatedDate;
                return oldTube;
            }
        );
    }

    private async Task NotifyPredefinedTubesIdentified(List<SampleTray> traysWithPredefinedPositions, string instrumentNumber, CancellationToken cancellationToken)
    {
        if (traysWithPredefinedPositions.Count > 0)
        {
            var predefinedTubeIds = traysWithPredefinedPositions
                                    .SelectMany(tray => tray.SampleTubes
                                        .Where(x => (x.IdentitySource is SampleTubeIdentitySource.ManualInput or SampleTubeIdentitySource.External)
                                                    && x is { TubeId: not null, SampleId: not null, SampleType: SampleType.Sample })
                                        .Select(x => new SampleTubeInformation(new SampleTubeId(x.TubeId!, x.SampleId!), x.IdentitySource)))
                                    .ToList();

            if (predefinedTubeIds.Count > 0)
            {
                await publisher.Publish(new TubesIdentifiedEvent(instrumentNumber, predefinedTubeIds), cancellationToken);
            }
        }
    }

    private async Task<List<SampleTray>> GetRescanTubes(List<DetectedSampleTray> validInstrumentTrays, CancellationToken cancellationToken)
    {
        var traysWithRescanTubes = new List<SampleTray>();

        // Get all detected tube IDs from all trays
        var allDetectedTubeIds = validInstrumentTrays
            .SelectMany(tray => tray.DetectedTubes)
            .Where(tube => !string.IsNullOrEmpty(tube.TubeId))
            .Select(tube => tube.TubeId)
            .ToHashSet();

        if (allDetectedTubeIds.Count == 0)
        {
            return traysWithRescanTubes;
        }

        // Find tubes in database that have these TubeIds but are removed from trays
        var removedTubesFromTray = await dbContext.SampleTubes
            .Where(tube => allDetectedTubeIds.Contains(tube.TubeId!) &&
                          tube.TrayId == null &&
                          tube.TubePosition == null)
            .Select(tube => tube.TubeId)
            .ToListAsync(cancellationToken);

        if (removedTubesFromTray.Count == 0)
        {
            return traysWithRescanTubes;
        }

        foreach (var tray in validInstrumentTrays)
        {
            var removedTubes = tray.DetectedTubes
                .Where(detectedTube => !string.IsNullOrEmpty(detectedTube.TubeId) && removedTubesFromTray.Contains(detectedTube.TubeId))
                .Select(detectedTube => new SampleTube
                {
                    TubePosition = detectedTube.SamplePosition
                })
                .ToList();

            if (removedTubes.Count > 0)
            {
                traysWithRescanTubes.Add(new SampleTray
                {
                    Id = tray.TrayBarcode,
                    TrayPosition = tray.TrayPosition,
                    SampleTubes = removedTubes
                });
            }
        }

        return traysWithRescanTubes;
    }

    private async Task<SampleDrawerContentsResponse> CreateSampleDrawerContentsResponseAsync(List<SampleTray> traysWithPredefinedPositions, List<DetectedSampleTray> errorInstrumentTrays, List<SampleTray> tubesPlacedInWrongDrawerTrays, List<SampleTray> traysWithRescanTubes, CancellationToken cancellationToken)
    {
        var predefinedTrays = new List<SampleTrayWithPredefinedPositions>();

        foreach (var tray in traysWithPredefinedPositions)
        {
            var predefinedPositions = new List<PredefinedPosition>();
            foreach (var position in tray.SampleTubes)
            {
                //Lot number will be empty and tube type id will be -1 for non QC samples
                var lotNumber = string.Empty;
                var tubeTypeId = -1;

                if (position.SampleType == SampleType.QualityControl && !string.IsNullOrEmpty(position.TubeBarcode))
                {
                    var qcResult = await sender.Send(new IsQualityControlQuery(position.TubeBarcode, position.InstrumentNumber!), cancellationToken);
                    lotNumber = qcResult.LotNumber;
                    tubeTypeId = qcResult.TubeTypeId;
                }

                predefinedPositions.Add(new PredefinedPosition
                {
                    SamplePosition = position.TubePosition ?? 0,
                    TubeId = position.TubeId ?? string.Empty,
                    SampleId = position.SampleId ?? string.Empty,
                    SampleType = !string.IsNullOrEmpty(position.TubeId) ? position.SampleType.ToEnumSampleType() : EnumSampleType.UnspecifiedSampleType,
                    IdentitySource = !string.IsNullOrEmpty(position.TubeId) ? position.IdentitySource.ToEnumIdentitySource() : EnumIdentitySource.UnspecifiedIdentitySource,
                    HighPriority = position.HighPriority,
                    TubeLoadedInWrongDrawer = false,
                    LotNumber = lotNumber,
                    TubeTypeIndex = tubeTypeId
                });
            }
            predefinedTrays.Add(new SampleTrayWithPredefinedPositions
            {
                TrayPosition = tray.TrayPosition ?? 0,
                TubePositions = { predefinedPositions },
            });
        }

        foreach (var tubesPlacedInWrongDrawerTray in tubesPlacedInWrongDrawerTrays)
        {
            var predefinedTray = predefinedTrays.FirstOrDefault(x => x.TrayPosition == tubesPlacedInWrongDrawerTray.TrayPosition);
            if (predefinedTray == null)
            {
                predefinedTray = new SampleTrayWithPredefinedPositions
                {
                    TrayPosition = tubesPlacedInWrongDrawerTray.TrayPosition ?? 0,
                };
                predefinedTrays.Add(predefinedTray);
            }

            foreach (var tube in tubesPlacedInWrongDrawerTray.SampleTubes)
            {
                var errorPosition = new PredefinedPosition
                {
                    SamplePosition = tube.TubePosition ?? 0,
                    TubeId = string.Empty,
                    SampleId = string.Empty,
                    SampleType = EnumSampleType.UnspecifiedSampleType,
                    IdentitySource = EnumIdentitySource.UnspecifiedIdentitySource,
                    HighPriority = false,
                    TubeLoadedInWrongDrawer = true
                };
                predefinedTray.TubePositions.Add(errorPosition);
            }
        }

        foreach (var rescanTubesTray in traysWithRescanTubes)
        {
            var predefinedTray = predefinedTrays.FirstOrDefault(x => x.TrayPosition == rescanTubesTray.TrayPosition);
            if (predefinedTray == null)
            {
                predefinedTray = new SampleTrayWithPredefinedPositions
                {
                    TrayPosition = rescanTubesTray.TrayPosition ?? 0,
                };
                predefinedTrays.Add(predefinedTray);
            }

            foreach (var rescanTube in rescanTubesTray.SampleTubes)
            {
                var existingPosition = predefinedTray.TubePositions.FirstOrDefault(x => x.SamplePosition == rescanTube.TubePosition);
                if (existingPosition == null)
                {
                    var rescanPosition = new PredefinedPosition
                    {
                        SamplePosition = rescanTube.TubePosition ?? 0,
                        TubeId = string.Empty,
                        SampleId = string.Empty,
                        SampleType = EnumSampleType.UnspecifiedSampleType,
                        IdentitySource = EnumIdentitySource.UnspecifiedIdentitySource,
                        HighPriority = false,
                        TubeLoadedInWrongDrawer = false
                    };
                    predefinedTray.TubePositions.Add(rescanPosition);
                }
            }
        }

        foreach (var tray in errorInstrumentTrays)
        {
            predefinedTrays.Add(new SampleTrayWithPredefinedPositions
            {
                TrayPosition = tray.TrayPosition,
                SampleTrayError = tray.SampleTrayError
            });
        }

        return new SampleDrawerContentsResponse
        {
            SampleTrays = { predefinedTrays }
        };
    }

    private List<SampleTray> GetSampleTubesPlacedInWrongDrawerTrays(List<DetectedSampleTray> instrumentTrays, string instrumentNumber, DrawerType instrumentDrawer)
    {
        var traysWithWrongDrawerTubes = new List<SampleTray>();

        var isOutputOnlyDrawer = samplesSettings.Value.Drawer.DrawerMode == DrawerMode.LeftDrawerAsInputAndRightDrawerAsOutput && instrumentDrawer == DrawerType.Sample2;
        if (!isOutputOnlyDrawer)
        {
            return traysWithWrongDrawerTubes;
        }

        foreach (var instrumentTray in instrumentTrays)
        {
            var tubesInWrongDrawer = new List<SampleTube>();

            foreach (var detectedTube in instrumentTray.DetectedTubes)
            {
                var sampleTube = sampleStorage.GetTube(detectedTube.TubeId!);

                var isTubeInWrongDrawer = sampleTube == null || sampleTube.InstrumentNumber != instrumentNumber;
                if (isTubeInWrongDrawer)
                {
                    tubesInWrongDrawer.Add(new SampleTube
                    {
                        TubePosition = detectedTube.SamplePosition,
                    });
                }
            }

            if (tubesInWrongDrawer.Count > 0)
            {
                traysWithWrongDrawerTubes.Add(new SampleTray
                {
                    Id = instrumentTray.TrayBarcode,
                    TrayPosition = instrumentTray.TrayPosition,
                    SampleTubes = tubesInWrongDrawer
                });
            }
        }

        return traysWithWrongDrawerTubes;
    }

    private async Task HandleUnConfirmedManualInputTubes(List<DetectedSampleTray> validInstrumentTrays, CancellationToken cancellationToken)
    {
        var validTrayIds = validInstrumentTrays.Select(x => x.TrayBarcode).ToList();

        var sampleTrays = await dbContext.SampleTrays
             .Where(tray => validTrayIds.Contains(tray.Id))
             .Include(tray => tray.SampleTubes)
             .ToListAsync(cancellationToken);

        foreach (var tray in sampleTrays)
        {
            // Unmap the tubes from the tray in database
            var unconfirmedTubes = tray.SampleTubes
                .Where(tube => tube.IdentitySource == SampleTubeIdentitySource.ConfirmManualInput)
                .ToList();

            foreach (var tube in unconfirmedTubes)
            {
                var requestIdResponse = await sender.Send(new GetRequestIdQuery(tube.SampleId!, tube.RequestCreatedDate), cancellationToken);
                if (requestIdResponse.RequestExists)
                {
                    tube.TrayId = null;
                    tube.TubePosition = null;
                    tube.InstrumentNumber = null;
                }
                else
                {
                    tray.SampleTubes.Remove(tube);
                    dbContext.SampleTubes.Remove(tube);
                }
            }
        }
    }
}