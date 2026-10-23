using System.Collections.Immutable;
using System.Reflection;

namespace Pharos.Server.Settings.Infrastructure.ConfigurationProvider;

public class SettingsExporterTests
{

    public SettingsExporterTests()
    {
        ConfigurationManagerExtensions.AssembliesWithSettings = Modules.ModuleList.Select(m => m.Assembly).ToImmutableArray();
    }

    /// <summary>
    /// Creates custom default settings for testing.
    /// First tuple item is the value, second is whether it's a PageList structure (true) or regular setting (false).
    /// </summary>
    private static Dictionary<string, (string?, bool)> CreateDefaultSettings()
    {
        return new Dictionary<string, (string?, bool)>
        {
            // Regular settings (non-PageList)
            { "General:RegionalSettings:Culture", ("en-US", false) },
            { "General:RegionalSettings:TemperatureUnit", ("Celsius", false) },
            { "Identity:PasswordComplexitySettings:MinLength", ("8", false) },
            { "Reflex:ReflexTestingEnabled", ("false", false) },
            { "Request:SecondsUntilMissingTestOrder", ("30", false) },
            { "ComplexSettings:SomeNumber", ("0", false) },
            { "ComplexSettings:PreviewEnabled", ("true", false) },
            
            // PageList structure
            { "Lis:LisChannels:Name", ("", true) },
            { "Lis:LisChannels:ProtocolType", ("Astm", true) },
            { "Lis:LisChannels:IsEnabled", ("true", true) },
            { "Lis:LisChannels:AstmSettings:CommunicationSettings:CommunicationMode", ("Tcp", true) },
            { "Lis:LisChannels:AstmSettings:CommunicationSettings:TcpSettings:TcpPort", ("5000", true) },
            { "Lis:LisChannels:AstmSettings:CommunicationSettings:TcpSettings:SocketType", ("Client", true) },
            
             // PageList inside PageList structure
            { "Lis:LisChannels:AstmSettings:CommunicationSettings:Formats:Name", ("", true) },
            { "Lis:LisChannels:AstmSettings:CommunicationSettings:Formats:Used", ("False", true) },

            // PageList inside PageList initialized entries
            { "Lis:LisChannels:AstmSettings:CommunicationSettings:Formats:0:Name", ("", true) },
            { "Lis:LisChannels:AstmSettings:CommunicationSettings:Formats:0:Used", ("False", true) },
        };
    }

    [Fact]
    public void MergeWithDefaultSettings_ShouldMergeBasicSettings()
    {
        // Arrange
        var defaultSettings = CreateDefaultSettings();
        var dbSettings = new Dictionary<string, string?>
        {
            { "Setting1", "Value1" },
            { "Setting2", "Value2" }
        };

        // Act
        var result = SettingsExporter.MergeSettingsFromDBWithDefaultSettings(defaultSettings, dbSettings);

        // Assert

        // Default settings should be present
        Assert.Contains(result, kvp => kvp.Key == "General:RegionalSettings:Culture" && kvp.Value == "en-US");

        // DB settings should be present
        Assert.Contains(result, kvp => kvp.Key == "Setting1" && kvp.Value == "Value1");
        Assert.Contains(result, kvp => kvp.Key == "Setting2" && kvp.Value == "Value2");

        Assert.Equal(9, result.Count); // 7 defaults + 2 DB settings
    }


    [Fact]
    public void MergeWithDefaultSettings_ShouldMergeDbSettingsOverDefaultSettings()
    {
        // Arrange - Override a known default setting
        var defaultSettings = CreateDefaultSettings();
        var dbSettings = new Dictionary<string, string?>
        {
            { "Reflex:ReflexTestingEnabled", "true" }, // Override default
            { "General:RegionalSettings:Culture", "sv" },
        };

        // Act
        var result = SettingsExporter.MergeSettingsFromDBWithDefaultSettings(defaultSettings, dbSettings);

        // Assert - Should contain DB value, not default
        Assert.Contains(result, kvp => kvp.Key == "Reflex:ReflexTestingEnabled" && kvp.Value == "true");
        Assert.Contains(result, kvp => kvp.Key == "General:RegionalSettings:Culture" && kvp.Value == "sv");
        Assert.DoesNotContain(result, kvp => kvp.Key == "General:RegionalSettings:Culture" && kvp.Value == "en-US");
    }

