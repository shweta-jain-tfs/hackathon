using System.Globalization;
using System.Text;
using Pharos.Server.Application.Settings.Attributes;
using Pharos.Server.Application.Settings.General;
using Pharos.Server.Settings.Application.Metadata;
using Pharos.Server.Settings.Application.Services;
using Option = Pharos.Server.Settings.Application.Metadata.Option;

namespace Pharos.Server.Settings.Infrastructure.ConfigurationProvider;

public class SettingsImporterTests
{
    public SettingsImporterTests()
    {
        _ = Modules.ModuleList.Select(m => m.Assembly);
        BaseDefaultSettingsProvider.Initialize([]);
    }

    private static Dictionary<string, Option> CreateDefaultSettings()
    {
        return new Dictionary<string, Option>
        {
            ["Lis:LisChannels:Name"] = CreateDefaultOption(OptionType.String),
            ["Lis:LisChannels:AstmSettings:CommunicationSettings:CommunicationMode"] = CreateDefaultOption(OptionType.Choice, defaultValue: "Tcp", choices: new List<Choice> { new() { Value = "Tcp" }, new() { Value = "Rs232" } }),
            ["Lis:LisChannels:AstmSettings:CommunicationSettings:TcpSettings:TcpReconnectionInterval"] = CreateDefaultOption(OptionType.Integer, "3000"),
            ["Lis:LisChannels:AstmSettings:CommunicationSettings:TcpSettings:SocketType"] = CreateDefaultOption(OptionType.Choice, defaultValue: "Server", choices: new List<Choice> { new() { Value = "Client" }, new() { Value = "Server" } }),
            ["Lis:LisChannels:AstmSettings:CommunicationSettings:TcpSettings:IpAddress"] = CreateDefaultOption(OptionType.String),
            ["Lis:LisChannels:AstmSettings:CommunicationSettings:TcpSettings:TcpPort"] = CreateDefaultOption(OptionType.Integer),
            ["Request:AutomaticDeleteSettings:JobRunTime"] = CreateDefaultOption(OptionType.Time, "00:00"),
            ["Request:DaysBeforeReuseOfSampleId"] = CreateDefaultOption(OptionType.Integer, "365"),
            ["Request:TubeTypes:TubeTypes:InnerBottomLevel"] = CreateDefaultOption(OptionType.Integer, "10"),
            ["Request:TubeTypes:TubeTypes:InnerDiameter"] = CreateDefaultOption(OptionType.Double, "10.0"),
            ["Request:TubeTypes:TubeTypes:Name"] = CreateDefaultOption(OptionType.String),
            ["Request:TubeTypes:TubeTypes:TubeDefinitionId"] = CreateDefaultOption(OptionType.Integer, "0"),
            ["Request:TubeTypes:TubeTypes:BottomShape"] = CreateDefaultOption(OptionType.Choice, defaultValue: "Flat", choices: new List<Choice> { new() { Value = "Flat" }, new() { Value = "Round" } }),
            ["Request:SecondsUntilMissingTestOrder"] = CreateDefaultOption(OptionType.Integer, "30"),
            ["Identity:PasswordComplexitySettings:RequireDigit"] = CreateDefaultOption(OptionType.Boolean, "True"),

        };
    }

