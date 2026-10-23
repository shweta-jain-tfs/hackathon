using Pharos.Contracts.Notifications;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Request;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Infrastructure;
using Pharos.Server.Sample.Application.Events;
using Pharos.Server.Sample.Application.Interfaces;
using Pharos.Server.Sample.Application.Mapping;
using Pharos.Server.Sample.Domain;

namespace Pharos.Server.Sample.Application.Commands.SetTrayNotInInstrument;

internal record SetTrayNotInInstrumentCommand(string TrayId) : IRequest;

internal sealed class SetTrayNotInInstrumentCommandHandler(
    ISampleDbContext dbContext,
    ISender sender,
    IInstrumentSampleStorage sampleStorage,
    IDateTime dateTime,
    IInstrumentManager instrumentManager,
    ISampleTrayHub sampleTrayHub) : IRequestHandler<SetTrayNotInInstrumentCommand>
{
    public async Task Handle(SetTrayNotInInstrumentCommand request, CancellationToken cancellationToken)
    {
        var storageTray = sampleStorage.GetTray(request.TrayId);
        if (storageTray is null)
        {
            return;
        }

        var drawerState = instrumentManager.GetInstrument(storageTray.InstrumentNumber!).DrawersLockState[storageTray.Drawer!.Value].LockState;
        if (drawerState == LockState.Open)
        {
            return;
        }

        await PersistTrayContentsFromStorage(storageTray, cancellationToken);
        await UnmapTrayFromInstrument(storageTray.Id, cancellationToken);

        sampleStorage.UpdateTrayAsUnknownObject(request.TrayId);

        if (dbContext.HasChanges())
        {
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        await sampleTrayHub.PositionStatusUpdated([request.TrayId]);

        await sender.Send(new SendTrayNotUsableAllInstrumentsCommand(request.TrayId), cancellationToken);
    }

    private async Task UnmapTrayFromInstrument(string trayId, CancellationToken cancellationToken)
    {
        var tray = await dbContext.SampleTrays
          .Where(x => x.Id == trayId)
          .FirstOrDefaultAsync(cancellationToken)
          .ConfigureAwait(false);

        if (tray == null)
        {
            return;
        }

        tray.InstrumentNumber = null;
        tray.Drawer = null;
        tray.TrayPosition = null;
        tray.LastModified = dateTime.Now;
    }

    private async Task PersistTrayContentsFromStorage(SampleTray storageTray, CancellationToken cancellationToken)
    {
        var existingTray = await dbContext.SampleTrays
          .Where(x => x.Id == storageTray.Id)
          .Include(x => x.SampleTubes)
          .FirstOrDefaultAsync(cancellationToken)
          .ConfigureAwait(false);

        if (existingTray == null)
        {
            existingTray = new SampleTray { Id = storageTray.Id };
            existingTray.AddDomainEvent(new SampleTrayCreatedEvent(existingTray));
            dbContext.SampleTrays.Add(existingTray);
        }
        else
        {
            existingTray.AddDomainEvent(new SampleTrayUpdatedEvent(existingTray));
        }

        existingTray.InstrumentNumber = storageTray.InstrumentNumber;
        existingTray.Drawer = storageTray.Drawer;
        existingTray.TrayPosition = storageTray.TrayPosition;

        var area = storageTray.Drawer == DrawerType.Sample1 ? SampleArea.SampleDrawer1 : SampleArea.SampleDrawer2;

        var unsavedTubes = sampleStorage.TubesAsQueryable(storageTray.InstrumentNumber!)
            .Where(x => x.Area == area && x.TrayPosition == storageTray.TrayPosition && x.TrayId == storageTray.Id)
            .ToList();
        if (unsavedTubes.Count == 0)
        {
            return;
        }

        await PersistTubes(existingTray, unsavedTubes, cancellationToken);
    }

    private async Task PersistTubes(SampleTray existingTray, List<SampleTube> unsavedTubes, CancellationToken cancellationToken)
    {
        var unsavedTubeIds = unsavedTubes.Where(x => !string.IsNullOrEmpty(x.TubeId))
            .Select(x => x.TubeId)
            .ToArray();

        var existingTubes = await dbContext.SampleTubes
           .Where(t => unsavedTubeIds.Contains(t.TubeId) && (t.InstrumentNumber == null || t.InstrumentNumber == existingTray.InstrumentNumber!))
           .GroupBy(t => t.TubeId)
           .Select(g => g.OrderByDescending(t => t.LoadTimestamp ?? DateTimeOffset.MinValue)
               .First(t => t.LoadTimestamp == null || t.LoadTimestamp == g.Max(x => x.LoadTimestamp)))
           .ToListAsync(cancellationToken)
           .ConfigureAwait(false);

        //Remove tubes from the tray that are not in the unsavedTubes or are unidentified
        foreach (var tube in existingTray.SampleTubes)
        {
            var isUnidentified = string.IsNullOrEmpty(tube.TubeId) || tube.RequestCreatedDate == null;
            var isInUnsaved = unsavedTubeIds.Contains(tube.TubeId);

            if (isUnidentified && !isInUnsaved)
            {
                //Remove tubes from the tray that are not identified or do not have a request and not in unsavedTubes
                existingTray.SampleTubes.Remove(tube);
                dbContext.SampleTubes.Remove(tube);
            }
            else if (!isUnidentified && !isInUnsaved)
            {
                //Remove other identified tubes from the tray if not in unsavedTubes
                var requestIdResponse = await sender.Send(new GetRequestIdQuery(tube.SampleId!, tube.RequestCreatedDate), cancellationToken);
                if (requestIdResponse.RequestExists)
                {
                    tube.TrayId = null;
                    tube.TubePosition = null;
                    tube.InstrumentNumber = null;
                }
                else
                {
                    dbContext.SampleTubes.Remove(tube);
                }

                existingTray.SampleTubes.Remove(tube);
            }
        }

        //Update identified tubes existing in the database that are currently in the tray
        foreach (var existingTube in existingTubes)
        {
            var unsavedTube = unsavedTubes.FirstOrDefault(x => x.TubeId == existingTube.TubeId &&
                (existingTube.LoadTimestamp == null || x.LoadTimestamp == existingTube.LoadTimestamp));

            if (unsavedTube == null)
            {
                continue;
            }

            unsavedTube.UpdateSampleTubeFromStorage(existingTube);
            dbContext.SampleTubes.Update(existingTube);

            unsavedTubes.Remove(unsavedTube);
        }

        foreach (var unsavedTube in unsavedTubes)
        {
            var newTube = new SampleTube();
            unsavedTube.UpdateSampleTubeFromStorage(newTube);

            dbContext.SampleTubes.Add(newTube);
            existingTray.SampleTubes.Add(newTube);
        }
    }
}