    [Fact]
    public void MergeWithDefaultSettings_ShouldIncludeDefaultSettings()
    {
        // Arrange
        var defaultSettings = CreateDefaultSettings();
        var dbSettings = new Dictionary<string, string?>();

        // Act
        var result = SettingsExporter.MergeSettingsFromDBWithDefaultSettings(defaultSettings, dbSettings);

        // Assert - Should contain non-PageList default settings
        Assert.NotEmpty(result);
        Assert.Contains(result, kvp => kvp.Key == "General:RegionalSettings:Culture" && kvp.Value == "en-US");
        Assert.Contains(result, kvp => kvp.Key == "General:RegionalSettings:TemperatureUnit" && kvp.Value == "Celsius");
        Assert.Contains(result, kvp => kvp.Key == "Identity:PasswordComplexitySettings:MinLength" && kvp.Value == "8");
    }

    [Fact]
    public void MergeWithDefaultSettings_ShouldExportPageListWithSingleEntry()
    {
        // Arrange
        var defaultSettings = CreateDefaultSettings();
        var dbSettings = new Dictionary<string, string?>
        {
            { "Lis:LisChannels:0:Name", "Channel1" },
            { "Lis:LisChannels:0:ProtocolType", "Astm" }
        };

        // Act
        var result = SettingsExporter.MergeSettingsFromDBWithDefaultSettings(defaultSettings, dbSettings);

        // Assert
        Assert.Contains(result, kvp => kvp.Key == "Lis:LisChannels:0:Name" && kvp.Value == "Channel1");
        Assert.Contains(result, kvp => kvp.Key == "Lis:LisChannels:0:ProtocolType" && kvp.Value == "Astm");

        // Should also include default structure properties for this entry
        Assert.Contains(result, kvp => kvp.Key == "Lis:LisChannels:0:IsEnabled");
    }

    [Fact]
    public void MergeWithDefaultSettings_ShouldExportPageListWithManyEntries()
    {
        // Arrange
        var defaultSettings = CreateDefaultSettings();
        var dbSettings = new Dictionary<string, string?>
        {
            // First entry
            { "Lis:LisChannels:0:Name", "Channel1" },
            { "Lis:LisChannels:0:ProtocolType", "Astm" },
            { "Lis:LisChannels:0:IsEnabled", "true" },
            
            // Second entry
            { "Lis:LisChannels:1:Name", "Channel2" },
            { "Lis:LisChannels:1:ProtocolType", "HL7" },
            { "Lis:LisChannels:1:IsEnabled", "false" },
            
            // Third entry
            { "Lis:LisChannels:2:Name", "Channel3" },
            { "Lis:LisChannels:2:ProtocolType", "DataCap" },
            { "Lis:LisChannels:2:IsEnabled", "true" },
            
            // Fourth entry
            { "Lis:LisChannels:3:Name", "Channel4" },
            { "Lis:LisChannels:3:ProtocolType", "Astm" },
            { "Lis:LisChannels:3:IsEnabled", "true" }
        };

        // Act
        var result = SettingsExporter.MergeSettingsFromDBWithDefaultSettings(defaultSettings, dbSettings);

        // Assert - Verify all entries are exported
        Assert.Contains(result, kvp => kvp.Key == "Lis:LisChannels:0:Name" && kvp.Value == "Channel1");
        Assert.Contains(result, kvp => kvp.Key == "Lis:LisChannels:1:Name" && kvp.Value == "Channel2");
        Assert.Contains(result, kvp => kvp.Key == "Lis:LisChannels:2:Name" && kvp.Value == "Channel3");
        Assert.Contains(result, kvp => kvp.Key == "Lis:LisChannels:3:Name" && kvp.Value == "Channel4");

        // Verify all properties for each entry
        Assert.Contains(result, kvp => kvp.Key == "Lis:LisChannels:0:ProtocolType" && kvp.Value == "Astm");
        Assert.Contains(result, kvp => kvp.Key == "Lis:LisChannels:1:ProtocolType" && kvp.Value == "HL7");
        Assert.Contains(result, kvp => kvp.Key == "Lis:LisChannels:2:ProtocolType" && kvp.Value == "DataCap");
        Assert.Contains(result, kvp => kvp.Key == "Lis:LisChannels:3:IsEnabled" && kvp.Value == "true");

        // Verify structure is applied to each entry from defaults
        for (var i = 0; i < 4; i++)
        {
            Assert.Contains(result, kvp => kvp.Key == $"Lis:LisChannels:{i}:Name");
            Assert.Contains(result, kvp => kvp.Key == $"Lis:LisChannels:{i}:ProtocolType");
            Assert.Contains(result, kvp => kvp.Key == $"Lis:LisChannels:{i}:IsEnabled");
        }
    }