    [Theory]
    [InlineData("Request:TubeTypes:TubeTypes:0:InnerDiameter,15\r\n", "Request:TubeTypes:TubeTypes:0:InnerDiameter", "15")]
    [InlineData("Lis:LisChannels:0:AstmSettings:CommunicationSettings:CommunicationMode,Tcp\r\n", "Lis:LisChannels:0:AstmSettings:CommunicationSettings:CommunicationMode", "Tcp")]
    [InlineData("Lis:LisChannels:0:Name,Seeded ASTM 1001\r\n", "Lis:LisChannels:0:Name", "Seeded ASTM 1001")]
    [InlineData("Lis:LisChannels:1:Name,Lis2\r\n", "Lis:LisChannels:1:Name", "Lis2")]
    [InlineData("Request:AutomaticDeleteSettings:JobRunTime,01:00\r\n", "Request:AutomaticDeleteSettings:JobRunTime", "01:00")]
    [InlineData("Request:AutomaticDeleteSettings:JobRunTime,23:59\r\n", "Request:AutomaticDeleteSettings:JobRunTime", "23:59")]
    [Trait("ModuleRequirement", "MR_3648")]
    public void ParseCsvToSettings_ShouldReturnPageWithChildren_WhenValidCsv(string csvContent, string expectedKey, string expectedValue)
    {
        var defaultSettings = CreateDefaultSettings();
        var csvWithHeader = AddCsvHeader(csvContent);
        var (isImportSuccessful, page, importErrors, _) = SettingsImporter.ParseCsvToSettings(csvWithHeader, defaultSettings);

        Assert.True(isImportSuccessful);
        Assert.NotEmpty(page.Children);
        Assert.Single(page.Children);
        Assert.Equal(expectedKey, page.Children[0].Key);
        Assert.IsType<Option>(page.Children[0]);
        var option = page.Children[0] as Option;
        Assert.Equal(expectedValue, option?.Value);
        Assert.Empty(importErrors);
    }

    [Theory]
    // parameters to generate a CSV containing only valid values
    [InlineData("Lis:LisChannels", 3, "AstmSettings:CommunicationSettings:CommunicationMode", "Tcp", 0, "", "", true)]
    [InlineData("Lis:LisChannels", 3, "AstmSettings:CommunicationSettings:TcpSettings:TcpReconnectionInterval", "5000", 0, "", "", true)]
    [InlineData("Lis:LisChannels", 5, "AstmSettings:CommunicationSettings:TcpSettings:SocketType", "Client", 0, "", "", true)]
    [InlineData("Lis:LisChannels", 10, "AstmSettings:CommunicationSettings:TcpSettings:IpAddress", "127.0.0.1", 0, "", "", true)]
    [InlineData("Lis:LisChannels", 10, "AstmSettings:CommunicationSettings:TcpSettings:TcpPort", "1001", 0, "", "", true)]
    [InlineData("Lis:LisChannels", 35, "Name", "Seeded ASTM 1001", 0, "", "", true)]
    [InlineData("Request", 4, "SecondsUntilMissingTestOrder", "36", 0, "", "", true)]
    [InlineData("Request:TubeTypes:TubeTypes", 4, "InnerBottomLevel", "2", 0, "", "", true)]
    [InlineData("Request:TubeTypes:TubeTypes", 8, "InnerDiameter", "15", 0, "", "", true)]
    [InlineData("Request:TubeTypes:TubeTypes", 2, "Name", "Tube Type 1", 0, "", "", true)]
    [InlineData("Request:TubeTypes:TubeTypes", 2, "TubeDefinitionId", "1", 0, "", "", true)]
    [InlineData("Request:TubeTypes:TubeTypes", 7, "BottomShape", "Flat", 0, "", "", true)]
    [Trait("ModuleRequirement", "MR_3648")]
    public void ParseCsvToSettings_ShouldReturnPageWithNumberOfValidChildren_FoundInCsv(string prefix, int numValidSettings, string validKey, string validValue, int numInvalidSettings, string invalidKey, string invalidValue, bool isImportExpectedSuccessful)
    {
        var defaultSettings = CreateDefaultSettings();
        var csvContent = GenerateCsvContent(prefix, numValidSettings, validKey, validValue, numInvalidSettings, invalidKey, invalidValue);
        var csvWithHeader = AddCsvHeader(csvContent);
        var (isImportSuccessful, page, importErrors, _) = SettingsImporter.ParseCsvToSettings(csvWithHeader, defaultSettings);

        Assert.Equal(isImportExpectedSuccessful, isImportSuccessful);
        Assert.Equal(numValidSettings, page.Children.Count);
        Assert.Equal(numInvalidSettings, importErrors.Count);
    }

