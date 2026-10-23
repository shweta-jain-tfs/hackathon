using Pharos.Server.Settings.Application.Metadata;
using Pharos.Server.Settings.Domain;

namespace Pharos.Server.Settings.Infrastructure.Database;

public class SettingsDbContextInitializerTests
{
    [Fact]
    public void FilterUnrecognizedSettings_KeepsSettingMappingToKnownOption()
    {
        // Arrange
        var options = new Dictionary<string, Option>
        {
            ["Samples:TubeTypes:Name"] = new Option { Key = "Samples:TubeTypes:Name" },
        };
        var settings = new List<Setting>
        {
            new() { Key = "Samples:TubeTypes:Name" },
        };

        // Act
        var result = SettingsDbContextInitializer.FilterUnrecognizedSettings(settings, options);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void FilterUnrecognizedSettings_KeepsIndexedListEntryMatchingOptionTemplate()
    {
        // Arrange - the stored key contains a numeric (index) segment, the option is the template
        var options = new Dictionary<string, Option>
        {
            ["Samples:TubeTypes:Name"] = new Option { Key = "Samples:TubeTypes:Name" },
        };
        var settings = new List<Setting>
        {
            new() { Key = "Samples:TubeTypes:0:Name" },
        };

        // Act
        var result = SettingsDbContextInitializer.FilterUnrecognizedSettings(settings, options);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void FilterUnrecognizedSettings_ReturnsSettingWithoutMatchingOption()
    {
        // Arrange - an obsolete key left behind after the section was moved
        var options = new Dictionary<string, Option>
        {
            ["Samples:TubeTypes:Name"] = new Option { Key = "Samples:TubeTypes:Name" },
        };
        var obsolete = new Setting { Key = "Request:TubeTypesSettings:TubeTypes:0:Name" };
        var settings = new List<Setting> { obsolete };

        // Act
        var result = SettingsDbContextInitializer.FilterUnrecognizedSettings(settings, options);

        // Assert
        result.Should().ContainSingle().Which.Should().BeSameAs(obsolete);
    }

    [Fact]
    public void FilterUnrecognizedSettings_ReturnsOnlyObsoleteSettings_WhenMixed()
    {
        // Arrange
        var options = new Dictionary<string, Option>
        {
            ["Samples:TubeTypes:Name"] = new Option { Key = "Samples:TubeTypes:Name" },
            ["General:Language"] = new Option { Key = "General:Language" },
        };
        var validIndexed = new Setting { Key = "Samples:TubeTypes:0:Name" };
        var validExact = new Setting { Key = "General:Language" };
        var obsolete = new Setting { Key = "Request:TubeTypesSettings:TubeTypes:0:Name" };
        var settings = new List<Setting> { validIndexed, validExact, obsolete };

        // Act
        var result = SettingsDbContextInitializer.FilterUnrecognizedSettings(settings, options);

        // Assert
        result.Should().ContainSingle().Which.Key.Should().Be("Request:TubeTypesSettings:TubeTypes:0:Name");
    }

    [Fact]
    public void FilterUnrecognizedSettings_ReturnsEmpty_WhenNoSettings()
    {
        // Arrange
        var options = new Dictionary<string, Option>
        {
            ["Samples:TubeTypes:Name"] = new Option { Key = "Samples:TubeTypes:Name" },
        };

        // Act
        var result = SettingsDbContextInitializer.FilterUnrecognizedSettings([], options);

        // Assert
        result.Should().BeEmpty();
    }
}