    [Fact]
    public void MergeWithDefaultSettings_ShouldNotExportPageListSructure()
    {
        // Arrange
        var defaultSettings = CreateDefaultSettings();
        var dbSettings = new Dictionary<string, string?>
        {
        };

        // Act
        var result = SettingsExporter.MergeSettingsFromDBWithDefaultSettings(defaultSettings, dbSettings);

        // Assert
        Assert.DoesNotContain(result, kvp => kvp.Key.StartsWith("Lis:LisChannels:"));
    }

    // When there is pagelist entry in DB, the structure should not be exported
    [Fact]
    public void MergeWithDefaultSettings_ShouldNotExportPageListSructureWithDBEntry()
    {
        // Arrange
        var defaultSettings = CreateDefaultSettings();
        var dbSettings = new Dictionary<string, string?>
        {
            { "Lis:LisChannels:0:Name", "Channel1" },
        };

        // Act
        var result = SettingsExporter.MergeSettingsFromDBWithDefaultSettings(defaultSettings, dbSettings);

        // Assert
        Assert.DoesNotContain(result, kvp => kvp.Key == "Lis:LisChannels:Name");
        Assert.DoesNotContain(result, kvp => kvp.Key == "Lis:LisChannels:ProtocolType");
        Assert.DoesNotContain(result, kvp => kvp.Key == "Lis:LisChannels:IsEnabled");
    }

