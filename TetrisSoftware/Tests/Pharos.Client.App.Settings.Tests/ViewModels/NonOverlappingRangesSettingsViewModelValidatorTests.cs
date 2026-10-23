using Microsoft.Extensions.Localization;
using Pharos.Client.App.Settings.Models;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.Models;

namespace Pharos.Client.App.Settings.ViewModels;

public class NonOverlappingRangesSettingsViewModelValidatorTests
{
    private readonly Mock<IStringLocalizer<Translations>> _mockLocalizer = new();
    private readonly NonOverlappingRangesSettingsViewModelValidator _sut = new();

    [Trait("ProductRequirement", "PR_1919")]
    [Trait("ModuleRequirement", "MR_3926")]
    [Fact]
    public void Validate_WhenRangesOverlap_ReturnsSectionsOverlapErrorsForRangeOptions()
    {
        var options = CreateOptions(sampleStart: 1, sampleLength: 4, suffixStart: 4, suffixLength: 2);

        var errors = _sut.Validate(options).ToList();

        errors.Should().HaveCount(4);
        errors.Should().OnlyContain(error => error.Error == SettingUpdateError.SectionsOverlap);
        errors.Select(error => error.Key).Should().BeEquivalentTo(
            CreateOptionKey("SampleIdStartPosition"),
            CreateOptionKey("SampleIdLength"),
            CreateOptionKey("SampleIdSuffixStartPosition"),
            CreateOptionKey("SampleIdSuffixLength"));
    }

    [Trait("ProductRequirement", "PR_1919")]
    [Trait("ModuleRequirement", "MR_3926")]
    [Fact]
    public void Validate_WhenDependencyIsDisabled_ReturnsNoErrors()
    {
        var options = CreateOptions(extractSuffix: false, sampleStart: 1, sampleLength: 4, suffixStart: 4, suffixLength: 2);

        var errors = _sut.Validate(options);

        errors.Should().BeEmpty();
    }

    [Trait("ProductRequirement", "PR_1919")]
    [Trait("ModuleRequirement", "MR_3926")]
    [Fact]
    public void Validate_WhenRangeValueIsInvalid_ReturnsNoOverlapErrors()
    {
        var options = CreateOptions(sampleStart: 0, sampleLength: 4, suffixStart: 1, suffixLength: 2);

        var errors = _sut.Validate(options);

        errors.Should().BeEmpty();
    }

    [Trait("ProductRequirement", "PR_1919")]
    [Trait("ModuleRequirement", "MR_3926")]
    [Fact]
    public void Validate_WhenSameRuleIsAttachedToMultipleOptions_ReturnsErrorsOnce()
    {
        var options = CreateOptions(sampleStart: 1, sampleLength: 4, suffixStart: 4, suffixLength: 2);

        var errors = _sut.Validate(options).ToList();

        errors.Should().HaveCount(4);
        errors.Select(error => error.Key).Should().OnlyHaveUniqueItems();
    }

    private Dictionary<string, OptionViewModelBase> CreateOptions(
        bool extractSampleId = true,
        bool sameSampleId = true,
        bool extractSuffix = true,
        int sampleStart = 1,
        int sampleLength = 1,
        int suffixStart = 2,
        int suffixLength = 1)
    {
        var options = new OptionViewModelBase[]
        {
            CreateBoolOption("ExtractSampleIdFromBarcode", extractSampleId),
            CreateIntegerOption("SampleIdStartPosition", sampleStart),
            CreateIntegerOption("SampleIdLength", sampleLength),
            CreateBoolOption("SampleTubesWithSameSampleId", sameSampleId),
            CreateBoolOption("ExtractSampleIdSuffixFromBarcode", extractSuffix),
            CreateIntegerOption("SampleIdSuffixStartPosition", suffixStart),
            CreateIntegerOption("SampleIdSuffixLength", suffixLength)
        }.ToDictionary(option => option.Key!, StringComparer.Ordinal);

        var validation = CreateNonOverlappingRangesValidation();
        foreach (var key in GetRangeKeys(validation))
        {
            var model = (Option)options[key].Model!;
            model.NonOverlappingRanges ??= [];
            model.NonOverlappingRanges.Add(validation);
        }

        return options;
    }

    private static NonOverlappingRangesValidation CreateNonOverlappingRangesValidation()
    {
        return new NonOverlappingRangesValidation
        {
            FirstStartKey = CreateOptionKey("SampleIdStartPosition"),
            FirstLengthKey = CreateOptionKey("SampleIdLength"),
            SecondStartKey = CreateOptionKey("SampleIdSuffixStartPosition"),
            SecondLengthKey = CreateOptionKey("SampleIdSuffixLength"),
            Error = nameof(SettingUpdateError.SectionsOverlap),
            EnabledWhen =
            [
                new Dependency
                {
                    MainOptionKey = CreateOptionKey("ExtractSampleIdFromBarcode"),
                    MainOptionValue = true.ToString(),
                    Type = DependencyType.Disable
                },
                new Dependency
                {
                    MainOptionKey = CreateOptionKey("SampleTubesWithSameSampleId"),
                    MainOptionValue = true.ToString(),
                    Type = DependencyType.Disable
                },
                new Dependency
                {
                    MainOptionKey = CreateOptionKey("ExtractSampleIdSuffixFromBarcode"),
                    MainOptionValue = true.ToString(),
                    Type = DependencyType.Disable
                }
            ]
        };
    }

    private static string[] GetRangeKeys(NonOverlappingRangesValidation validation)
    {
        return
        [
            validation.FirstStartKey,
            validation.FirstLengthKey,
            validation.SecondStartKey,
            validation.SecondLengthKey
        ];
    }

    private BoolViewModel CreateBoolOption(string keySuffix, bool value)
    {
        return new BoolViewModel(_mockLocalizer.Object)
        {
            Model = new Option
            {
                Key = CreateOptionKey(keySuffix),
                Type = OptionType.Boolean
            },
            Value = value
        };
    }

    private IntegerViewModel CreateIntegerOption(string keySuffix, int value)
    {
        return new IntegerViewModel(_mockLocalizer.Object, Mock.Of<ISettingsDataService<SettingItemBase>>())
        {
            Model = new Option
            {
                Key = CreateOptionKey(keySuffix),
                Type = OptionType.Integer
            },
            Value = value
        };
    }

    private static string CreateOptionKey(string keySuffix)
    {
        return $"Samples:SpecialSampleHandling:{keySuffix}";
    }
}
