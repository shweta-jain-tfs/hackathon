using Pharos.Server.Application.Settings.Instrument;

namespace Pharos.Server.Settings.Application.Settings;

public class InstrumentSettingsTests
{
    [Fact]
    public void GetInstrumentSettings_ShouldReturnExistingSettings_WhenSerialNumberMatches()
    {
        var expected = new InstrumentSpecificSettings { SerialNumber = "SEED_002" };
        var settings = new InstrumentSettings
        {
            Instruments = [expected]
        };

        var result = settings.GetInstrumentSettings("seed_002");

        result.Should().BeSameAs(expected);
    }

    [Fact]
    public void GetInstrumentSettings_ShouldReturnDefaultSettings_WhenSerialNumberIsNotConfigured()
    {
        var settings = new InstrumentSettings();

        var result = settings.GetInstrumentSettings("SEED_002");

        result.SerialNumber.Should().Be("SEED_002");
        result.Waste.MaximumLiquidWasteFillingVolume.Should().Be(10);
        result.Waste.PipedOutWasteInstalled.Should().BeFalse();
        result.Rinse.PipedInRinseInstalled.Should().BeFalse();
        result.DrawerLock.AutoLockCompartmentTimeout.Should().Be(15);
        result.Reagents.KeepExpiredReagentsOnBoard.Should().BeFalse();
        result.Reagents.KeepEmptyReagentsOnBoard.Should().BeFalse();
    }
}
