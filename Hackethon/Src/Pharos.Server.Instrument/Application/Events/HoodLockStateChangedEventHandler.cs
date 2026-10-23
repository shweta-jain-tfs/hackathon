using MediatR;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Instrument.Domain;

namespace Pharos.Server.Instrument.Application.Events;

internal record HoodLockStateChanged(string InstrumentNumber, HoodLockState PreviousLockState, HoodLockState CurrentLockState) : INotification;

internal class HoodLockStateChangedEventHandler(IInstrumentManager instrumentManager, ICurrentInstrumentServerHub currentInstrumentHub) : INotificationHandler<HoodLockStateChanged>
{
    public Task Handle(HoodLockStateChanged notification, CancellationToken cancellationToken)
    {
        var instrumentId = instrumentManager.GetInstrument(notification.InstrumentNumber).Id;

        return currentInstrumentHub.HoodLockStateChanged(instrumentId, (int)notification.CurrentLockState.LockState, notification.CurrentLockState.IsUnlockInProgress);
    }

    //TODO: Create Audit Trail Event for Hood Lock State Change
}
