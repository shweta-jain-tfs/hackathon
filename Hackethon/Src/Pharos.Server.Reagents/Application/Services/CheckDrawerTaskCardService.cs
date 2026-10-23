using MediatR;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Domain;
using Pharos.Server.Reagents.Application.Interfaces;
using Tetris.Protocol.Pharos.Storage;

namespace Pharos.Server.Reagents.Application.Services;

internal interface ICheckDrawerTaskCardService
{
    Task UpdateCheckLiquidDrawerTaskCard(string instrumentNumber, CancellationToken cancellationToken);
    Task UpdateCheckWasteDrawerTaskCard(string instrumentNumber, CancellationToken cancellationToken);
}

internal class CheckDrawerTaskCardService(
    IInstrumentReagentStorage reagentStorage,
    IInstrumentManager instrumentManager,
    IMediator mediator) : ICheckDrawerTaskCardService
{
    public async Task UpdateCheckLiquidDrawerTaskCard(string instrumentNumber, CancellationToken cancellationToken)
    {
        var liquidWasteContainers = reagentStorage.GetLiquidWaste(instrumentNumber);
        var rinseContainers = reagentStorage.GetRinseContainers(instrumentNumber);

        var lockState = instrumentManager.GetInstrument(instrumentNumber).DrawersLockState[DrawerType.RinseWaste].LockState;
        var isDrawerClosed = lockState is LockState.ClosedUnlocked;

        var missingWasteCount = Constants.MaxLiquidWasteContainers - liquidWasteContainers.Count;
        var missingRinseCount = Constants.MaxRinseContainers - rinseContainers.Count;

        var wasteErrorCount = liquidWasteContainers.Count(HasWasteError);
        var rinseErrorCount = rinseContainers.Count(HasRinseError);

        if (wasteErrorCount == 0 && rinseErrorCount == 0 && missingWasteCount == 0 && missingRinseCount == 0)
        {
            await mediator.Send(new RemoveTaskCardCommand(TaskCardType.CheckLiquidDrawer, instrumentNumber), cancellationToken);
            return;
        }

        if (!isDrawerClosed && wasteErrorCount == 0 && rinseErrorCount == 0)
        {
            return;
        }

        var bothWasteIssues = (missingWasteCount + wasteErrorCount) >= Constants.MaxLiquidWasteContainers;
        var bothRinseIssues = (missingRinseCount + rinseErrorCount) >= Constants.MaxRinseContainers;

        var isResting = IsInstrumentInRestingState(instrumentNumber);

        var cardState = (!isResting && (bothWasteIssues || bothRinseIssues)) ? TaskCardState.Error : TaskCardState.Warning;

        await mediator.Send(new TaskCardCommand(TaskCardType.CheckLiquidDrawer, cardState, instrumentNumber), cancellationToken);
    }

    public async Task UpdateCheckWasteDrawerTaskCard(string instrumentNumber, CancellationToken cancellationToken)
    {
        var wasteContainers = reagentStorage.GetSolidWaste(instrumentNumber);

        var lockState = instrumentManager.GetInstrument(instrumentNumber).DrawersLockState[DrawerType.SolidWaste].LockState;
        var isDrawerClosedLocked = lockState is LockState.ClosedLocked;

        var biohazardMissing = !wasteContainers.Any(wc => wc.Type == EnumWasteContainerType.WasteContainerTypeBioHazard);
        var recyclableMissing = !wasteContainers.Any(wc => wc.Type == EnumWasteContainerType.WasteContainerTypeRecyclable);
        var anyBinHasError = wasteContainers.Any(HasWasteError);

        if (!biohazardMissing && !recyclableMissing && !anyBinHasError)
        {
            await mediator.Send(new RemoveTaskCardCommand(TaskCardType.CheckWasteDrawer, instrumentNumber), cancellationToken);
            return;
        }

        if (!isDrawerClosedLocked && !anyBinHasError)
        {
            return;
        }

        var isResting = IsInstrumentInRestingState(instrumentNumber);

        var cardState = isResting ? TaskCardState.Warning : TaskCardState.Error;

        await mediator.Send(new TaskCardCommand(TaskCardType.CheckWasteDrawer, cardState, instrumentNumber), cancellationToken);
    }

    private bool IsInstrumentInRestingState(string instrumentNumber)
    {
        var instrumentState = instrumentManager.GetInstrument(instrumentNumber).State;
        return instrumentState is InstrumentState.PowerSaving
            or InstrumentState.Startup
            or InstrumentState.Standby
            or InstrumentState.Shutdown;
    }

    private static bool HasWasteError(WasteContainer container)
    {
        return container.Error == EnumWasteContainerError.WasteContainerVolumeReadError;
    }

    private static bool HasRinseError(RinseContainer container)
    {
        return container.Error == EnumRinseBottleError.RinseBottleVolumeReadError ||
               container.Error == EnumRinseBottleError.RinseBottleCannotPumpLiquidError;
    }
}
