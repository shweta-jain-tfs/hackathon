using FluentAssertions;
using Pharos.Client.Common.App.Helpers;
using Pharos.Client.Common.Models;
using Pharos.Common.Web.Components.Health;

namespace Pharos.Client.Tests.Helpers;

public class HealthStatusMapperTests
{
    [Theory]
    [InlineData(SignalTowerLightState.Green, HealthStatus.Ok)]
    [InlineData(SignalTowerLightState.Yellow, HealthStatus.Warning)]
    [InlineData(SignalTowerLightState.Red, HealthStatus.Alarm)]
    [InlineData(SignalTowerLightState.FlashingRed, HealthStatus.Alarm)]
    public void Map_KnownLightStates_ReturnsExpectedHealthStatus(
        SignalTowerLightState lightState,
        HealthStatus expectedStatus)
    {
        var result = HealthStatusMapper.Map(lightState, InstrumentState.Assay);

        result.Should().Be(expectedStatus);
    }

    [Fact]
    public void Map_OffLightState_WithPowerSavingInstrumentState_ReturnsOff()
    {
        var result = HealthStatusMapper.Map(SignalTowerLightState.Off, InstrumentState.PowerSaving);

        result.Should().Be(HealthStatus.Off);
    }

    [Fact]
    public void Map_OffLightState_WithOfflineInstrumentState_ReturnsAlarm()
    {
        var result = HealthStatusMapper.Map(SignalTowerLightState.Off, InstrumentState.Offline);

        result.Should().Be(HealthStatus.Alarm);
    }

    [Fact]
    public void Map_OffLightState_WithStandbyInstrumentState_ReturnsStandby()
    {
        var result = HealthStatusMapper.Map(SignalTowerLightState.Off, InstrumentState.Standby);

        result.Should().Be(HealthStatus.Standby);
    }
}