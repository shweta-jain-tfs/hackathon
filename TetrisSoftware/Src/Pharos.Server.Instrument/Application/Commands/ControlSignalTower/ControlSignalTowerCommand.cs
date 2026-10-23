using MediatR;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Instrument.Application.Interfaces;
using Pharos.Server.Instrument.Domain;
using Tetris.Protocol.Common;
using Tetris.Protocol.Instrument.Control;

namespace Pharos.Server.Instrument.Application.Commands.ControlSignalTower;

internal record ControlSignalTowerCommand(string InstrumentNumber, ControlSignalTowerRequest ControlSignalTowerRequest) : IRequest, INotification, IInstrumentCommand;

internal class ControlSignalTowerCommandHandler : InstrumentCommandHandlerBase<ControlSignalTowerCommand>, INotificationHandler<ControlSignalTowerCommand>
{
    private readonly IInstrumentManager _instrumentManager;

    public ControlSignalTowerCommandHandler(IInstrumentManager instrumentManager, ICommunicationSessionManager sessionManager) : base(sessionManager)
    {
        _instrumentManager = instrumentManager;
    }

    protected override bool ThrowIfNotAllowed => false;

    protected override async Task Handle(ControlSignalTowerCommand request, CommunicationSession session, CancellationToken cancellationToken)
    {
        var instrument = (Domain.Instrument)_instrumentManager.GetInstrument(request.InstrumentNumber);

        // Copy incoming request.
        var controlRequest = new ControlSignalTowerRequest
        {
            SetLight = request.ControlSignalTowerRequest.SetLight,
            SetSound = request.ControlSignalTowerRequest.SetSound
        };
        if (controlRequest.SetLight == EnumSignalTowerLightState.UnspecifiedSignalTowerLightState)
        {
            controlRequest.SetLight = MapLight(instrument.SignalTower.LightState);
        }

        if (CheckIfSendIsNeeded(instrument, controlRequest))
        {
            await session.Connection.ControlSignalTowerAsync(controlRequest, cancellationToken);

            // Keep the "presumed" state locally to avoid multiple gRPC calls while we are waiting for the instrument to send back the actual state.
            instrument.SignalTower.SetPendingLightState(SignalTower.MapLight(controlRequest.SetLight));

            if (controlRequest.SetSound is EnumSignalTowerControlSoundState.SoundContinuosOn or EnumSignalTowerControlSoundState.SoundShortSignal)
            {
                instrument.SignalTower.SetPendingSoundState(SignalTowerSoundState.ContinuousOn);
            }
            else if (controlRequest.SetSound == EnumSignalTowerControlSoundState.SoundOff)
            {
                instrument.SignalTower.SetPendingSoundState(SignalTowerSoundState.Off);
            }
        }
    }

    private static EnumSignalTowerLightState MapLight(SignalTowerLightState lightState)
    {
        return lightState switch
        {
            SignalTowerLightState.Off => EnumSignalTowerLightState.Off,
            SignalTowerLightState.Green => EnumSignalTowerLightState.Green,
            SignalTowerLightState.Yellow => EnumSignalTowerLightState.Yellow,
            SignalTowerLightState.Red => EnumSignalTowerLightState.Red,
            SignalTowerLightState.FlashingRed => EnumSignalTowerLightState.RedBlink,
            _ => throw new ArgumentOutOfRangeException(nameof(lightState), lightState, null)
        };
    }

    private static bool CheckIfSendIsNeeded(Domain.Instrument instrument, ControlSignalTowerRequest request)
    {
        var mappedLight = SignalTower.MapLight(request.SetLight);
        if (mappedLight != instrument.SignalTower.LightState)
        {
            return true;
        }

        var setSound = request.SetSound;

        if (setSound == EnumSignalTowerControlSoundState.SoundNotSet)
        {
            return false;
        }

        // task card says continuous on but the signal tower is already sounding with continuous on
        if (setSound == EnumSignalTowerControlSoundState.SoundContinuosOn && instrument.SignalTower.SoundState == SignalTowerSoundState.ContinuousOn)
        {
            return false;
        }

        // task card says off, but the signal tower is already off
        if (setSound == EnumSignalTowerControlSoundState.SoundOff && instrument.SignalTower.SoundState == SignalTowerSoundState.Off)
        {
            return false;
        }

        // task card says short signal, but the signal tower is sounding with continuous on
        if (setSound == EnumSignalTowerControlSoundState.SoundShortSignal && instrument.SignalTower.SoundState == SignalTowerSoundState.ContinuousOn)
        {
            return false;
        }

        return true;
    }
}