using MediatR;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Instrument.Application.Events;
using Pharos.Server.Instrument.Domain;
using Tetris.Protocol.Common;

namespace Pharos.Server.Instrument.Application.Commands.UnlockCompartment;

internal record CompartmentLockStateChangedCommand(string InstrumentNumber, EnumInstrumentCompartmentWithLock Compartment) : IRequest;

internal class CompartmentLockStateChangeCommandHandler(IInstrumentManager instrumentManager, IMediator mediator) : IRequestHandler<CompartmentLockStateChangedCommand>
{
    public async Task Handle(CompartmentLockStateChangedCommand request, CancellationToken cancellationToken)
    {
        var instrument = (Domain.Instrument)instrumentManager.GetInstrument(request.InstrumentNumber);

        if (request.Compartment == EnumInstrumentCompartmentWithLock.CompartmentHood)
        {
            var previousHoodLockState = instrument.HoodLockState;
            instrument.HoodLockState = new HoodLockState(previousHoodLockState.LockState, true);

            await mediator.Publish(new HoodLockStateChanged(instrument.SerialNumber, previousHoodLockState, instrument.HoodLockState), cancellationToken);
            return;
        }

        var previousDrawersState = instrument.DrawersLockState;
        var currentDrawersState = new DrawerLockStateList();

        foreach (var drawer in Enum.GetValues<DrawerType>())
        {
            var lockState = LockState.ClosedLocked;
            DateTimeOffset? openedAt = null;
            var isUnlockInProgress = previousDrawersState?.First(x => x.Drawer == drawer).IsUnlockInProgress ?? false;

            var previousState = previousDrawersState?.FirstOrDefault(x => x.Drawer == drawer);
            if (previousState != null)
            {
                lockState = previousState.LockState;
                if (lockState == LockState.Open)
                {
                    openedAt = previousState.OpenedAt;
                }
            }

            isUnlockInProgress = request.Compartment switch
            {
                EnumInstrumentCompartmentWithLock.CompartmentSampleDrawer1 when drawer == DrawerType.Sample1 => true,
                EnumInstrumentCompartmentWithLock.CompartmentSampleDrawer2 when drawer == DrawerType.Sample2 => true,
                EnumInstrumentCompartmentWithLock.CompartmentReagentDrawer when drawer == DrawerType.Reagent => true,
                EnumInstrumentCompartmentWithLock.CompartmentSolidWasteDrawer when drawer == DrawerType.SolidWaste => true,
                _ => isUnlockInProgress
            };

            currentDrawersState.Add(new DrawerLockState(drawer, lockState, isUnlockInProgress, openedAt));
        }

        if (previousDrawersState == null)
        {
            instrument.DrawersLockState = currentDrawersState;
            await mediator.Publish(new DrawerLockStateChanged(instrument.SerialNumber, null, currentDrawersState), cancellationToken);
        }
        else
        {
            foreach (var currentDrawerState in currentDrawersState)
            {
                var previousDrawerState = previousDrawersState.FirstOrDefault(x => x.Drawer == currentDrawerState.Drawer);
                if (previousDrawerState != null && (previousDrawerState.LockState != currentDrawerState.LockState || currentDrawerState.IsUnlockInProgress))
                {
                    instrument.DrawersLockState = currentDrawersState;
                    await mediator.Publish(new DrawerLockStateChanged(instrument.SerialNumber, previousDrawersState, currentDrawersState), cancellationToken);
                    break;
                }
            }
        }
    }
}

