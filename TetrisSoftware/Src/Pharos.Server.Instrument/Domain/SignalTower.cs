using Tetris.Protocol.Common;
using Tetris.Protocol.Pharos;

namespace Pharos.Server.Instrument.Domain;

internal class SignalTower
{
    public SignalTower()
    {
        Reset();
    }

    private bool _lightStatePending;
    private bool _soundStatePending;

    public SignalTowerLightState LightState { get; private set; }
    public SignalTowerSoundState SoundState { get; private set; }

    public void SetPendingLightState(SignalTowerLightState state)
    {
        if (LightState != state)
        {
            LightState = state;
            _lightStatePending = true;
        }
    }

    public void SetPendingSoundState(SignalTowerSoundState state)
    {
        if (SoundState != state)
        {
            SoundState = state;
            _soundStatePending = true;
        }
    }

    public bool SetState(SignalTowerState request)
    {
        return SetState(MapLight(request.LightState), MapSound(request.SoundState));
    }

    public bool SetState(SignalTowerLightState lightState, SignalTowerSoundState soundState)
    {
        var stateChanged = _lightStatePending || _soundStatePending;

        if (LightState != lightState)
        {
            LightState = lightState;
            stateChanged = true;
        }

        if (SoundState != soundState)
        {
            SoundState = soundState;
            stateChanged = true;
        }

        _lightStatePending = false;
        _soundStatePending = false;

        return stateChanged;
    }

    public static SignalTowerLightState MapLight(EnumSignalTowerLightState lightState)
    {
        return lightState switch
        {
            EnumSignalTowerLightState.Off => SignalTowerLightState.Off,
            EnumSignalTowerLightState.Green => SignalTowerLightState.Green,
            EnumSignalTowerLightState.Yellow => SignalTowerLightState.Yellow,
            EnumSignalTowerLightState.Red => SignalTowerLightState.Red,
            EnumSignalTowerLightState.RedBlink => SignalTowerLightState.FlashingRed,
            _ => throw new ArgumentOutOfRangeException(nameof(lightState), lightState, null)
        };
    }

    public static SignalTowerSoundState MapSound(EnumSignalTowerSoundState soundState)
    {
        return soundState switch
        {
            EnumSignalTowerSoundState.SoundContinuosOn => SignalTowerSoundState.ContinuousOn,
            EnumSignalTowerSoundState.SoundOff => SignalTowerSoundState.Off,
            _ => throw new ArgumentOutOfRangeException(nameof(soundState), soundState, null)
        };
    }

    public void Reset()
    {
        LightState = SignalTowerLightState.Off;
        SoundState = SignalTowerSoundState.Off;
        _lightStatePending = false;
        _soundStatePending = false;
    }
}