    // When defaults and DB are empty, result should be empty
    [Fact]
    public void EmptyDefaultandEmptySettings_ResultShouldBeEmpty()
    {
        // Arrange
        var defaultSettings = new Dictionary<string, (string?, bool)>();
        var dbSettings = new Dictionary<string, string?>();

        // Act
        var result = SettingsExporter.MergeSettingsFromDBWithDefaultSettings(defaultSettings, dbSettings);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void EmptyDefault_ResultShouldContainDBSettings()
    {
        // Arrange
        var defaultSettings = new Dictionary<string, (string?, bool)>();
        var dbSettings = new Dictionary<string, string?>
        {
            { "Lis:LisChannels:0:Name", "Channel1" },
        };

        // Act
        var result = SettingsExporter.MergeSettingsFromDBWithDefaultSettings(defaultSettings, dbSettings);

        // Assert
        Assert.Contains(result, kvp => kvp.Key == "Lis:LisChannels:0:Name");
    }

    [Fact]
    public void MergeWithDefaultSettings_ShouldNotExportPageListInsidePageListSructure()
    {
        // Arrange
        var defaultSettings = CreateDefaultSettings();
        var dbSettings = new Dictionary<string, string?>
        {
            { "Lis:LisChannels:0:Name", "Channel1" },
        };

        // Act
        var result = SettingsExporter.MergeSettingsFromDBWithDefaultSettings(defaultSettings, dbSettings);

        // Assert
        Assert.DoesNotContain(result, kvp => kvp.Key == "Lis:LisChannels:AstmSettings:CommunicationSettings:Formats:Name");
        Assert.DoesNotContain(result, kvp => kvp.Key == "Lis:LisChannels:AstmSettings:CommunicationSettings:Formats:Used");
    }

    // When there is pagelist entry with pagelist inside pagelist, the structure should not be exported
    [Fact]
    public void MergeWithDefaultSettings_ShouldNotExportPageListInsidePageListSructureWithDBEntry()
    {
        // Arrange
        var defaultSettings = CreateDefaultSettings();
        var dbSettings = new Dictionary<string, string?>
        {
            { "Lis:LisChannels:0:Name", "Channel1" },
        };

        // Act
        var result = SettingsExporter.MergeSettingsFromDBWithDefaultSettings(defaultSettings, dbSettings);

        // Assert
        Assert.DoesNotContain(result, kvp => kvp.Key == "Lis:LisChannels:Name");
        Assert.DoesNotContain(result, kvp => kvp.Key == "Lis:LisChannels:AstmSettings:CommunicationSettings:Formats:Name");
        Assert.DoesNotContain(result, kvp => kvp.Key == "Lis:LisChannels:AstmSettings:CommunicationSettings:Formats:Used");
    }

    // When there is pagelist entry with pagelist inside pagelist entry in DB, the structure should not be exported
    [Fact]
    public void MergeWithDefaultSettings_ShouldNotExportPageListInsidePageListSructureWithDBEntryOfPageListInsidePagelist()
    {
        // Arrange
        var defaultSettings = CreateDefaultSettings();
        var dbSettings = new Dictionary<string, string?>
        {
            { "Lis:LisChannels:0:Name", "Channel1" },
            { "Lis:LisChannels:0:AstmSettings:CommunicationSettings:Formats:0:Name", "DB" }
        };

        // Act
        var result = SettingsExporter.MergeSettingsFromDBWithDefaultSettings(defaultSettings, dbSettings);

        // Assert
        Assert.Contains(result, kvp => kvp.Key == "Lis:LisChannels:0:Name" && kvp.Value == "Channel1");
        Assert.Contains(result, kvp => kvp.Key == "Lis:LisChannels:0:AstmSettings:CommunicationSettings:Formats:0:Name" && kvp.Value == "DB");
        Assert.DoesNotContain(result, kvp => kvp.Key == "Lis:LisChannels:Name");
        Assert.DoesNotContain(result, kvp => kvp.Key == "Lis:LisChannels:AstmSettings:CommunicationSettings:Formats:Name");
        Assert.DoesNotContain(result, kvp => kvp.Key == "Lis:LisChannels:AstmSettings:CommunicationSettings:Formats:Used");
    }


    [Fact]
    public void MergeWithDefaultSettings_ShouldExportNestedPageListWithMultipleLevels()
    {
        // Arrange - Three levels deep: Lis -> LisChannels -> nested settings
        var defaultSettings = CreateDefaultSettings();
        var dbSettings = new Dictionary<string, string?>
        {
            { "Lis:LisChannels:0:Name", "Channel0" },
            { "Lis:LisChannels:0:AstmSettings:CommunicationSettings:CommunicationMode", "Tcp" },
            { "Lis:LisChannels:0:AstmSettings:CommunicationSettings:TcpSettings:TcpPort", "5000" },

            { "Lis:LisChannels:1:Name", "Channel1" },
            { "Lis:LisChannels:1:AstmSettings:CommunicationSettings:CommunicationMode", "Rs232" }
        };

        // Act
        var result = SettingsExporter.MergeSettingsFromDBWithDefaultSettings(defaultSettings, dbSettings);

        // Assert - Verify all three levels are exported correctly
        Assert.Contains(result, kvp => kvp.Key == "Lis:LisChannels:0:Name" && kvp.Value == "Channel0");
        Assert.Contains(result, kvp => kvp.Key == "Lis:LisChannels:0:AstmSettings:CommunicationSettings:CommunicationMode" && kvp.Value == "Tcp");
        Assert.Contains(result, kvp => kvp.Key == "Lis:LisChannels:0:AstmSettings:CommunicationSettings:TcpSettings:TcpPort" && kvp.Value == "5000");
        Assert.Contains(result, kvp => kvp.Key == "Lis:LisChannels:1:Name" && kvp.Value == "Channel1");

        // Verify default structure values are applied
        Assert.Contains(result, kvp => kvp.Key == "Lis:LisChannels:0:AstmSettings:CommunicationSettings:TcpSettings:SocketType");
        Assert.Contains(result, kvp => kvp.Key == "Lis:LisChannels:1:AstmSettings:CommunicationSettings:TcpSettings:SocketType");
    }

    [Fact]
    public void MergeWithDefaultSettings_ShouldHandleMixedPageListAndRegularSettings()
    {
        // Arrange
        var defaultSettings = CreateDefaultSettings();
        var dbSettings = new Dictionary<string, string?>
        {
            // Regular settings
            { "General:RegionalSettings:Culture", "de-DE" },
            
            // PageList settings
            { "Lis:LisChannels:0:Name", "ASTM Channel" },
            { "Lis:LisChannels:0:ProtocolType", "Astm" },
            
            // More regular settings
            { "Identity:PasswordComplexitySettings:MinLength", "12" },
            
            // Another PageList entry
            { "Lis:LisChannels:1:Name", "HL7 Channel" },
            { "Lis:LisChannels:1:ProtocolType", "HL7" }
        };

        // Act
        var result = SettingsExporter.MergeSettingsFromDBWithDefaultSettings(defaultSettings, dbSettings);

        // Assert - Verify regular settings
        Assert.Contains(result, kvp => kvp.Key == "General:RegionalSettings:Culture" && kvp.Value == "de-DE");
        Assert.Contains(result, kvp => kvp.Key == "Identity:PasswordComplexitySettings:MinLength" && kvp.Value == "12");

        // Verify PageList settings
        Assert.Contains(result, kvp => kvp.Key == "Lis:LisChannels:0:Name" && kvp.Value == "ASTM Channel");
        Assert.Contains(result, kvp => kvp.Key == "Lis:LisChannels:1:Name" && kvp.Value == "HL7 Channel");

        // Verify other default settings are included
        Assert.Contains(result, kvp => kvp.Key == "General:RegionalSettings:TemperatureUnit");
        Assert.True(result.Count > dbSettings.Count);
    }

    [Fact]
    public void MergeWithDefaultSettings_ShouldExportPageListWithGaps()
    {
        // Arrange - PageList with non-sequential indices (gaps)
        var defaultSettings = CreateDefaultSettings();
        var dbSettings = new Dictionary<string, string?>
        {
            { "Request:TubeTypes:TubeTypes:0:Name", "Tube0" },
            { "Request:TubeTypes:TubeTypes:0:InnerDiameter", "10" },
            
            // Gap - no index 1
            
            { "Request:TubeTypes:TubeTypes:2:Name", "Tube2" },
            { "Request:TubeTypes:TubeTypes:2:InnerDiameter", "12" },

            { "Request:TubeTypes:TubeTypes:5:Name", "Tube5" },
            { "Request:TubeTypes:TubeTypes:5:InnerDiameter", "15" }
        };

        // Act
        var result = SettingsExporter.MergeSettingsFromDBWithDefaultSettings(defaultSettings, dbSettings);

        // Assert - Verify entries with gaps are preserved
        Assert.Contains(result, kvp => kvp.Key == "Request:TubeTypes:TubeTypes:0:Name" && kvp.Value == "Tube0");
        Assert.Contains(result, kvp => kvp.Key == "Request:TubeTypes:TubeTypes:2:Name" && kvp.Value == "Tube2");
        Assert.Contains(result, kvp => kvp.Key == "Request:TubeTypes:TubeTypes:5:Name" && kvp.Value == "Tube5");

        // Verify no index 1 exists
        Assert.DoesNotContain(result, kvp => kvp.Key.Contains("TubeTypes:1:"));

        // Verify index 3 and 4 don't exist
        Assert.DoesNotContain(result, kvp => kvp.Key.Contains("TubeTypes:3:"));
        Assert.DoesNotContain(result, kvp => kvp.Key.Contains("TubeTypes:4:"));
    }

    [Fact]
    public void MergeWithDefaultSettings_ShouldNotExportInitializedPageListStructure()
    {
        // Arrange - ComplexSettings.MethodReportComments has an initialized entry that shouldn't be in structure
        var defaultSettings = CreateDefaultSettings();
        var dbSettings = new Dictionary<string, string?>
        {
            // Only regular settings, no PageList entries
            { "ComplexSettings:SomeNumber", "50" }
        };

        // Act
        var result = SettingsExporter.MergeSettingsFromDBWithDefaultSettings(defaultSettings, dbSettings);

        // Assert - Should not contain initialized PageList structure entries
        // The initialized entry "0" should not be exported unless it's in DB
        Assert.DoesNotContain(result, kvp => kvp.Key.StartsWith("ComplexSettings:MethodReportComments:"));

        // But should contain the regular setting
        Assert.Contains(result, kvp => kvp.Key == "ComplexSettings:SomeNumber" && kvp.Value == "50");
    }

    [Fact]
    public void MergeWithDefaultSettings_ShouldHandleEmptyDbSettings()
    {
        // Arrange
        var defaultSettings = CreateDefaultSettings();
        var dbSettings = new Dictionary<string, string?>();

        // Act
        var result = SettingsExporter.MergeSettingsFromDBWithDefaultSettings(defaultSettings, dbSettings);

        // Assert - Should only contain non-PageList default settings
        Assert.NotEmpty(result);

        // Should not contain any indexed entries
        Assert.All(result.Keys, key => Assert.DoesNotContain(":0:", key));
        Assert.All(result.Keys, key => Assert.DoesNotContain(":1:", key));

        // Should contain default settings
        var expectedDefaultCount = defaultSettings.Count(kvp => !kvp.Value.Item2);
        Assert.Equal(expectedDefaultCount, result.Count);
    }

    [Fact]
    public void MergeWithDefaultSettings_ShouldHandleNullValues()
    {
        // Arrange
        var defaultSettings = CreateDefaultSettings();
        var dbSettings = new Dictionary<string, string?>
        {
            { "Setting1", null },
            { "Setting2", "Value2" }
        };

        // Act
        var result = SettingsExporter.MergeSettingsFromDBWithDefaultSettings(defaultSettings, dbSettings);

        // Assert - Null values should be converted to empty strings
        Assert.Contains(result, kvp => kvp.Key == "Setting1" && string.IsNullOrEmpty(kvp.Value));
        Assert.Contains(result, kvp => kvp.Key == "Setting2" && kvp.Value == "Value2");
    }

    [Fact]
    public void MergeWithDefaultSettings_ShouldIncludeDefaultSettingsNotInDb()
    {
        // Arrange
        var defaultSettings = CreateDefaultSettings();
        var dbSettings = new Dictionary<string, string?>
        {
            { "Lis:LisChannels:0:Name", "Channel1" }
        };

        // Act
        var result = SettingsExporter.MergeSettingsFromDBWithDefaultSettings(defaultSettings, dbSettings);

        // Assert - Should contain default settings that aren't PageList structures
        Assert.Contains(result, kvp => !kvp.Key.Contains(":0:") && !kvp.Key.Contains(":1:"));

        // Should contain more entries than just the DB settings
        Assert.True(result.Count > 1);

        // Check specific default settings exist
        Assert.Contains(result, kvp => kvp.Key == "General:RegionalSettings:Culture");
        Assert.Contains(result, kvp => kvp.Key == "Identity:PasswordComplexitySettings:MinLength");
    }

    [Fact]
    public void MergeWithDefaultSettings_ShouldPreserveComplexNestedStructures()
    {
        // Arrange - Lis channel with nested communication settings
        var defaultSettings = CreateDefaultSettings();
        var dbSettings = new Dictionary<string, string?>
        {
            { "Lis:LisChannels:0:Name", "TestChannel" },
            { "Lis:LisChannels:0:AstmSettings:CommunicationSettings:CommunicationMode", "Tcp" },
            { "Lis:LisChannels:0:AstmSettings:CommunicationSettings:TcpSettings:TcpPort", "5000" },
            { "Lis:LisChannels:0:AstmSettings:CommunicationSettings:TcpSettings:SocketType", "Client" }
        };

        // Act
        var result = SettingsExporter.MergeSettingsFromDBWithDefaultSettings(defaultSettings, dbSettings);

        // Assert - Verify all nested properties are preserved
        Assert.Contains(result, kvp => kvp.Key == "Lis:LisChannels:0:Name" && kvp.Value == "TestChannel");
        Assert.Contains(result, kvp => kvp.Key == "Lis:LisChannels:0:AstmSettings:CommunicationSettings:CommunicationMode" && kvp.Value == "Tcp");
        Assert.Contains(result, kvp => kvp.Key == "Lis:LisChannels:0:AstmSettings:CommunicationSettings:TcpSettings:TcpPort" && kvp.Value == "5000");
        Assert.Contains(result, kvp => kvp.Key == "Lis:LisChannels:0:AstmSettings:CommunicationSettings:TcpSettings:SocketType" && kvp.Value == "Client");
    }

    [Fact]
    public void MergeWithDefaultSettings_WithOnlyDefaultSettings_ShouldReturnNonPageListDefaults()
    {
        // Arrange - No DB settings
        var defaultSettings = CreateDefaultSettings();
        var dbSettings = new Dictionary<string, string?>();

        // Act
        var result = SettingsExporter.MergeSettingsFromDBWithDefaultSettings(defaultSettings, dbSettings);

        // Assert
        var expectedDefaults = defaultSettings
            .Where(kvp => !kvp.Value.Item2 && kvp.Value.Item1 != null)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Item1!);

        Assert.Equal(expectedDefaults.Count, result.Count);

        foreach (var expected in expectedDefaults)
        {
            Assert.Contains(result, kvp => kvp.Key == expected.Key && kvp.Value == expected.Value);
        }
    }

