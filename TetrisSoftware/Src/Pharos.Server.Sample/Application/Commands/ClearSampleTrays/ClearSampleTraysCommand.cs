using Pharos.Contracts.Notifications;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Request;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Sample.Application.Interfaces;
using Pharos.Server.Sample.Domain;

namespace Pharos.Server.Sample.Application.Commands.ClearSampleTrays;

internal record ClearSampleTraysCommand(string[] SampleTrayIds) : IRequest<List<string>>;

internal sealed class ClearSampleTraysCommandHandler(ISampleDbContext dbContext, ISampleTrayHub sampleTrayHub, IInstrumentSampleStorage sampleStorage,
    IInstrumentManager instrumentManager, ISender sender) : IRequestHandler<ClearSampleTraysCommand, List<string>>
{
    public async Task<List<string>> Handle(ClearSampleTraysCommand request, CancellationToken cancellationToken)
    {
        var sampleTrays = await dbContext.SampleTrays
            .Where(x => request.SampleTrayIds.Contains(x.Id))
            .Include(x => x.SampleTubes)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (sampleTrays.Count == 0)
        {
            return new List<string>();
        }

        var clearedTrays = new List<SampleTray>();

        foreach (var tray in sampleTrays)
        {
            // Skip clearing if tray is present in the instrument and drawer is closed
            if (IsTrayInInstrumentAndDrawerClosed(tray.Id))
            {
                continue;
            }

            foreach (var tube in tray.SampleTubes)
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
                    dbContext.SampleTubes.Remove(tube);
                }
            }

            tray.SampleTubes.Clear();
            clearedTrays.Add(tray);
        }

        if (dbContext.HasChanges())
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (clearedTrays.Count > 0)
        {
            await NotifySampleTrayPositionsCleared(clearedTrays);
            await HandleTaskCard(clearedTrays, cancellationToken);
        }

        return clearedTrays.Select(x => x.Id).ToList();
    }

    private bool IsTrayInInstrumentAndDrawerClosed(string trayId)
    {
        var tray = sampleStorage.GetTray(trayId);
        if (tray == null)
        {
            return false;
        }

        return instrumentManager.GetInstrument(tray.InstrumentNumber!).DrawersLockState[tray.Drawer!.Value].LockState != LockState.Open;
    }

    private async Task NotifySampleTrayPositionsCleared(List<SampleTray> sampleTrays)
    {
        var trayIds = sampleTrays.Select(x => x.Id).ToList();

        await sampleTrayHub.PositionStatusUpdated(trayIds);
        await sampleTrayHub.FreePositionAndErrorCountUpdated();
    }

    private async Task HandleTaskCard(List<SampleTray> sampleTrays, CancellationToken cancellationToken)
    {
        foreach (var instrumentNumber in sampleTrays.Select(x => x.InstrumentNumber).Distinct())
        {
            if (instrumentNumber != null)
            {
                await HandleTaskCardForSampleTubeStatus(instrumentNumber, SampleTubeStatus.PartlyCompleted, TaskCardType.SamplePartlyCompleted, cancellationToken);
                await HandleSampleTubeErrorTaskCard(instrumentNumber, cancellationToken);
                await HandleUnloadRequestedTaskCard(instrumentNumber, cancellationToken);
            }
        }
    }

    private async Task HandleSampleTubeErrorTaskCard(string instrumentNumber, CancellationToken cancellationToken)
    {
        var hasErrorTubes = await HasTubesWithErrors(instrumentNumber, DrawerType.Sample1, cancellationToken);

        if (hasErrorTubes)
        {
            return;
        }

        hasErrorTubes = await HasTubesWithErrors(instrumentNumber, DrawerType.Sample2, cancellationToken);

        if (!hasErrorTubes)
        {
            await sender.Send(new RemoveTaskCardCommand(TaskCardType.SampleTubeError, instrumentNumber), cancellationToken);
        }
    }

    private async Task HandleTaskCardForSampleTubeStatus(string instrumentNumber, SampleTubeStatus status, TaskCardType taskCardType, CancellationToken cancellationToken)
    {
        var hasSampleTubesWithStatus = await HasSampleTubesWithStatus(instrumentNumber, DrawerType.Sample1, status, cancellationToken);

        if (!hasSampleTubesWithStatus)
        {
            hasSampleTubesWithStatus = await HasSampleTubesWithStatus(instrumentNumber, DrawerType.Sample2, status, cancellationToken);
        }

        if (!hasSampleTubesWithStatus)
        {
            await sender.Send(new RemoveTaskCardCommand(taskCardType, instrumentNumber), cancellationToken);
        }
    }

    private async Task HandleUnloadRequestedTaskCard(string instrumentNumber, CancellationToken cancellationToken)
    {
        var hasUnloadRequestedTubes = await HasUnloadRequestedTubes(instrumentNumber, DrawerType.Sample1, cancellationToken);

        if (!hasUnloadRequestedTubes)
        {
            hasUnloadRequestedTubes = await HasUnloadRequestedTubes(instrumentNumber, DrawerType.Sample2, cancellationToken);
        }

        if (!hasUnloadRequestedTubes)
        {
            await sender.Send(new RemoveTaskCardCommand(TaskCardType.RemoveSamplesFromDrawer, instrumentNumber), cancellationToken);
        }
    }

    private async Task<bool> HasSampleTubesWithStatus(string instrumentNumber, DrawerType drawerType, SampleTubeStatus status, CancellationToken cancellationToken)
    {
        var lockState = instrumentManager.GetInstrument(instrumentNumber).DrawersLockState[drawerType].LockState;
        var sampleArea = drawerType == DrawerType.Sample1 ? SampleArea.SampleDrawer1 : SampleArea.SampleDrawer2;
        if (lockState == LockState.Open)
        {
            return await dbContext.SampleTubes.AnyAsync(x => x.InstrumentNumber == instrumentNumber && x.Status == status && x.TrayId != null && x.TubePosition != null, cancellationToken);
        }
        else
        {
            var tubes = sampleStorage.TubesAsQueryable(instrumentNumber).ToList();
            return tubes.Exists(x => x.Area == sampleArea && x.Status == status);
        }
    }

    private async Task<bool> HasUnloadRequestedTubes(string instrumentNumber, DrawerType drawerType, CancellationToken cancellationToken)
    {
        var lockState = instrumentManager.GetInstrument(instrumentNumber).DrawersLockState[drawerType].LockState;
        var sampleArea = drawerType == DrawerType.Sample1 ? SampleArea.SampleDrawer1 : SampleArea.SampleDrawer2;

        if (lockState == LockState.Open)
        {
            return await dbContext.SampleTubes.AnyAsync(
                tube => tube.InstrumentNumber == instrumentNumber
                    && tube.UnloadRequested
                    && tube.TrayId != null
                    && tube.TubePosition != null,
                cancellationToken);
        }

        return sampleStorage.TubesAsQueryable(instrumentNumber).Any(tube => tube.Area == sampleArea && tube.UnloadRequested);
    }

    private async Task<bool> HasTubesWithErrors(string instrumentNumber, DrawerType drawerType, CancellationToken cancellationToken)
    {
        var lockState = instrumentManager.GetInstrument(instrumentNumber).DrawersLockState[drawerType].LockState;
        var sampleArea = drawerType == DrawerType.Sample1 ? SampleArea.SampleDrawer1 : SampleArea.SampleDrawer2;

        bool hasErrors;

        if (lockState == LockState.Open)
        {
            hasErrors = await dbContext.SampleTubes.AnyAsync(x => x.InstrumentNumber == instrumentNumber &&
                (x.InternalError != SampleTubeError.None || x.InstrumentError != SampleTubeError.None) &&
                x.TrayId != null && x.TubePosition != null, cancellationToken);
        }
        else
        {
            hasErrors = sampleStorage.TubesAsQueryable(instrumentNumber).Any(x => x.Area == sampleArea && x.Error != SampleTubeError.None);
        }

        return hasErrors;
    }
}