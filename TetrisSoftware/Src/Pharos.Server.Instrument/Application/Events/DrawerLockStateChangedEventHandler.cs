using MediatR;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Domain.Enums;
using ICurrentInstrumentServerHub = Pharos.Server.Application.Instrument.ICurrentInstrumentServerHub;

namespace Pharos.Server.Instrument.Application.Events;

internal class DrawerLockStateChangedEventHandler(IInstrumentManager instrumentManager, ICurrentInstrumentServerHub currentInstrumentHub) : INotificationHandler<DrawerLockStateChanged>
{
    public async Task Handle(DrawerLockStateChanged notification, CancellationToken cancellationToken)
    {
        var instrumentId = (instrumentManager.GetInstrument(notification.InstrumentNumber)).Id;

        var previousStateDict = notification.PreviousState?.ToDictionary(x => x.Drawer);
        var currentStateDict = notification.CurrentState.ToDictionary(x => x.Drawer);

        await ProcessDrawerWithLockStateChange(
            instrumentId,
            DrawerType.Sample1,
            previousStateDict,
            currentStateDict,
            (id, lockState, unlockInProgress) => currentInstrumentHub.LeftSampleDrawerLockStateChanged(id, (int)lockState, unlockInProgress));

        await ProcessDrawerWithLockStateChange(
            instrumentId,
            DrawerType.Sample2,
            previousStateDict,
            currentStateDict,
            (id, lockState, unlockInProgress) => currentInstrumentHub.RightSampleDrawerLockStateChanged(id, (int)lockState, unlockInProgress));

        await ProcessDrawerWithLockStateChange(
            instrumentId,
            DrawerType.Reagent,
            previousStateDict,
            currentStateDict,
            (id, lockState, unlockInProgress) => currentInstrumentHub.ReagentDrawerLockStateChanged(id, (int)lockState, unlockInProgress));

        await ProcessDrawerWithoutLockStateChange(
            instrumentId,
            DrawerType.BottledReagent,
            previousStateDict,
            currentStateDict,
            (id, lockState) => currentInstrumentHub.BottleDrawerLockStateChanged(id, (int)lockState));

        await ProcessDrawerWithoutLockStateChange(
            instrumentId,
            DrawerType.RinseWaste,
            previousStateDict,
            currentStateDict,
            (id, lockState) => currentInstrumentHub.LiquidDrawerLockStateChanged(id, (int)lockState));


        await ProcessDrawerWithLockStateChange(
            instrumentId,
            DrawerType.SolidWaste,
            previousStateDict,
            currentStateDict,
            (id, lockState, unlockInProgress) => currentInstrumentHub.WasteDrawerLockStateChanged(id, (int)lockState, unlockInProgress));
    }

    private static async Task ProcessDrawerWithLockStateChange(
        int instrumentId,
        DrawerType drawerType,
        Dictionary<DrawerType, DrawerLockState>? previousStateDict,
        Dictionary<DrawerType, DrawerLockState> currentStateDict,
        Func<int, LockState, bool, Task> notifyAction)
    {
        var currentState = currentStateDict[drawerType];
        var previousState = previousStateDict?.GetValueOrDefault(drawerType);

        if (previousState == null ||
            previousState.LockState != currentState.LockState ||
            previousState.IsUnlockInProgress != currentState.IsUnlockInProgress)
        {
            await notifyAction(instrumentId, currentState.LockState, currentState.IsUnlockInProgress);
        }
    }

    private static async Task ProcessDrawerWithoutLockStateChange(
        int instrumentId,
        DrawerType drawerType,
        Dictionary<DrawerType, DrawerLockState>? previousStateDict,
        Dictionary<DrawerType, DrawerLockState> currentStateDict,
        Func<int, LockState, Task> notifyAction)
    {
        var currentLockState = currentStateDict[drawerType].LockState;
        var previousLockState = previousStateDict?.GetValueOrDefault(drawerType)?.LockState;

        if (previousLockState == null || previousLockState != currentLockState)
        {
            await notifyAction(instrumentId, currentLockState);
        }
    }
}