    [Fact]
    public void MergeWithDefaultSettings_WithPartialDbOverrides_ShouldMergeCorrectly()
    {
        // Arrange - Override some defaults, add some new values
        var defaultSettings = CreateDefaultSettings();
        var dbSettings = new Dictionary<string, string?>
        {
            // Override existing default
            { "General:RegionalSettings:Culture", "fr-FR" },
            
            // Add new PageList entry
            { "Lis:LisChannels:0:Name", "MyChannel" },
            { "Lis:LisChannels:0:ProtocolType", "Astm" }
        };

        // Act
        var result = SettingsExporter.MergeSettingsFromDBWithDefaultSettings(defaultSettings, dbSettings);

        // Assert
        // DB values should override defaults
        Assert.Contains(result, kvp => kvp.Key == "General:RegionalSettings:Culture" && kvp.Value == "fr-FR");

        // New PageList entry should exist
        Assert.Contains(result, kvp => kvp.Key == "Lis:LisChannels:0:Name" && kvp.Value == "MyChannel");

        // Other defaults should still be present
        Assert.Contains(result, kvp => kvp.Key == "General:RegionalSettings:TemperatureUnit");
        Assert.Contains(result, kvp => kvp.Key == "Identity:PasswordComplexitySettings:MinLength");
    }

