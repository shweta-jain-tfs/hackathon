using System.Globalization;
using System.Text;
using MediatR;
using Microsoft.Extensions.Logging;
using Pharos.Server.Application.Settings.Result;
using Pharos.Server.Application.Settings.Attributes;
using Pharos.Server.Application.Settings.Sample;
using Pharos.Server.Settings.Application.Commands;
using Pharos.Server.Settings.Application.Commands.ImportSettings;
using Pharos.Server.Settings.Application.Commands.NotifySettingsChanged;
using Pharos.Server.Settings.Application.Interfaces;
using Pharos.Server.Settings.Application.Metadata;
using Pharos.Server.Settings.Application.Services;
using Pharos.Server.Settings.Application.Validation;
using Pharos.Server.Settings.Domain;
using Pharos.Server.Settings.Infrastructure.ConfigurationProvider;

namespace Pharos.Server.Settings.Commands;

public class ImportSettingsCommandTests
{
    private readonly Mock<ISettingsDbContext> _mockDbContext = new();
    private readonly Mock<ISender> _mockSender = new();
    private readonly Mock<IPageListMaxItemsValidator> _mockValidator = new();
    private readonly ImportSettingsCommandHandler _sut;

    public ImportSettingsCommandTests()
    {
        _mockDbContext.Setup(s => s.Settings).ReturnsDbSet(new List<Setting>());
        _mockValidator.Setup(v => v.ValidateImportedPageLists(It.IsAny<Page>()))
            .Returns(PageListMaxItemsValidationResult.Success());

        _sut = new ImportSettingsCommandHandler(
            _mockDbContext.Object,
            Mock.Of<ILogger<ImportSettingsCommandHandler>>(),
            _mockSender.Object,
            _mockValidator.Object);

        BaseDefaultSettingsProvider.Initialize([]);
    }

