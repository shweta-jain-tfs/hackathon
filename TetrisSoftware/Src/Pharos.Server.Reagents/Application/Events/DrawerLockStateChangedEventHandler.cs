using MediatR;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Reagents.Application.Services;

namespace Pharos.Server.Reagents.Application.Events;

internal class DrawerLockStateChangedEventHandler(ICheckDrawerTaskCardService checkDrawerTaskCardService) : INotificationHandler<DrawerLockStateChanged>
{
    public async Task Handle(DrawerLockStateChanged notification, CancellationToken cancellationToken)
    {
        if (HasDrawerLockStateChangedToClosed(notification, DrawerType.RinseWaste, LockState.ClosedUnlocked))
        {
            await checkDrawerTaskCardService.UpdateCheckLiquidDrawerTaskCard(notification.InstrumentNumber, cancellationToken);
        }

        if (HasDrawerLockStateChangedToClosed(notification, DrawerType.SolidWaste, LockState.ClosedLocked))
        {
            await checkDrawerTaskCardService.UpdateCheckWasteDrawerTaskCard(notification.InstrumentNumber, cancellationToken);
        }
    }

    private static bool HasDrawerLockStateChangedToClosed(DrawerLockStateChanged notification, DrawerType drawerType, LockState closedLockState)
    {
        var currentLockState = notification.CurrentState.First(x => x.Drawer == drawerType).LockState;
        var previousLockState = notification.PreviousState?.FirstOrDefault(x => x.Drawer == drawerType)?.LockState;

        return currentLockState == closedLockState
            && (previousLockState == null || previousLockState != currentLockState);
    }
}
