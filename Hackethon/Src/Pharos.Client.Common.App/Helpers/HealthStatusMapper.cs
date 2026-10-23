using Pharos.Common.Web.Components.Health;

namespace Pharos.Client.Common.App.Helpers;

/// <summary>
/// Maps instrument/tower states to presentational <see cref="HealthStatus"/>.
/// </summary>
public static class HealthStatusMapper
{
    /// <summary>
    /// Map tower light + instrument state to presentation HealthStatus.
    /// </summary>
    public static HealthStatus Map(SignalTowerLightState lightState, InstrumentState instrumentState)
    {
        if (instrumentState == InstrumentState.Offline)
        {
            return HealthStatus.Alarm;
        }

        return lightState switch
        {
            SignalTowerLightState.Green => HealthStatus.Ok,
            SignalTowerLightState.Yellow => HealthStatus.Warning,
            SignalTowerLightState.Red or SignalTowerLightState.FlashingRed => HealthStatus.Alarm,
            SignalTowerLightState.Off => instrumentState is InstrumentState.PowerSaving
                ? HealthStatus.Off
                : HealthStatus.Standby,
            _ => throw new ArgumentOutOfRangeException(nameof(lightState), lightState, null),
        };
    }
}