    [Theory]
    [InlineData("Lis:LisChannels:0:Name,TestValue\r\n")]
    [Trait("ModuleRequirement", "MR_3651")]
    public void ParseCsvToSettings_ShouldReturnMalformedFileError_WhenCsvHasNoValidHeader(string csvContent)
    {
        var defaultSettings = CreateDefaultSettings();
        var (isImportSuccessful, page, importErrors, _) = SettingsImporter.ParseCsvToSettings(csvContent, defaultSettings);

        Assert.False(isImportSuccessful);
        Assert.Empty(page.Children);
        Assert.Single(importErrors);
        Assert.Equal(SettingImportError.MalformedFile, importErrors[0].Error);
    }

    [Theory]
    [InlineData("Request:TubeTypes:TubeTypes:0:BottomShape,NonExistingValue", new[] { SettingImportError.InvalidEnumValue })]
    [InlineData("Lis:LisChannels:1:Name,FirstValue\nLis:LisChannels:1:Name,SecondValue", new[] { SettingImportError.DuplicateKey, SettingImportError.DuplicateKey })]
    [InlineData("Lis:LisChannels:1:Name,FirstValue\nLis:LisChannels:1:Name,SecondValue\nLis:LisChannels:1:Name,ThirdValue", new[] { SettingImportError.DuplicateKey, SettingImportError.DuplicateKey, SettingImportError.DuplicateKey })]
    [InlineData("Lis:LisChannels:0:Name\r\n", new[] { SettingImportError.MissingKeyValueDelimiter })]
    [Trait("ModuleRequirement", "MR_5121")]
    public void ParseCsvToSettings_ShouldReturnValidationErrors_WhenCsvHasInvalidValues(string csvContent, SettingImportError[] expectedErrors)
    {
        var defaultSettings = CreateDefaultSettings();
        var csvWithHeader = AddCsvHeader(csvContent);
        var (isImportSuccessful, page, importErrors, _) = SettingsImporter.ParseCsvToSettings(csvWithHeader, defaultSettings);

        Assert.False(isImportSuccessful);
        var expectedErrorGroups = expectedErrors.GroupBy(err => err)
            .ToDictionary(g => g.Key, g => g.Count());
        foreach (var (expectedError, expectedErrorCount) in expectedErrorGroups)
        {
            var importErrorCount = importErrors.Count(x => x.Error == expectedError);
            Assert.Equal(expectedErrorCount, importErrorCount);
        }
    }

