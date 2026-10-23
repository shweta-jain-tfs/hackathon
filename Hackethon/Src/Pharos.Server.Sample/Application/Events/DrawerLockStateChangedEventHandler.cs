using Microsoft.Extensions.Options;
using Pharos.Contracts.Notifications;
using Pharos.Server.Application.AuditTrail.Events;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Request;
using Pharos.Server.Application.Settings.Sample;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Infrastructure;
using Pharos.Server.Sample.Application.Interfaces;
using Pharos.Server.Sample.Domain;

namespace Pharos.Server.Sample.Application.Events;

internal class DrawerLockStateChangedEventHandler(
    IInstrumentSampleStorage sampleStorage,
    ISampleDbContext dbContext,
    ISampleQcIdProvider sampleIdProvider,
    IOptionsSnapshot<SpecialSampleHandlingSettings> settings,
    ISampleTrayHub sampleTrayHub,
    IDateTime dateTime,
    ISender sender) : INotificationHandler<DrawerLockStateChanged>
{

    private readonly HashSet<string> _modifiedSampleTrayIdList = [];

    public async Task Handle(DrawerLockStateChanged notification, CancellationToken cancellationToken)
    {
        await CreateAuditTrailForDrawerState(notification, DrawerType.Sample1, cancellationToken);
        await CreateAuditTrailForDrawerState(notification, DrawerType.Sample2, cancellationToken);

        await PersistDrawerContentsIfOpen(notification, DrawerType.Sample1, cancellationToken);
        await PersistDrawerContentsIfOpen(notification, DrawerType.Sample2, cancellationToken);

        if (dbContext.HasChanges())
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (_modifiedSampleTrayIdList.Count > 0)
        {
            await sampleTrayHub.PositionStatusUpdated(_modifiedSampleTrayIdList.ToList());
        }
    }

    private async Task CreateAuditTrailForDrawerState(DrawerLockStateChanged notification, DrawerType drawerType, CancellationToken cancellationToken)
    {
        if (notification.PreviousState == null || notification.CurrentState == null)
        {
            return;
        }

        var previousDrawerState = notification.PreviousState.FirstOrDefault(x => x.Drawer == drawerType);
        var currentDrawerState = notification.CurrentState.First(x => x.Drawer == drawerType);

        if (previousDrawerState != null && previousDrawerState.LockState == currentDrawerState.LockState)
        {
            return;
        }

        if (currentDrawerState.LockState == LockState.Open)
        {
            var openedEvent = SampleDrawerEvents.SampleDrawerOpenedEvent.Create(notification.InstrumentNumber, drawerType, dateTime.Now);
            await sender.Send(openedEvent.ToCommand(), cancellationToken);
        }
        else if (currentDrawerState.LockState == LockState.ClosedLocked)
        {
            var closedEvent = SampleDrawerEvents.SampleDrawerClosedEvent.Create(notification.InstrumentNumber, drawerType, dateTime.Now);
            await sender.Send(closedEvent.ToCommand(), cancellationToken);
        }
    }

    private async Task PersistDrawerContentsIfOpen(DrawerLockStateChanged notification, DrawerType drawerType, CancellationToken cancellationToken)
    {
        if (!IsDrawerOpened(notification, drawerType))
        {
            return;
        }

        var trays = sampleStorage.GetTrays(notification.InstrumentNumber, drawerType)
            .Where(x => x.Error == Domain.SampleTrayError.None)
            .ToList();
        if (trays.Count == 0)
        {
            return;
        }

        var trayIds = trays.Select(x => x.Id).ToArray();
        var existingTraysWithTubes = await dbContext.SampleTrays
            .Where(x => trayIds.Contains(x.Id))
            .Include(x => x.SampleTubes)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var instrumentNumber = notification.InstrumentNumber;

        foreach (var tray in trays)
        {
            var existingTray = existingTraysWithTubes.FirstOrDefault(x => x.Id == tray.Id);

            // Normally, trays are created and updated on GetSampleDrawerContents call from the instrument.
            // This is just in case if Pharos and the instrument went out of sync.
            if (existingTray == null)
            {
                existingTray = new SampleTray { Id = tray.Id };
                existingTray.AddDomainEvent(new SampleTrayCreatedEvent(existingTray));
                dbContext.SampleTrays.Add(existingTray);
            }
            else
            {
                existingTray.AddDomainEvent(new SampleTrayUpdatedEvent(existingTray));
            }

            existingTray.Drawer = drawerType;
            existingTray.InstrumentNumber = instrumentNumber;
            existingTray.TrayPosition = tray.TrayPosition;

            var unsavedTubes = sampleStorage.TubesAsQueryable(instrumentNumber)
                .Where(x => x.Area == (drawerType == DrawerType.Sample1 ? SampleArea.SampleDrawer1 : SampleArea.SampleDrawer2) && x.TrayPosition == tray.TrayPosition)
                .ToList();

            var unsavedTubeIds = unsavedTubes
                .Where(x => !string.IsNullOrEmpty(x.TubeId))
                .Select(x => x.TubeId)
                .ToArray();

            // We get the tubes from the database that either have no LoadTimestamp set (never saved)
            // or the latest LoadTimestamp for the tubeId (to compare with the one in the instrument storage).
            // These tubes are not necessarily in the tray, but we need to update them.
            var existingTubes = await dbContext.SampleTubes
                .Where(t => unsavedTubeIds.Contains(t.TubeId) && (t.InstrumentNumber == null || t.InstrumentNumber == instrumentNumber))
                .GroupBy(t => t.TubeId)
                .Select(g => g
                    .OrderByDescending(t => t.LoadTimestamp ?? DateTimeOffset.MinValue)
                    .First(t => t.LoadTimestamp == null || t.LoadTimestamp == g.Max(x => x.LoadTimestamp)))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            foreach (var tube in existingTray.SampleTubes.ToList())
            {
                var isUnidentified = string.IsNullOrEmpty(tube.TubeId);
                var isInUnsaved = unsavedTubeIds.Contains(tube.TubeId);

                if (isUnidentified && !tube.IdentitySource.Equals(SampleTubeIdentitySource.ManualInput))
                {
                    //Remove tubes from the tray that are not identified or do not have a request excluding ManualInput
                    existingTray.SampleTubes.Remove(tube);
                    dbContext.SampleTubes.Remove(tube);
                }
                else if (!isUnidentified && !isInUnsaved)
                {
                    // Remove other identified tubes from the tray if not in unsavedTubes
                    if (tube.RequestCreatedDate != null)
                    {
                        var requestIdResponse = await sender.Send(new GetRequestIdQuery(tube.SampleId!, tube.RequestCreatedDate), cancellationToken);
                        if (requestIdResponse.RequestExists)
                        {
                            tube.TrayId = null;
                            tube.TubePosition = null;
                            tube.InstrumentNumber = null;
                        }
                    }
                    else
                    {
                        dbContext.SampleTubes.Remove(tube);
                    }
                    existingTray.SampleTubes.Remove(tube);
                }
            }

            // update identified tubes existing in the database that are currently in the tray
            foreach (var existingTube in existingTubes)
            {
                var unsavedTube = unsavedTubes.FirstOrDefault(x =>
                    x.TubeId == existingTube.TubeId &&
                    (existingTube.LoadTimestamp == null || x.LoadTimestamp == existingTube.LoadTimestamp));

                if (unsavedTube == null)
                {
                    continue;
                }

                UpdateTube(unsavedTube, existingTube, instrumentNumber);
                dbContext.SampleTubes.Update(existingTube);

                unsavedTubes.Remove(unsavedTube);
            }

            var fixedSampleSuffix = settings.Value is { SampleTubesWithSameSampleId: true, ExtractSampleIdSuffixFromBarcode: true };

            foreach (var unsavedTube in unsavedTubes)
            {
                var newTube = new SampleTube
                {
                    TubeId = unsavedTube.TubeId,
                    SampleId = unsavedTube.SampleId,
                    SampleIdSuffix = fixedSampleSuffix && unsavedTube.Error == SampleTubeError.None && !string.IsNullOrEmpty(unsavedTube.TubeBarcode)
                        ? (await sampleIdProvider.GetSampleQcTubeKey(unsavedTube.TubeBarcode)).IdSuffix
                        : null,
                    SampleType = unsavedTube.SampleType,
                };

                UpdateTube(unsavedTube, newTube, instrumentNumber);

                dbContext.SampleTubes.Add(newTube);
                existingTray.SampleTubes.Add(newTube);
            }

            tray.AddDomainEvent(new SampleTrayUpdatedEvent(tray));

            //TODO: Remove following code once the TrayPanel is refactored to refresh based on the changed in drawer trays.
            _modifiedSampleTrayIdList.Add(tray.Id);
        }
    }

    private static bool IsDrawerOpened(DrawerLockStateChanged notification, DrawerType drawerType)
    {
        var previousState = notification.PreviousState;
        var currentState = notification.CurrentState;

        var drawerLockState = currentState.First(x => x.Drawer == drawerType).LockState;

        return drawerLockState == LockState.Open
            && (previousState == null || previousState.First(x => x.Drawer == drawerType).LockState != drawerLockState);
    }

    private static void UpdateTube(SampleTube source, SampleTube target, string instrumentNumber)
    {
        target.InstrumentNumber = instrumentNumber;
        target.LoadTimestamp = source.LoadTimestamp;
        target.RequestCreatedDate = source.RequestCreatedDate;
        target.HighPriority = source.HighPriority;
        target.Status = source.Status;
        target.UnloadRequested = source.UnloadRequested;
        target.InstrumentError = source.InstrumentError;
        target.InternalError = source.InternalError;
        target.SampleVolume = source.SampleVolume;
        target.ImageIdentity = source.ImageIdentity;
        target.TrayId = source.TrayId;
        target.TubePosition = source.TubePosition;
        target.SampleType = source.SampleType;

        // If the tube was identified via manual input, TubeBarcode is set by the user and is not updated by InstrumentSampleStorage.
        // (TubeId and SampleId may still be updated in InstrumentSampleStorage, as they are sent as PredefinedTubes.)
        // If TubeBarcode is already set in the target, do not overwrite it.
        // If the instrument ever changes TubeBarcode, a new tube entry will be created, so this logic is safe.
        if (string.IsNullOrEmpty(target.TubeBarcode))
        {
            target.TubeBarcode = source.TubeBarcode;
            target.SampleIdSuffix = source.SampleIdSuffix;
        }

        target.IdentitySource = source.IdentitySource == SampleTubeIdentitySource.ManualInput
                      ? SampleTubeIdentitySource.ConfirmManualInput
                      : source.IdentitySource;
    }
}