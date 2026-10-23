namespace Pharos.Client.Common.App.State.CurrentInstrument;

public class SignalTowerViewModel
{
    public void SetState(SignalTower signalTower)
    {
        LightState = signalTower.LightState;
        SoundState = signalTower.SoundState;
    }

    public SignalTowerSoundState SoundState { get; private set; } = SignalTowerSoundState.Off;

    public SignalTowerLightState LightState { get; private set; } = SignalTowerLightState.Off;

    public bool PossibleToMute => SoundState is SignalTowerSoundState.ContinuousOn;
}