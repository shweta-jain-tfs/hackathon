using MediatR;
using Pharos.Contracts.Notifications;
using Pharos.Server.Application.Instrument;
using Tetris.Protocol.Pharos;

namespace Pharos.Server.Instrument.Application.Events;

internal record SignalTowerStateChangedEvent(string InstrumentNumber, SignalTowerState Message) : INotification;

internal class SignalTowerStateChangedEventHandler(
    IInstrumentManager instrumentManager,
    ICurrentInstrumentServerHub currentInstrumentHub,
    IInstrumentHub instrumentHub
    ) : INotificationHandler<SignalTowerStateChangedEvent>
{
    public async Task Handle(SignalTowerStateChangedEvent notification, CancellationToken cancellationToken)
    {
        var instrument = (Domain.Instrument)instrumentManager.GetInstrument(notification.InstrumentNumber);

        if (instrument.SignalTower.SetState(notification.Message))
        {
            await currentInstrumentHub.SignalTowerStateChanged(instrument.Id, (int)instrument.SignalTower.LightState, (int)instrument.SignalTower.SoundState);
            await instrumentHub.SignalTowerStateChanged(instrument.Id, (int)instrument.SignalTower.LightState);
        }
    }
}