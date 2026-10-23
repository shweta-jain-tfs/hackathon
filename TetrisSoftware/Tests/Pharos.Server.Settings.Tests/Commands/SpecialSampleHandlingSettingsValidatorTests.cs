using System.Globalization;
using Pharos.Server.Application.Settings;
using Pharos.Server.Application.Settings.Attributes;
using Pharos.Server.Application.Settings.Sample;
using Pharos.Server.Settings.Application.Commands;
using Pharos.Server.Settings.Application.Metadata;
using Pharos.Server.Settings.Application.Services;
using Pharos.Server.Settings.Application.Validation;
using Pharos.Server.Settings.Infrastructure.ConfigurationProvider;

namespace Pharos.Server.Settings.Commands;

public class SpecialSampleHandlingSettingsValidatorTests
{
    [Trait("ProductRequirement", "PR_1919")]
    [Trait("ModuleRequirement", "MR_3926")]
    [Fact]
    public void ValidateOptions_WhenSampleIdAndSuffixSectionsOverlap_ReturnsInvalidValueErrors()
    {
        var options = CreateOptions(sampleStart: 1, sampleLength: 4, suffixStart: 4, suffixLength: 2);

        var errors = ValidateOptions(options);

        errors.Should().HaveCount(4);
        errors.Should().OnlyContain(error => error.Error == SettingImportError.SectionsOverlap);
        errors.Select(error => error.Key).Should().BeEquivalentTo(
            CreateOptionKey(nameof(SpecialSampleHandlingSettings.SampleIdStartPosition)),
            CreateOptionKey(nameof(SpecialSampleHandlingSettings.SampleIdLength)),
            CreateOptionKey(nameof(SpecialSampleHandlingSettings.SampleIdSuffixStartPosition)),
            CreateOptionKey(nameof(SpecialSampleHandlingSettings.SampleIdSuffixLength)));
    }

    [Trait("ProductRequirement", "PR_1919")]
    [Trait("ModuleRequirement", "MR_3926")]
    [Fact]
    public void ValidateOptions_WhenSuffixExtractionIsDisabled_AllowsOverlappingPositions()
    {
        var options = CreateOptions(extractSuffix: false, sampleStart: 1, sampleLength: 4, suffixStart: 4, suffixLength: 2);

        var errors = ValidateOptions(options);

        errors.Should().BeEmpty();
    }

    [Trait("ProductRequirement", "PR_1919")]
    [Trait("ModuleRequirement", "MR_3926")]
    [Fact]
    public void ValidateOptions_WhenSectionsDoNotOverlap_ReturnsNoErrors()
    {
        var options = CreateOptions(sampleStart: 1, sampleLength: 4, suffixStart: 5, suffixLength: 2);

        var errors = ValidateOptions(options);

        errors.Should().BeEmpty();
    }

    private static List<FailedSettingImportError> ValidateOptions(Dictionary<string, Option> options)
    {
        return SettingsCommandHelper.ValidateOptions(options, [], [new NonOverlappingRangesSettingsValidator()]);
    }

    private static Dictionary<string, Option> CreateOptions(
        bool extractSampleId = true,
        bool sameSampleId = true,
        bool extractSuffix = true,
        int sampleStart = 1,
        int sampleLength = 1,
        int suffixStart = 2,
        int suffixLength = 1)
    {
        var options = new[]
        {
            CreateBool(CreateOptionKey(nameof(SpecialSampleHandlingSettings.ExtractSampleIdFromBarcode)), extractSampleId),
            CreateInteger(CreateOptionKey(nameof(SpecialSampleHandlingSettings.SampleIdStartPosition)), sampleStart),
            CreateInteger(CreateOptionKey(nameof(SpecialSampleHandlingSettings.SampleIdLength)), sampleLength),
            CreateBool(CreateOptionKey(nameof(SpecialSampleHandlingSettings.SampleTubesWithSameSampleId)), sameSampleId),
            CreateBool(CreateOptionKey(nameof(SpecialSampleHandlingSettings.ExtractSampleIdSuffixFromBarcode)), extractSuffix),
            CreateInteger(CreateOptionKey(nameof(SpecialSampleHandlingSettings.SampleIdSuffixStartPosition)), suffixStart),
            CreateInteger(CreateOptionKey(nameof(SpecialSampleHandlingSettings.SampleIdSuffixLength)), suffixLength)
        };

        var result = options.ToDictionary(option => option.Key, StringComparer.Ordinal);
        var validation = CreateNonOverlappingRangesValidation();
        foreach (var key in validation.RangeKeys)
        {
            result[key].NonOverlappingRanges.Add(validation);
        }

        return result;
    }

    private static NonOverlappingRangesValidation CreateNonOverlappingRangesValidation()
    {
        return new NonOverlappingRangesValidation
        {
            FirstStartKey = CreateOptionKey(nameof(SpecialSampleHandlingSettings.SampleIdStartPosition)),
            FirstLengthKey = CreateOptionKey(nameof(SpecialSampleHandlingSettings.SampleIdLength)),
            SecondStartKey = CreateOptionKey(nameof(SpecialSampleHandlingSettings.SampleIdSuffixStartPosition)),
            SecondLengthKey = CreateOptionKey(nameof(SpecialSampleHandlingSettings.SampleIdSuffixLength)),
            EnabledWhen =
            [
                new Dependency
                {
                    MainOptionKey = CreateOptionKey(nameof(SpecialSampleHandlingSettings.ExtractSampleIdFromBarcode)),
                    MainOptionValue = true.ToString(),
                    Type = DependencyType.Disable
                },
                new Dependency
                {
                    MainOptionKey = CreateOptionKey(nameof(SpecialSampleHandlingSettings.SampleTubesWithSameSampleId)),
                    MainOptionValue = true.ToString(),
                    Type = DependencyType.Disable
                },
                new Dependency
                {
                    MainOptionKey = CreateOptionKey(nameof(SpecialSampleHandlingSettings.ExtractSampleIdSuffixFromBarcode)),
                    MainOptionValue = true.ToString(),
                    Type = DependencyType.Disable
                }
            ]
        };
    }

    private static string CreateOptionKey(string optionName)
    {
        return string.Join(
            PageHelper.SettingsKeyDeLimiter,
            SettingsServiceCollectionExtensions.GetConfigSectionName(typeof(SamplesSettings)),
            nameof(SamplesSettings.SpecialSampleHandling),
            optionName);
    }

    private static Option CreateBool(string key, bool value)
    {
        return new Option
        {
            Key = key,
            Value = value ? true.ToString() : false.ToString(),
            Type = OptionType.Boolean,
            DefaultValue = false.ToString()
        };
    }

    private static Option CreateInteger(string key, int value)
    {
        return new Option
        {
            Key = key,
            Value = value.ToString(CultureInfo.InvariantCulture),
            Type = OptionType.Integer,
            MinValue = "1",
            MaxValue = int.MaxValue.ToString(CultureInfo.InvariantCulture),
            DefaultValue = "1"
        };
    }
}