    [Theory]
    [InlineData(5, 3, 0, 0)]
    [InlineData(5, 3, 2, 0)]
    [InlineData(5, 0, 5, 0)]
    [InlineData(0, 0, 0, 5)]
    [InlineData(0, 0, 0, 0)]
    [InlineData(4, 4, 0, 0)]
    [InlineData(4, 4, 0, 1)]
    [InlineData(4, 1, 3, 1)]
    [InlineData(4, 0, 0, 0)]
    [Trait("ModuleRequirement", "MR_3648")]
    public async Task Should_insert_all_non_default_settings_from_csv(int numDbSettings, int numCsvSettingsSameValue, int numCsvSettingsDifferentValue, int numCsvSettingsNewKeys)
    {
        const string pageListPrefix = "ImportNonDefaultSettingsPage:Entries";
        var standaloneNonDefaultSetting = new Setting()
        {
            Key = "ImportNonDefaultSettingsPage:StandaloneFlag",
            Value = "true",
        };

        BaseDefaultSettingsProvider.Initialize([GetType().Assembly], _ => [typeof(ImportNonDefaultSettingsPage)]);

        (string name, OptionType type, bool isId)[] dbProperties = [
            ("Name", OptionType.String, false),
            ("TubeDefinitionId", OptionType.Integer, true),
            ("InnerBottomLevel", OptionType.Double, false),
            ("InnerDiameter", OptionType.Double, false),
            ("SampleDefault", OptionType.Boolean, false)];

        var dbSettings = GenerateTestSettings(
            numDbSettings,
            pageListPrefix,
            dbProperties);
        dbSettings.Add(standaloneNonDefaultSetting);

        var (importHandler, mockDbContext) = GenerateImportSettingsCommandHandler(dbSettings);
        (string name, OptionType type, bool isId)[] properties = [
            ("Name", type: OptionType.String, false),
            ("TubeDefinitionId", type: OptionType.Integer, true),
            ("InnerBottomLevel", type: OptionType.Double, false),
            ("InnerDiameter", type: OptionType.Double, false)];

        (string name, OptionType type, bool isId)[] newKeyProperties = [
            ("Name", type: OptionType.String, false),
            ("TubeDefinitionId", type: OptionType.Integer, true),
            ("InnerBottomLevel", type: OptionType.Double, false),
            ("InnerDiameter", type: OptionType.Double, false)];

        var csvSettings = GenerateTestSettings(
            numCsvSettingsSameValue,
            numCsvSettingsDifferentValue,
            numCsvSettingsNewKeys,
            numDbSettings,
            pageListPrefix,
            properties,
            newKeyProperties);
        csvSettings.Add(standaloneNonDefaultSetting);

        var command = new ImportSettingsCommand(GenerateCsvContent(csvSettings));

        await importHandler.Handle(command, TestContext.Current.CancellationToken);

        mockDbContext.Verify(m => m.Settings.Update(It.IsAny<Setting>()), Times.Never);
        mockDbContext.Verify(m => m.Settings.Add(It.IsAny<Setting>()), Times.Exactly(csvSettings.Count));
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_3648")]
    public async Task Should_not_insert_or_update_when_csv_contains_default_setting()
    {
        var dbSettings = new List<Setting>();
        var (importHandler, mockDbContext) = GenerateImportSettingsCommandHandler(dbSettings);

        BaseDefaultSettingsProvider.Initialize([GetType().Assembly], _ => [typeof(ImportBasicPage)]);

        var csvContent = $"{PageHelper.SettingsAsteriskLineAfterHeader}\r\n" +
            "ImportBasicPage:IntSetting,5";
        var command = new ImportSettingsCommand(csvContent);

        await importHandler.Handle(command, TestContext.Current.CancellationToken);

        mockDbContext.Verify(m => m.Settings.Update(It.IsAny<Setting>()), Times.Never);
        mockDbContext.Verify(m => m.Settings.Add(It.IsAny<Setting>()), Times.Never);
    }

    [Fact]
    [Trait("ProductRequirement", "PR_531")]
    [Trait("ModuleRequirement", "MR_3648")]
    public async Task MR_3648_Should_notify_updated_key_when_import_resets_stored_setting_to_default()
    {
        var setting = new Setting
        {
            Key = "ImportBasicPage:IntSetting",
            Value = "10"
        };

        _mockDbContext.Setup(s => s.Settings).ReturnsDbSet(new List<Setting> { setting });
        BaseDefaultSettingsProvider.Initialize([GetType().Assembly], _ => [typeof(ImportBasicPage)]);

        var csvContent = $"{PageHelper.SettingsAsteriskLineAfterHeader}\r\n" +
            "ImportBasicPage:IntSetting,5";
        var command = new ImportSettingsCommand(csvContent);

        await _sut.Handle(command, TestContext.Current.CancellationToken);

        _mockSender.Verify(sender =>
            sender.Send(It.Is<NotifySettingsChangedCommand>(cmd =>
                    cmd.UpdatedKeys != null &&
                    cmd.UpdatedKeys.Contains("ImportBasicPage:IntSetting") &&
                    cmd.DeletedKeys != null &&
                    !cmd.DeletedKeys.Contains("ImportBasicPage:IntSetting")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("ProductRequirement", "PR_531")]
    [Trait("ModuleRequirement", "MR_3648")]
    public async Task Should_notify_deleted_page_list_entry_key_when_import_removes_entry()
    {
        var setting = new Setting
        {
            Key = "ImportPageListPage:Pages:5:Name",
            Value = "Channel6"
        };

        _mockDbContext.Setup(s => s.Settings).ReturnsDbSet(new List<Setting> { setting });
        BaseDefaultSettingsProvider.Initialize([GetType().Assembly], _ => [typeof(ImportBasicPage), typeof(ImportPageListPage)]);

        var csvContent = $"{PageHelper.SettingsAsteriskLineAfterHeader}\r\n" +
            "ImportBasicPage:Name,ImportedName";
        var command = new ImportSettingsCommand(csvContent);

        await _sut.Handle(command, TestContext.Current.CancellationToken);

        _mockSender.Verify(sender =>
            sender.Send(It.Is<NotifySettingsChangedCommand>(cmd =>
                    cmd.DeletedKeys != null &&
                    cmd.DeletedKeys.Contains("ImportPageListPage:Pages:5")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("ProductRequirement", "PR_531")]
    [Trait("ModuleRequirement", "MR_3648")]
    public async Task Should_notify_updated_page_list_entry_key_when_import_changes_entry_value()
    {
        var setting = new Setting
        {
            Key = "ImportPageListPage:Pages:5:Name",
            Value = "Channel6"
        };

        _mockDbContext.Setup(s => s.Settings).ReturnsDbSet(new List<Setting> { setting });
        BaseDefaultSettingsProvider.Initialize([GetType().Assembly], _ => [typeof(ImportPageListPage)]);

        var csvContent = $"{PageHelper.SettingsAsteriskLineAfterHeader}\r\n" +
            "ImportPageListPage:Pages:5:Name,ImportedChannel";
        var command = new ImportSettingsCommand(csvContent);

        await _sut.Handle(command, TestContext.Current.CancellationToken);

        _mockSender.Verify(sender =>
            sender.Send(It.Is<NotifySettingsChangedCommand>(cmd =>
                    cmd.UpdatedKeys != null &&
                    cmd.UpdatedKeys.Contains("ImportPageListPage:Pages:5") &&
                    cmd.UpdatedKeys.Contains("ImportPageListPage:Pages") &&
                    cmd.UpdatedKeys.Contains("ImportPageListPage:Pages:5:Name") &&
                    cmd.DeletedKeys != null &&
                    !cmd.DeletedKeys.Contains("ImportPageListPage:Pages:5")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("ProductRequirement", "PR_531")]
    [Trait("ModuleRequirement", "MR_3648")]
    public async Task Should_notify_updated_page_list_entry_key_when_import_resets_entry_setting_to_default()
    {
        var settings = new List<Setting>
        {
            new()
            {
                Key = "ImportNonDefaultSettingsPage:Entries:5:Name",
                Value = "Channel6"
            },
            new()
            {
                Key = "ImportNonDefaultSettingsPage:Entries:5:InnerDiameter",
                Value = "11.1"
            }
        };

        _mockDbContext.Setup(s => s.Settings).ReturnsDbSet(settings);
        BaseDefaultSettingsProvider.Initialize([GetType().Assembly], _ => [typeof(ImportNonDefaultSettingsPage)]);

        var csvContent = $"{PageHelper.SettingsAsteriskLineAfterHeader}\r\n" +
            "ImportNonDefaultSettingsPage:Entries:5:Name,Channel6";
        var command = new ImportSettingsCommand(csvContent);

        await _sut.Handle(command, TestContext.Current.CancellationToken);

        _mockSender.Verify(sender =>
            sender.Send(It.Is<NotifySettingsChangedCommand>(cmd =>
                    cmd.UpdatedKeys != null &&
                    cmd.UpdatedKeys.Contains("ImportNonDefaultSettingsPage:Entries:5") &&
                    cmd.UpdatedKeys.Contains("ImportNonDefaultSettingsPage:Entries") &&
                    cmd.DeletedKeys != null &&
                    cmd.DeletedKeys.Contains("ImportNonDefaultSettingsPage:Entries:5:InnerDiameter") &&
                    !cmd.DeletedKeys.Contains("ImportNonDefaultSettingsPage:Entries:5")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("ProductRequirement", "PR_531")]
    [Trait("ModuleRequirement", "MR_5122")]
    public async Task MR_5122_Should_not_insert_or_update_when_csv_has_no_valid_setting()
    {
        var dbSettings = GenerateTestSettings(
            14,
            "Request:TubeTypes:TubeTypeList",
            [("Name", OptionType.String, false), ("TubeDefinitionId", OptionType.Integer, true)]);
        var (importHandler, mockDbContext) = GenerateImportSettingsCommandHandler(dbSettings);
        const string csvContent = "test invalid csv";
        var command = new ImportSettingsCommand(csvContent);

        await importHandler.Handle(command, TestContext.Current.CancellationToken);

        mockDbContext.Verify(m => m.Settings.Update(It.IsAny<Setting>()), Times.Never);
        mockDbContext.Verify(m => m.Settings.Add(It.IsAny<Setting>()), Times.Never);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_3648")]
    public async Task Should_insert_when_imported_page_list_entry_has_same_name_as_existing_entry()
    {
        BaseDefaultSettingsProvider.Initialize([GetType().Assembly], _ => [typeof(ImportPageListPage)]);

        var dbSettings = new List<Setting>
        {
            new() { Key = "ImportPageListPage:Pages:0:Name", Value = "Channel1" }
        };
        var (importHandler, mockDbContext) = GenerateImportSettingsCommandHandler(dbSettings);

        var csvContent = $"{PageHelper.SettingsAsteriskLineAfterHeader}\r\n" +
            "ImportPageListPage:Pages:1:Name,Channel1\r\n";
        var command = new ImportSettingsCommand(csvContent);

        var result = await importHandler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Empty(result.Errors);
        mockDbContext.Verify(m => m.Settings.Remove(It.IsAny<Setting>()), Times.Once);
        mockDbContext.Verify(m => m.Settings.Add(
            It.Is<Setting>(s =>
                s.Key == "ImportPageListPage:Pages:1:Name" &&
                s.Value == "Channel1")),
            Times.Once);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5122")]
    public async Task Should_fail_import_when_max_items_exceeded()
    {
        var csvContent = $"{PageHelper.SettingsAsteriskLineAfterHeader}\r\n" +
            "Lis:LisChannels:0:Name,Channel1\r\n" +
            "Lis:LisChannels:1:Name,Channel2\r\n" +
            "Lis:LisChannels:2:Name,Channel3\r\n" +
            "Lis:LisChannels:3:Name,Channel4\r\n" +
            "Lis:LisChannels:4:Name,Channel5\r\n" +
            "Lis:LisChannels:5:Name,Channel6\r\n" +
            "Lis:LisChannels:6:Name,Channel7\r\n" +
            "Lis:LisChannels:7:Name,Channel8\r\n" +
            "Lis:LisChannels:8:Name,Channel9\r\n" +
            "Lis:LisChannels:9:Name,Channel10\r\n" +
            "Lis:LisChannels:10:Name,Channel11";

        var validationResult = PageListMaxItemsValidationResult.Failure(
            [new FailedSettingImportError("Lis:LisChannels", "11", SettingImportError.MaxItemsExceeded)]);

        _mockValidator.Setup(v => v.ValidateImportedPageLists(It.IsAny<Page>()))
            .Returns(validationResult);

        var result = await _sut.Handle(new ImportSettingsCommand(csvContent), TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Error == SettingImportError.MaxItemsExceeded);
        _mockDbContext.Verify(m => m.Settings.Add(It.IsAny<Setting>()), Times.Never);
        _mockDbContext.Verify(m => m.Settings.Update(It.IsAny<Setting>()), Times.Never);
        _mockDbContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockValidator.Verify(v => v.ValidateImportedPageLists(It.IsAny<Page>()), Times.Once);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_3648")]
    public async Task Should_succeed_import_when_at_max_limit()
    {
        var csvContent =
            $"{PageHelper.SettingsAsteriskLineAfterHeader}\r\n" +
            "Lis:LisChannels:0:Name,Channel1\r\n" +
            "Lis:LisChannels:1:Name,Channel2\r\n" +
            "Lis:LisChannels:2:Name,Channel3\r\n" +
            "Lis:LisChannels:3:Name,Channel4\r\n" +
            "Lis:LisChannels:4:Name,Channel5\r\n" +
            "Lis:LisChannels:5:Name,Channel6\r\n" +
            "Lis:LisChannels:6:Name,Channel7\r\n" +
            "Lis:LisChannels:7:Name,Channel8\r\n" +
            "Lis:LisChannels:8:Name,Channel9\r\n" +
            "Lis:LisChannels:9:Name,Channel10";

        _mockValidator.Setup(v => v.ValidateImportedPageLists(It.IsAny<Page>()))
            .Returns(PageListMaxItemsValidationResult.Success());

        var result = await _sut.Handle(new ImportSettingsCommand(csvContent), TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        _mockValidator.Verify(v => v.ValidateImportedPageLists(It.IsAny<Page>()), Times.Once);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_3648")]
    public async Task Should_succeed_import_when_below_max_limit()
    {
        var csvContent =
            $"{PageHelper.SettingsAsteriskLineAfterHeader}\r\n" +
            "Lis:LisChannels:0:Name,Channel1\r\n" +
            "Lis:LisChannels:1:Name,Channel2";

        _mockValidator.Setup(v => v.ValidateImportedPageLists(It.IsAny<Page>()))
            .Returns(PageListMaxItemsValidationResult.Success());

        var result = await _sut.Handle(new ImportSettingsCommand(csvContent), TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        _mockValidator.Verify(v => v.ValidateImportedPageLists(It.IsAny<Page>()), Times.Once);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5121")]
    public async Task Should_validate_page_list_max_items_during_import()
    {
        var csvContent = $"{PageHelper.SettingsAsteriskLineAfterHeader}\r\n" +
            "Lis:LisChannels:0:Name,Channel1";

        _mockValidator.Setup(v => v.ValidateImportedPageLists(It.IsAny<Page>()))
            .Returns(PageListMaxItemsValidationResult.Success());

        await _sut.Handle(new ImportSettingsCommand(csvContent), TestContext.Current.CancellationToken);

        _mockValidator.Verify(v => v.ValidateImportedPageLists(It.IsAny<Page>()), Times.Once);
    }

    [Trait("ProductRequirement", "PR_2136")]
    [Trait("ModuleRequirement", "MR_5805")]
    [Fact]
    public async Task Should_import_blank_result_export_paths_when_exports_are_disabled()
    {
        BaseDefaultSettingsProvider.Initialize([typeof(ResultSettings).Assembly], _ => [typeof(ResultSettings)]);
        var (importHandler, mockDbContext) = GenerateImportSettingsCommandHandler([]);

        var csvContent = $"{PageHelper.SettingsAsteriskLineAfterHeader}\r\n" +
            "Result:Export:ExportOnAnalyticalRunEnd,False\r\n" +
            "Result:Export:AnalyticalRunExportPath,\r\n" +
            "Result:Export:ExportOnCustomRunEnd,False\r\n" +
            "Result:Export:CustomRunExportPath,\r\n";

        var result = await importHandler.Handle(new ImportSettingsCommand(csvContent), TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Empty(result.Errors);
        mockDbContext.Verify(m => m.Settings.Add(It.IsAny<Setting>()), Times.Never);
        mockDbContext.Verify(m => m.Settings.Update(It.IsAny<Setting>()), Times.Never);
        mockDbContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Trait("ProductRequirement", "PR_2136")]
    [Trait("ModuleRequirement", "MR_5805")]
    [Fact]
    public async Task Should_fail_import_for_blank_result_export_path_when_export_is_enabled()
    {
        BaseDefaultSettingsProvider.Initialize([typeof(ResultSettings).Assembly], _ => [typeof(ResultSettings)]);
        var (importHandler, mockDbContext) = GenerateImportSettingsCommandHandler([]);

        var csvContent = $"{PageHelper.SettingsAsteriskLineAfterHeader}\r\n" +
            "Result:Export:ExportOnAnalyticalRunEnd,True\r\n" +
            "Result:Export:AnalyticalRunExportPath,\r\n" +
            "Result:Export:ExportOnCustomRunEnd,False\r\n" +
            "Result:Export:CustomRunExportPath,\r\n";

        var result = await importHandler.Handle(new ImportSettingsCommand(csvContent), TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        var error = Assert.Single(result.Errors);
        Assert.Equal("Result:Export:AnalyticalRunExportPath", error.Key);
        Assert.Equal(SettingImportError.RequiredSettingNotDefined, error.Error);
        mockDbContext.Verify(m => m.Settings.Add(It.IsAny<Setting>()), Times.Never);
        mockDbContext.Verify(m => m.Settings.Update(It.IsAny<Setting>()), Times.Never);
        mockDbContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Trait("ProductRequirement", "PR_531")]
    [Trait("ModuleRequirement", "MR_5122")]
    [Fact]
    public async Task Should_fail_import_when_special_sample_handling_sections_overlap()
    {
        BaseDefaultSettingsProvider.Initialize([typeof(SamplesSettings).Assembly], _ => [typeof(SamplesSettings)]);
        var (importHandler, mockDbContext) = GenerateImportSettingsCommandHandler(
            [],
            [new NonOverlappingRangesSettingsValidator()]);

        var csvContent = GenerateCsvContent(
        [
            new() { Key = "Samples:SpecialSampleHandling:ExtractSampleIdFromBarcode", Value = "True" },
            new() { Key = "Samples:SpecialSampleHandling:SampleIdStartPosition", Value = "1" },
            new() { Key = "Samples:SpecialSampleHandling:SampleIdLength", Value = "4" },
            new() { Key = "Samples:SpecialSampleHandling:SampleTubesWithSameSampleId", Value = "True" },
            new() { Key = "Samples:SpecialSampleHandling:ExtractSampleIdSuffixFromBarcode", Value = "True" },
            new() { Key = "Samples:SpecialSampleHandling:SampleIdSuffixStartPosition", Value = "4" },
            new() { Key = "Samples:SpecialSampleHandling:SampleIdSuffixLength", Value = "1" }
        ]);

        var result = await importHandler.Handle(new ImportSettingsCommand(csvContent), TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains(result.Errors, error =>
            error.Key == "Samples:SpecialSampleHandling:SampleIdSuffixStartPosition" &&
            error.Error == SettingImportError.SectionsOverlap);
        mockDbContext.Verify(m => m.Settings.Add(It.IsAny<Setting>()), Times.Never);
        mockDbContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static (ImportSettingsCommandHandler, Mock<ISettingsDbContext>) GenerateImportSettingsCommandHandler(
        List<Setting> settings,
        IEnumerable<ISettingsCrossValidator>? crossValidators = null)
    {
        var mockLogger = Mock.Of<ILogger<ImportSettingsCommandHandler>>();
        var mockDbContext = new Mock<ISettingsDbContext>();
        var mockSender = new Mock<ISender>();
        var mockValidator = new Mock<IPageListMaxItemsValidator>();

        mockDbContext.Setup(s => s.Settings).ReturnsDbSet(settings);

        mockValidator.Setup(v => v.ValidateImportedPageLists(It.IsAny<Page>()))
            .Returns(PageListMaxItemsValidationResult.Success());

        var importHandler = new ImportSettingsCommandHandler(mockDbContext.Object, mockLogger, mockSender.Object, mockValidator.Object, crossValidators);
        return (importHandler, mockDbContext);
    }

    private static List<Setting> GenerateTestSettings(int numSettings, string prefixSegments, (string, OptionType, bool)[] properties)
    {
        return GenerateTestSettings(numSettings, 0, 0, 0, prefixSegments, properties, []);
    }

    private static List<Setting> GenerateTestSettings(
        int numCsvSettingsSameValue,
        int numCsvSettingsDifferentValue,
        int numCsvSettingsNewKeys,
        int numDbSettings,
        string prefixSegments,
        (string name, OptionType type, bool isId)[] properties,
        (string name, OptionType type, bool isId)[] newKeyProperties)
    {
        var settings = new List<Setting>();
        var numExistingSettings = numCsvSettingsSameValue + numCsvSettingsDifferentValue;
        for (var i = 1; i < numExistingSettings + 1; i++)
        {
            var isSameValue = i < numCsvSettingsSameValue;
            settings.AddRange(properties.Select(property => new Setting
            {
                Key = $"{prefixSegments}:{i}:{property.name}",
                Value = GetTestValueFromOptionType(
                    property.type,
                    i,
                    GetValueType(isSameValue, property.isId, isSameValue ? ValueType.Value : ValueType.Different),
                    property.name)
            }));
        }

        var j = Math.Max(numExistingSettings, numDbSettings) + 1;
        for (var i = 0; i < numCsvSettingsNewKeys; i++)
        {
            var k = i + j;
            settings.AddRange(newKeyProperties.Select(property => new Setting
            {
                Key = $"{prefixSegments}:{k}:{property.name}",
                Value = GetTestValueFromOptionType(
                    property.type,
                    k,
                    GetValueType(false, property.isId, ValueType.New),
                    property.name)
            }));
        }

        return settings;
    }

    private static ValueType GetValueType(bool isSameValue, bool isId, ValueType defaultValueType)
    {
        if (isSameValue)
        {
            return isId ? ValueType.OldId : ValueType.Value;
        }

        return isId ? ValueType.NewId : defaultValueType;
    }

    private static string GetTestValueFromOptionType(OptionType optionType, int index, ValueType valueType, string propertyName)
    {
        switch (optionType)
        {
            case OptionType.Boolean:
                return valueType == ValueType.Value ? "true" : "false";
            case OptionType.Integer:
                {
                    if (propertyName == "TubeDefinitionId")
                    {
                        int InTubeDefinitionRange(int i) => (Math.Abs(i) % 5) + 1;

                        return valueType switch
                        {
                            ValueType.OldId => $"{InTubeDefinitionRange(index)}",
                            ValueType.NewId => $"{InTubeDefinitionRange(index + 1000)}",
                            ValueType.Value => "0",
                            ValueType.Different => "1",
                            _ => "2",
                        };
                    }

                    return valueType switch
                    {
                        ValueType.OldId => $"{index}",
                        ValueType.NewId => $"{index + 1000}",
                        ValueType.Value => "0",
                        ValueType.Different => "1",
                        _ => "2",
                    };
                }
            case OptionType.Double:
                {
                    return valueType switch
                    {
                        ValueType.Value => "10.0",
                        ValueType.Different => "11.1",
                        _ => "12.2"
                    };
                }
            case OptionType.String:
            default:
                {
                    return valueType switch
                    {
                        ValueType.Value => $"value_{index}",
                        ValueType.Different => $"different_value_{index}",
                        _ => $"new_value_{index}"
                    };
                }
        }
    }

    private static string GenerateCsvContent(List<Setting> settings)
    {
        var csvContent = new StringBuilder();
        csvContent.Append($"{PageHelper.SettingsAsteriskLineAfterHeader}\r\n");
        foreach (var setting in settings)
        {
            csvContent.Append(string.Format(CultureInfo.InvariantCulture, $"{setting.Key},{setting.Value}\r\n"));
        }

        return csvContent.ToString();
    }

    private enum ValueType
    {
        OldId,
        NewId,
        Value,
        Different,
        New,
    }
}

[RootConfigSection(sortOrder: 1)]
[DisplayNameLocal("RootLabel")]
internal class ImportBasicPage
{
    [DisplayNameLocal("Int Label")]
    public int IntSetting { get; set; } = 5;

    [DisplayNameLocal("Name Label")]
    [Unique]
    public string Name { get; set; } = "DefaultName";
}

[RootConfigSection(sortOrder: 20)]
[DisplayNameLocal("RootLabel")]
internal class ImportPageListPage
{
    [DisplayNameLocal("PageList Label")]
    public IEnumerable<ImportBasicPage> Pages { get; set; } = [];
}

[RootConfigSection(sortOrder: 30)]
[DisplayNameLocal("RootLabel")]
internal class ImportNonDefaultSettingsPage
{
    [DisplayNameLocal("Entries Label")]
    public IEnumerable<ImportNonDefaultSettingsEntry> Entries { get; set; } = [];

    [DisplayNameLocal("Standalone Flag Label")]
    public bool StandaloneFlag { get; set; } = false;
}

internal class ImportNonDefaultSettingsEntry
{
    [DisplayNameLocal("Name Label")]
    public string Name { get; set; } = "DefaultName";

    [DisplayNameLocal("Tube Definition Id Label")]
    public int TubeDefinitionId { get; set; } = 0;

    [DisplayNameLocal("Inner Bottom Level Label")]
    public double InnerBottomLevel { get; set; } = 0.0;

    [DisplayNameLocal("Inner Diameter Label")]
    public double InnerDiameter { get; set; } = 0.0;

    [DisplayNameLocal("Sample Default Label")]
    public bool SampleDefault { get; set; } = true;
}