    [Theory]
    [InlineData("NonExistingRoot:TubeTypes:TubeTypes:0:InnerDiameter,15\r\n")]
    [InlineData("Request:TubeTypes:TubeTypes:0:NonExistingProperty,15\r\n")]
    [Trait("ModuleRequirement", "MR_3653")]
    public void ParseCsvToSettings_ShouldIgnoreUnrecognizedSettings(string csvContent)
    {
        var defaultSettings = CreateDefaultSettings();
        var csvWithHeader = AddCsvHeader(csvContent);
        var (isImportSuccessful, page, importErrors, unrecognizedSettings) = SettingsImporter.ParseCsvToSettings(csvWithHeader, defaultSettings);

        Assert.True(isImportSuccessful);
        Assert.Empty(page.Children);
        Assert.Empty(importErrors);
        Assert.NotNull(unrecognizedSettings);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_6002")]
    public void ParseCsvToSettings_ShouldSkipInstrumentSpecificSettings()
    {
        var defaultSettings = CreateDefaultSettings();
        defaultSettings["Instrument:Instruments:Name"] = CreateDefaultOption(OptionType.String);
        var csvWithHeader = AddCsvHeader("Instrument:Instruments:0:Name,Instrument A\r\n");
        var excludedPrefixes = new HashSet<string>(StringComparer.Ordinal)
        {
            "Instrument:Instruments:"
        };
        var exclusions = new ImportExportExclusions(excludedPrefixes, new HashSet<string>(StringComparer.Ordinal));

        var (isImportSuccessful, page, importErrors, unrecognizedSettings) =
            SettingsImporter.ParseCsvToSettings(csvWithHeader, defaultSettings, exclusions);

        Assert.True(isImportSuccessful);
        Assert.Empty(page.Children);
        Assert.Empty(importErrors);
        Assert.Single(unrecognizedSettings);
        Assert.Equal("Instrument:Instruments:0:Name", unrecognizedSettings[0].Key);
    }

    [Theory]
    [InlineData("Request:SecondsUntilMissingTestOrder,36\r\n", "Request:SecondsUntilMissingTestOrder")]
    [InlineData("Request:TubeTypes:TubeTypes:0:TubeDefinitionId,1\r\n", "Request:TubeTypes:TubeTypes:TubeDefinitionId")]
    public void ParseCsvToSettings_ShouldSkipExactExcludedSettings(string csvContent, string excludedKey)
    {
        var defaultSettings = CreateDefaultSettings();
        var csvWithHeader = AddCsvHeader(csvContent);
        var exclusions = new ImportExportExclusions(
            new HashSet<string>(StringComparer.Ordinal),
            new HashSet<string>(StringComparer.Ordinal) { excludedKey });

        var (isImportSuccessful, page, importErrors, unrecognizedSettings) =
            SettingsImporter.ParseCsvToSettings(csvWithHeader, defaultSettings, exclusions);

        Assert.True(isImportSuccessful);
        Assert.Empty(page.Children);
        Assert.Empty(importErrors);
        Assert.Single(unrecognizedSettings);
    }

    [Fact]
    public void ParseCsvToSettings_ShouldSkipExcludedSubtreeInsidePageListEntry()
    {
        var defaultSettings = CreateDefaultSettings();
        defaultSettings["Request:TubeTypes:TubeTypes:Dimensions:Height"] = CreateDefaultOption(OptionType.Integer, "10");
        var csvWithHeader = AddCsvHeader("Request:TubeTypes:TubeTypes:0:Dimensions:Height,20\r\n");
        var exclusions = new ImportExportExclusions(
            new HashSet<string>(StringComparer.Ordinal) { "Request:TubeTypes:TubeTypes:Dimensions:" },
            new HashSet<string>(StringComparer.Ordinal));

        var (isImportSuccessful, page, importErrors, unrecognizedSettings) =
            SettingsImporter.ParseCsvToSettings(csvWithHeader, defaultSettings, exclusions);

        Assert.True(isImportSuccessful);
        Assert.Empty(page.Children);
        Assert.Empty(importErrors);
        Assert.Single(unrecognizedSettings);
    }

    [Fact]
    public void ParseCsvToSettings_ShouldSkipActionButtons()
    {
        BaseDefaultSettingsProvider.Initialize(
            [typeof(ActionButtonWithOptionPage).Assembly],
            _ => [typeof(ActionButtonWithOptionPage)]);

        var defaultSettings = BaseDefaultSettingsProvider.GetDefaultOptions();
        var csvWithHeader = AddCsvHeader("ActionButtonWithOptionPage:ExportButton,True\r\n");

        var (isImportSuccessful, page, importErrors, unrecognizedSettings) =
            SettingsImporter.ParseCsvToSettings(csvWithHeader, defaultSettings);

        Assert.True(isImportSuccessful);
        Assert.Empty(page.Children);
        Assert.Empty(importErrors);
        Assert.Single(unrecognizedSettings);
    }

    [Theory]
    [InlineData("Identity:PasswordComplexitySettings:RequireDigit,22\r\n")] // RequireDigit is bool, value is an int (invalid)
    [InlineData("Request:DaysBeforeReuseOfSampleId,someString\r\n")] // DaysBeforeReuseOfSampleId is int, value is a string (invalid)
    [InlineData("Request:TubeTypes:TubeTypes:0:InnerBottomLevel,notAnInt\r\n")] // InnerBottomLevel is int, value is string (invalid)
    [InlineData("Request:TubeTypes:TubeTypes:0:InnerDiameter,notADouble\r\n")] // InnerDiameter is double, value is string (invalid)
    [InlineData("Request:AutomaticDeleteSettings:JobRunTime,25:00\r\n")] // JobRunTime is TimeOnly, value is invalid hour
    [InlineData("Request:AutomaticDeleteSettings:JobRunTime,23:60\r\n")] // JobRunTime is TimeOnly, value is invalid minute
    [InlineData("Request:AutomaticDeleteSettings:JobRunTime,notATime\r\n")] // JobRunTime is TimeOnly, value is string (invalid)
    [Trait("ModuleRequirement", "MR_5121")]
    public void ParseCsvToSettings_ShouldReturnInvalidValueError_WhenOptionValueIsInvalid(string csvContent)
    {
        var defaultSettings = CreateDefaultSettings();
        var csvWithHeader = AddCsvHeader(csvContent);
        var (isImportSuccessful, page, importErrors, _) = SettingsImporter.ParseCsvToSettings(csvWithHeader, defaultSettings);

        Assert.False(isImportSuccessful);
        Assert.Empty(page.Children);
        Assert.Single(importErrors);
        Assert.Equal(SettingImportError.InvalidValue, importErrors[0].Error);
    }

    [Theory]
    [InlineData("GeneralSettings:RegionalSettings:TimeZone,Pacific Standard Time\r\n", "Pacific Standard Time")]
    [InlineData("GeneralSettings:RegionalSettings:TimeZone,UTC\r\n", "UTC")]
    [InlineData("GeneralSettings:RegionalSettings:TimeZone,Tokyo Standard Time\r\n", "Tokyo Standard Time")]
    [Trait("ModuleRequirement", "MR_3648")]
    public void ParseCsvToSettings_ShouldAcceptProviderBasedDynamicDropdown_WhenImportingTimeZone(string csvContent, string expectedValue)
    {
        var defaultSettings = new Dictionary<string, Option>
        {
            ["GeneralSettings:RegionalSettings:TimeZone"] = CreateDefaultOption(
                OptionType.Choice,
                defaultValue: "UTC",
                dynamicDropdownProviderType: typeof(TimeZoneDropdownProvider))
        };

        var csvWithHeader = AddCsvHeader(csvContent);
        var (isImportSuccessful, page, importErrors, _) = SettingsImporter.ParseCsvToSettings(csvWithHeader, defaultSettings);

        Assert.True(isImportSuccessful);
        Assert.Single(page.Children);
        var option = page.Children[0] as Option;
        Assert.Equal(expectedValue, option?.Value);
        Assert.Empty(importErrors);
    }

    private static string GenerateCsvContent(string prefix, int numValidSettings, string validKey, string validValue, int numInvalidSettings, string invalidKey, string invalidValue)
    {
        var csvContent = new StringBuilder();
        for (var i = 0; i < numValidSettings; i++)
        {
            csvContent.Append(string.Format(CultureInfo.InvariantCulture, $"{prefix}:{i}:{validKey},{validValue}\r\n"));
        }
        for (var i = 0; i < numInvalidSettings; i++)
        {
            csvContent.Append(string.Format(CultureInfo.InvariantCulture, $"{prefix}:{i}:{invalidKey},{invalidValue}\r\n"));
        }
        return csvContent.ToString();
    }

    private static Option CreateDefaultOption(
    OptionType type,
    string? defaultValue = "",
    bool required = false,
    bool unique = false,
    string minValue = "0",
    string maxValue = "0",
    int maxLength = int.MaxValue,
    bool isColumnHeader = false,
    bool isNameColumn = false,
    string unitResourceName = "",
    string dynamicDropdownOrigin = "",
    string dynamicDropdownValueTarget = "",
    bool isDisplayModeRadioButtons = false,
    List<Choice>? choices = null,
    Type? dynamicDropdownProviderType = null)
    {
        return new Option
        {
            Type = type,
            Required = required,
            Unique = unique,
            DefaultValue = defaultValue,
            Value = defaultValue,
            MinValue = minValue,
            MaxValue = maxValue,
            MaxLength = maxLength,
            IsColumnHeader = isColumnHeader,
            IsNameColumn = isNameColumn,
            UnitResourceName = unitResourceName,
            DynamicDropdownOrigin = dynamicDropdownOrigin,
            DynamicDropdownValueTarget = dynamicDropdownValueTarget,
            IsDisplayModeRadioButtons = isDisplayModeRadioButtons,
            Choices = choices ?? [],
            Dependency = [],
            DynamicDropdownProviderType = dynamicDropdownProviderType
        };
    }

    private static string AddCsvHeader(string csvContent)
    {
        return $"{PageHelper.SettingsAsteriskLineAfterHeader}\r\n{csvContent}";
    }

    [Theory]
    [InlineData("true", "True")]
    [InlineData("TRUE", "True")]
    [InlineData("True", "True")]
    [InlineData("false", "False")]
    [InlineData("FALSE", "False")]
    [InlineData("False", "False")]
    [InlineData("1", "True")]
    [InlineData("0", "False")]
    public void ParseCsvToSettings_BooleanValues_NormalizedToTitleCase(string importedValue, string expectedNormalized)
    {
        var defaultSettings = CreateDefaultSettings();
        var csvContent = AddCsvHeader($"Identity:PasswordComplexitySettings:RequireDigit,{importedValue}\r\n");

        var (isImportSuccessful, page, importErrors, _) = SettingsImporter.ParseCsvToSettings(csvContent, defaultSettings);

        Assert.True(isImportSuccessful);
        Assert.Single(page.Children);
        var option = Assert.IsType<Option>(page.Children[0]);
        Assert.Equal(expectedNormalized, option.Value);
        Assert.Empty(importErrors);
    }

    [Trait("ProductRequirement", "PR_1919")]
    [Trait("ModuleRequirement", "MR_3926")]
    [Fact]
    public void ParseCsvToSettings_ShouldCopyNonOverlappingRangeMetadataToPageListEntry()
    {
        var validation = new NonOverlappingRangesValidation
        {
            FirstStartKey = "Root:Entries:FirstStart",
            FirstLengthKey = "Root:Entries:FirstLength",
            SecondStartKey = "Root:Entries:SecondStart",
            SecondLengthKey = "Root:Entries:SecondLength",
            EnabledWhen =
            [
                new Dependency
                {
                    MainOptionKey = "Root:Entries:Enabled",
                    MainOptionValue = true.ToString(),
                    Type = DependencyType.Disable
                }
            ]
        };
        var defaultSettings = new Dictionary<string, Option>
        {
            ["Root:Entries:FirstStart"] = CreateDefaultOption(OptionType.Integer, "1")
        };
        defaultSettings["Root:Entries:FirstStart"].Key = "Root:Entries:FirstStart";
        defaultSettings["Root:Entries:FirstStart"].NonOverlappingRanges.Add(validation);

        var csvContent = AddCsvHeader("Root:Entries:0:FirstStart,2\r\n");

        var (isImportSuccessful, page, importErrors, _) = SettingsImporter.ParseCsvToSettings(csvContent, defaultSettings);

        Assert.True(isImportSuccessful);
        Assert.Empty(importErrors);
        var option = Assert.IsType<Option>(page.Children.Single());
        var updatedValidation = option.NonOverlappingRanges.Single();
        updatedValidation.RangeKeys.Should().BeEquivalentTo(
            "Root:Entries:0:FirstStart",
            "Root:Entries:0:FirstLength",
            "Root:Entries:0:SecondStart",
            "Root:Entries:0:SecondLength");
        updatedValidation.EnabledWhen.Single().MainOptionKey.Should().Be("Root:Entries:0:Enabled");
    }

}
