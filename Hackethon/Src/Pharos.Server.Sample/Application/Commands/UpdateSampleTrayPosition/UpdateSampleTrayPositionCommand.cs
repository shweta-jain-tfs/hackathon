using System.Text;
using FluentValidation;
using Microsoft.Extensions.Options;
using Pharos.Contracts.Notifications;
using Pharos.Server.Application.AuditTrail.Events;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Request;
using Pharos.Server.Application.Sample;
using Pharos.Server.Application.Settings.Sample;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Localization;
using Pharos.Server.Sample.Application.Helpers;
using Pharos.Server.Sample.Application.Interfaces;
using Pharos.Server.Sample.Domain;

namespace Pharos.Server.Sample.Application.Commands.UpdateSampleTrayPosition;

public class UpdateSampleTrayPositionCommand : IRequest
{
    public string TrayId { get; set; } = default!;
    public int TubePosition { get; set; }
    public string? TubeBarcode { get; set; }
    public bool HighPriority { get; set; }
}

internal sealed class UpdateSampleTrayPositionCommandHandler(
    ISampleDbContext dbContext,
    ISampleQcIdProvider sampleQcIdProvider,
    ISampleTrayHub sampleTrayHub,
    IInstrumentSampleStorage sampleStorage,
    IInstrumentManager instrumentManager,
    IMediator mediator,
    IOptionsSnapshot<SamplesSettings> samplesSettings)
    : IRequestHandler<UpdateSampleTrayPositionCommand>
{
    private static readonly CompositeFormat _duplicateTubeBarcode = CompositeFormat.Parse(Translations.DuplicateTubeBarcode);

    public async Task Handle(UpdateSampleTrayPositionCommand request, CancellationToken cancellationToken)
    {
        var sampleTray = await dbContext.SampleTrays
                                        .Include(x => x.SampleTubes)
                                        .FirstOrDefaultAsync(x => x.Id == request.TrayId, cancellationToken);

        if (sampleTray == null)
        {
            throw new NotFoundException(nameof(SampleTray), request.TrayId);
        }

        var previousTube = sampleTray.SampleTubes.Where(x => x.TubePosition == request.TubePosition)
                                                .Select(tube => new SampleTube { TubeBarcode = tube.TubeBarcode, IdentitySource = tube.IdentitySource })
                                                .FirstOrDefault();

        ValidateEditingAllowed(request, previousTube);

        var (initialTubeErrorCount, initialAvailablePositionCount) = CalculateErrorAndFreePositionCounts(sampleTray);

        var sampleQCTubeKey = await sampleQcIdProvider.GetSampleQcTubeKey(request.TubeBarcode);

        var previousHighPriority = sampleTray.SampleTubes.FirstOrDefault(x => x.TubePosition == request.TubePosition)?.HighPriority ?? false;

        var affectedTrayIds = await HandleUpdateSampleTrayPosition(request, sampleQCTubeKey, sampleTray, cancellationToken);

        var (finalTubeErrorCount, finalAvailablePositionCount) = CalculateErrorAndFreePositionCounts(sampleTray);

        if (dbContext.HasChanges())
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await NotifyClient(initialTubeErrorCount, finalTubeErrorCount, initialAvailablePositionCount, finalAvailablePositionCount, affectedTrayIds);

            await CreateAuditTrail(request, sampleTray, previousTube);
        }

        await NotifySampleTubePriorityChanged(request, sampleQCTubeKey, previousHighPriority, sampleTray.InstrumentNumber, cancellationToken);

        var instrumentNumber = sampleTray.InstrumentNumber;
        if (!string.IsNullOrEmpty(instrumentNumber))
        {
            await HandleTaskCardForSampleTubeStatus(instrumentNumber, SampleTubeStatus.PartlyCompleted, TaskCardType.SamplePartlyCompleted, cancellationToken);
            await HandleSampleTubeErrorTaskCard(instrumentNumber, cancellationToken);
            await HandleUnloadRequestedTaskCard(instrumentNumber, cancellationToken);
        }
    }

    private void ValidateEditingAllowed(UpdateSampleTrayPositionCommand request, SampleTube? previousTube)
    {
        if (!samplesSettings.Value.Tray.EnableEditingOfSampleId)
        {
            var isBarcodeEmpty = string.IsNullOrWhiteSpace(request.TubeBarcode);
            var isBarcodeUnchanged = previousTube?.TubeBarcode == request.TubeBarcode;

            if (!(isBarcodeEmpty || isBarcodeUnchanged))
            {
                throw new ValidationException(Translations.EditingSampleIdNotAllowed);
            }
        }
    }

    private static (int errorPositionsCount, int freePositionsCount) CalculateErrorAndFreePositionCounts(SampleTray sampleTray)
    {
        var errorPositionsCount = sampleTray.SampleTubes.Count(x => x.Error != SampleTubeError.None);
        var freePositionsCount = Constants.MaxSampleTubesInTray - sampleTray.SampleTubes.Count;
        return (errorPositionsCount, freePositionsCount);
    }

    private async Task<HashSet<string>> HandleUpdateSampleTrayPosition(UpdateSampleTrayPositionCommand request, SampleQcTubeKey sampleQcTubeKey, SampleTray sampleTray, CancellationToken cancellationToken)
    {
        var affectedTrayIds = new HashSet<string>();

        if (!string.IsNullOrEmpty(request.TubeBarcode))
        {
            // Check if the tube with entered barcode already exists in the storage (closed drawer)
            // If yes then throw validation exception to prevent duplicate tube barcodes in the system
            var duplicateTubeInInstrument = sampleStorage.GetTube(sampleQcTubeKey.TubeId);

            if (duplicateTubeInInstrument != null && IsTubeInCloseDrawer(duplicateTubeInInstrument))
            {
                throw new ValidationException(string.Format(null, _duplicateTubeBarcode, duplicateTubeInInstrument.TubeId));
            }

            // Check if the tube with entered barcode already exists in the database (open drawer)
            // If found, unmap the tube from its current location to prevent duplicate tube barcodes across open trays
            var duplicateTubeInDB = GetSampleTubeEntity(sampleQcTubeKey, request.TubePosition, request.TrayId);

            if (duplicateTubeInDB is not null)
            {
                if (!string.IsNullOrEmpty(duplicateTubeInDB.TrayId))
                {
                    affectedTrayIds.Add(duplicateTubeInDB.TrayId);
                }

                await RemoveOrUnmapSampleTubeFromPosition(duplicateTubeInDB, sampleTray);
            }
        }

        var existingTube = sampleTray.SampleTubes.FirstOrDefault(x => x.TubePosition == request.TubePosition);
        if (existingTube is null)
        {
            await AddSampleTubeAsync(request, sampleTray, sampleQcTubeKey, cancellationToken);
        }
        else
        {
            await UpdateSampleTrayPosition(existingTube, sampleTray, sampleQcTubeKey, request, cancellationToken);
        }

        affectedTrayIds.Add(sampleTray.Id);
        return affectedTrayIds;
    }

    private async Task UpdateSampleTrayPosition(SampleTube existingTube, SampleTray sampleTray, SampleQcTubeKey sampleQcTubeKey, UpdateSampleTrayPositionCommand request, CancellationToken cancellationToken)
    {
        var identifiedTubeInDB = !string.IsNullOrWhiteSpace(existingTube.TubeBarcode);
        var hasBarcodeProvided = !string.IsNullOrWhiteSpace(request.TubeBarcode);
        var hasHighPrioritySet = request.HighPriority;

        // dealing with 'Identified' tube in DB
        if (identifiedTubeInDB)
        {
            var barcodeChanged = request.TubeBarcode != existingTube.TubeBarcode;

            if (barcodeChanged)
            {
                await RemoveOrUnmapSampleTubeFromPosition(existingTube, sampleTray);

                if (hasBarcodeProvided || hasHighPrioritySet)
                {
                    await AddSampleTubeAsync(request, sampleTray, sampleQcTubeKey, cancellationToken);
                }
            }
            else
            {
                existingTube.HighPriority = request.HighPriority;
                existingTube.IdentitySource = existingTube.IdentitySource == SampleTubeIdentitySource.ConfirmManualInput
                                                  ? SampleTubeIdentitySource.ManualInput
                                                  : existingTube.IdentitySource;
            }

        }
        // dealing with 'Not Identified' tube in DB
        else
        {
            RemoveSampleTubeFromPosition(existingTube, sampleTray);

            if (hasBarcodeProvided || hasHighPrioritySet)
            {
                await AddSampleTubeAsync(request, sampleTray, sampleQcTubeKey, cancellationToken);
            }

        }
    }

    private async Task AddSampleTubeAsync(UpdateSampleTrayPositionCommand request, SampleTray sampleTray, SampleQcTubeKey sampleQcTubeKey, CancellationToken cancellationToken)
    {
        var newTube = new SampleTube
        {
            TrayId = sampleTray.Id,
            TubePosition = request.TubePosition,
            HighPriority = sampleQcTubeKey.SampleType != SampleType.QualityControl ? request.HighPriority : false,
            InternalError = SampleTubeError.None,
            InstrumentError = SampleTubeError.None,
            InstrumentNumber = sampleTray.InstrumentNumber
        };

        if (!sampleQcTubeKey.UnreadableBarcode)
        {
            newTube.TubeBarcode = request.TubeBarcode;
            newTube.TubeId = sampleQcTubeKey.TubeId;
            newTube.SampleId = sampleQcTubeKey.Id;
            newTube.SampleIdSuffix = sampleQcTubeKey.IdSuffix;
            newTube.SampleType = sampleQcTubeKey.SampleType;
            newTube.Status = SampleTubeStatus.InProgress;
            newTube.IdentitySource = SampleTubeIdentitySource.ManualInput;

            var requestTestOrderStates = await mediator.Send(new GetTestOrderStatesQuery(new HashSet<string> { newTube.SampleId }), cancellationToken);
            if (requestTestOrderStates.TryGetValue(newTube.SampleId, out var testOrderState))
            {
                newTube.RequestCreatedDate = testOrderState.RequestUid.RequestCreatedDate;

                if (testOrderState.TestOrderStates.Count > 0)
                {
                    newTube.UpdateTubeCompletionStatus(testOrderState.TestOrderStates);
                }
            }
        }
        else
        {
            // if high priority is set, the tube is not identified otherwise it is None
            newTube.Status = request.HighPriority ? SampleTubeStatus.NotIdentified : SampleTubeStatus.None;
            newTube.IdentitySource = SampleTubeIdentitySource.BarcodeScanning;
        }

        sampleTray.SampleTubes.Add(newTube);
    }

    private SampleTube? GetSampleTubeEntity(SampleQcTubeKey sampleQcTubeKey, int position, string trayId)
    {
        // Fetch all tubes with matching TubeId excluding unmapped tube
        var allMatchingTubesWithId = dbContext.SampleTubes
            .Where(t => t.TubeId == sampleQcTubeKey.TubeId && !string.IsNullOrEmpty(t.TrayId) && t.TubePosition != null)
            .ToList();

        // Find another tube with the same TubeId in the same tray, but different position
        var duplicateTube = allMatchingTubesWithId.FirstOrDefault(t => t.TrayId == trayId && t.TubePosition != position);

        // Find another tube with the same TubeBarcode in a different tray
        duplicateTube ??= allMatchingTubesWithId.FirstOrDefault(t => t.TrayId != trayId);

        return duplicateTube;
    }

    private async Task RemoveOrUnmapSampleTubeFromPosition(SampleTube tube, SampleTray sampleTray)
    {
        var requestIdResponse = await mediator.Send(new GetRequestIdQuery(tube.SampleId!, tube.RequestCreatedDate));

        if (requestIdResponse.RequestExists)
        {
            UnmapSampleTubeFromPosition(tube, sampleTray);
        }
        else
        {
            RemoveSampleTubeFromPosition(tube, sampleTray);
        }
    }

    private static void UnmapSampleTubeFromPosition(SampleTube tubeToUnmap, SampleTray sampleTray)
    {
        tubeToUnmap.TubePosition = null;
        tubeToUnmap.TrayId = null;
        tubeToUnmap.InstrumentNumber = null;
        sampleTray.SampleTubes.Remove(tubeToUnmap);
    }

    private void RemoveSampleTubeFromPosition(SampleTube tubeToRemove, SampleTray sampleTray)
    {
        sampleTray.SampleTubes.Remove(tubeToRemove);
        dbContext.SampleTubes.Remove(tubeToRemove);
    }

    private bool IsTubeInCloseDrawer(SampleTube sampleTube)
    {
        var drawerType = sampleTube.Area == SampleArea.SampleDrawer1 ? DrawerType.Sample1 : DrawerType.Sample2;

        return instrumentManager.GetInstrument(sampleTube.InstrumentNumber!).DrawersLockState[drawerType].LockState != LockState.Open;
    }

    private async Task NotifyClient(int initialTubeErrorCount, int finalTubeErrorCount, int initialAvailablePositionCount, int finalAvailablePositionCount, HashSet<string> affectedTrayIds)
    {
        await sampleTrayHub.PositionStatusUpdated(affectedTrayIds.ToList());

        var hasTrayCountsChanged = initialTubeErrorCount != finalTubeErrorCount || initialAvailablePositionCount != finalAvailablePositionCount;

        if (hasTrayCountsChanged)
        {
            await sampleTrayHub.FreePositionAndErrorCountUpdated();
        }
    }

    private async Task NotifySampleTubePriorityChanged(UpdateSampleTrayPositionCommand request, SampleQcTubeKey sampleQCTubeKey, bool previousHighPriority, string? instrumentNumber, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(request.TubeBarcode) && previousHighPriority != request.HighPriority)
        {
            var sampleTubeId = new SampleTubeId(sampleQCTubeKey.TubeId, sampleQCTubeKey.Id);
            await mediator.Publish(new TubePriorityChangedEvent(instrumentNumber, sampleTubeId, request.HighPriority), cancellationToken);
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
            await mediator.Send(new RemoveTaskCardCommand(taskCardType, instrumentNumber), cancellationToken);
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
            await mediator.Send(new RemoveTaskCardCommand(TaskCardType.SampleTubeError, instrumentNumber), cancellationToken);
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
            await mediator.Send(new RemoveTaskCardCommand(TaskCardType.RemoveSamplesFromDrawer, instrumentNumber), cancellationToken);
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
                x => x.InstrumentNumber == instrumentNumber
                    && x.UnloadRequested
                    && x.TrayId != null
                    && x.TubePosition != null,
                cancellationToken);
        }

        return sampleStorage.TubesAsQueryable(instrumentNumber).Any(x => x.Area == sampleArea && x.UnloadRequested);
    }

    private async Task<bool> HasTubesWithErrors(string instrumentNumber, DrawerType drawerType, CancellationToken cancellationToken)
    {
        var lockState = instrumentManager.GetInstrument(instrumentNumber).DrawersLockState[drawerType].LockState;
        var sampleArea = drawerType == DrawerType.Sample1 ? SampleArea.SampleDrawer1 : SampleArea.SampleDrawer2;

        bool hasErrors;
        if (lockState == LockState.Open)
        {
            hasErrors = await dbContext.SampleTubes.AnyAsync(x => x.InstrumentNumber == instrumentNumber && (x.InternalError != SampleTubeError.None || x.InstrumentError != SampleTubeError.None) && x.TrayId != null && x.TubePosition != null, cancellationToken);
        }
        else
        {
            hasErrors = sampleStorage.TubesAsQueryable(instrumentNumber).Any(x => x.Area == sampleArea && x.Error != SampleTubeError.None);
        }

        return hasErrors;
    }

    private async Task CreateAuditTrail(UpdateSampleTrayPositionCommand request, SampleTray sampleTray, SampleTube? previousTube)
    {
        var logAuditTrail = false;

        var updatedTube = sampleTray.SampleTubes.FirstOrDefault(x => x.TubePosition == request.TubePosition);
        if (updatedTube != null)
        {
            var hasBarcodeChanged = !string.IsNullOrWhiteSpace(updatedTube.TubeBarcode) && updatedTube.TubeBarcode != previousTube?.TubeBarcode;

            var isConfirmedManualInput = previousTube?.IdentitySource == SampleTubeIdentitySource.ConfirmManualInput
                                            && updatedTube.IdentitySource == SampleTubeIdentitySource.ManualInput;

            logAuditTrail = hasBarcodeChanged || isConfirmedManualInput;
        }

        if (logAuditTrail)
        {
            var auditTrail = SampleTrayPositionEvents.ManuallyEnteredBarcodeEvent.Create(sampleTray.InstrumentNumber!, sampleTray.Id, request.TubePosition, request.TubeBarcode!, DateTimeOffset.Now);
            await mediator.Send(auditTrail.ToCommand());
        }
    }
}
