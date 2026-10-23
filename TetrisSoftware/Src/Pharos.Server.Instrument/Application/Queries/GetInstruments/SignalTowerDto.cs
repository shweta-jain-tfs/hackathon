using Pharos.Server.Instrument.Domain;

namespace Pharos.Server.Instrument.Application.Queries.GetInstruments;

#pragma warning disable CS8618
public class SignalTowerDto
{
    public SignalTowerLightState LightState { get; set; } = SignalTowerLightState.Off;
    public SignalTowerSoundState SoundState { get; set; } = SignalTowerSoundState.Off;
}