namespace Pharos.Client.App.Settings.Models;

public class SettingsNavigationServiceTests
{
    [Fact]
    public void GetPathFromKey_ShouldPreferPagePath_WhenGroupPathMapsToSamePageKey()
    {
        var sut = new SettingsNavigationService();
        sut.SetPageRouteMap(new Dictionary<string, string>
        {
            ["Instrument/Instruments/Instrument A/Waste"] = "Instrument:Instruments:0",
            ["Instrument/Instruments/Instrument A"] = "Instrument:Instruments:0"
        });

        var path = sut.GetPathFromKey("Instrument:Instruments:0");

        path.Should().Be("Instrument/Instruments/Instrument A");
    }

    [Fact]
    public void GetPathFromKey_ShouldKeepPreferredPagePath_WhenItStillMapsToSameKey()
    {
        var sut = new SettingsNavigationService();
        sut.SetPageRouteMap(new Dictionary<string, string>
        {
            ["InstrumentSettings/Instrument B/Waste"] = "Instrument:Instruments:1",
            ["InstrumentSettings/Instrument A"] = "Instrument:Instruments:0",
            ["InstrumentSettings/Instrument A/Waste"] = "Instrument:Instruments:0"
        });

        var path = sut.GetPathFromKey("Instrument:Instruments:0", "InstrumentSettings/Instrument A");

        path.Should().Be("InstrumentSettings/Instrument A");
    }

    [Fact]
    public void GetPathFromKey_ShouldNotKeepPreferredGroupPath_WhenPagePathMapsToSameKey()
    {
        var sut = new SettingsNavigationService();
        sut.SetPageRouteMap(new Dictionary<string, string>
        {
            ["InstrumentSettings/Instrument A/Waste"] = "Instrument:Instruments:0",
            ["InstrumentSettings/Instrument A"] = "Instrument:Instruments:0"
        });

        var path = sut.GetPathFromKey("Instrument:Instruments:0", "InstrumentSettings/Instrument A/Waste");

        path.Should().Be("InstrumentSettings/Instrument A");
    }
}