    [Fact]
    public void HasSegmentPrefix_ShouldReturnTrue_WhenKeysMatch()
    {
        // Use reflection to access private method
        var method = typeof(SettingsExporter).GetMethod("HasSegmentPrefix", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        // Act & Assert
        var result1 = (bool)method!.Invoke(null, ["Lis:LisChannels:Name", "Lis:LisChannels"])!;
        Assert.True(result1);

        var result2 = (bool)method.Invoke(null, ["Lis:LisChannels", "Lis:LisChannels"])!;
        Assert.True(result2);

        var result3 = (bool)method.Invoke(null, ["Lis:LisChannels:Name", "Lis:Other"])!;
        Assert.False(result3);
    }

    [Fact]
    public void HasIndexInKey_ShouldReturnTrue_WhenKeyContainsNumericSegment()
    {
        // Use reflection to access private method
        var method = typeof(SettingsExporter).GetMethod("HasIndexInKey", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        // Act & Assert
        var result1 = (bool)method!.Invoke(null, ["Lis:LisChannels:0:Name"])!;
        Assert.True(result1);

        var result2 = (bool)method.Invoke(null, ["Lis:LisChannels:Name"])!;
        Assert.False(result2);

        var result3 = (bool)method.Invoke(null, [""])!;
        Assert.False(result3);

        var result4 = (bool)method.Invoke(null, ["Lis:LisChannels:123:Property"])!;
        Assert.True(result4);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_6000")]
    public void MergeWithDefaultSettings_ProtectedDataFields_ShouldBeBlankInExport()
    {
        // Arrange
        var defaultSettings = new Dictionary<string, (string?, bool)>
        {
            { "General:Culture", ("en-US", false) },
            { "Security:Token", ("default-token", false) },
            { "Security:Password", ("default-password", false) },
        };

        var dbSettings = new Dictionary<string, string?>
        {
            { "Security:Token", "my-secret-token" },
            { "Security:Password", "my-secret-password" },
            { "General:Culture", "sv" },
        };

        // Simulate the export logic from GetSettingsExportQueryHandler:
        // ProtectedData / IsEncrypted options should be exported as empty string
        var encryptedKeys = new HashSet<string>(StringComparer.Ordinal)
        {
            "Security:Token",
            "Security:Password"
        };

        // Act
        var mergedSettings = SettingsExporter.MergeSettingsFromDBWithDefaultSettings(defaultSettings, dbSettings);

        var exportedLines = mergedSettings
            .Select(kvp => new KeyValuePair<string, string>(
                kvp.Key,
                encryptedKeys.Contains(kvp.Key) ? string.Empty : kvp.Value))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        // Assert - encrypted fields should be empty in the export
        Assert.Equal(string.Empty, exportedLines["Security:Token"]);
        Assert.Equal(string.Empty, exportedLines["Security:Password"]);

        // Non-encrypted fields should retain their values
        Assert.Equal("sv", exportedLines["General:Culture"]);
    }
}
