using System.ComponentModel.DataAnnotations;
using Pharos.Server.Application.Settings.Sample;
using Pharos.Server.Settings.Infrastructure.ConfigurationProvider;

namespace Pharos.Server.Settings.Application.Settings;

public class TubeBarcodesSettingsTests
{
    private static readonly string[] _prefixPropertyNames =
    [
        nameof(TubeBarcodesSettings.Prefix1),
        nameof(TubeBarcodesSettings.Prefix2),
        nameof(TubeBarcodesSettings.Prefix3),
        nameof(TubeBarcodesSettings.Prefix4),
        nameof(TubeBarcodesSettings.Prefix5)
    ];

    public static TheoryData<string> PrefixProperties => new()
    {
        _prefixPropertyNames[0],
        _prefixPropertyNames[1],
        _prefixPropertyNames[2],
        _prefixPropertyNames[3],
        _prefixPropertyNames[4]
    };

    public static TheoryData<string, string> PrefixesWithDisallowedCharacters
    {
        get
        {
            var data = new TheoryData<string, string>();
            foreach (var propertyName in _prefixPropertyNames)
            {
                data.Add(propertyName, "A&B");
                data.Add(propertyName, @"A\B");
                data.Add(propertyName, "A^B");
                data.Add(propertyName, "A|B");
            }

            return data;
        }
    }

    [Fact]
    [Trait("ProductRequirement", "PR_2803")]
    public void Constructor_ShouldInitializeAllPrefixesToEmptyStrings()
    {
        var settings = new TubeBarcodesSettings();

        settings.Prefix1.Should().BeEmpty();
        settings.Prefix2.Should().BeEmpty();
        settings.Prefix3.Should().BeEmpty();
        settings.Prefix4.Should().BeEmpty();
        settings.Prefix5.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(PrefixProperties))]
    [Trait("ProductRequirement", "PR_2803")]
    public void Validation_WhenPrefixHasTenCharacters_ShouldBeValid(string propertyName)
    {
        var settings = CreateSettingsWithPrefix(propertyName, new string('A', 10));

        var validationResults = Validate(settings);

        validationResults.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(PrefixProperties))]
    [Trait("ProductRequirement", "PR_2803")]
    public void Validation_WhenPrefixExceedsTenCharacters_ShouldBeInvalid(string propertyName)
    {
        var settings = CreateSettingsWithPrefix(propertyName, new string('A', 11));

        var validationResults = Validate(settings);

        validationResults.Should().ContainSingle(result => result.MemberNames.Contains(propertyName));
    }

    [Theory]
    [MemberData(nameof(PrefixesWithDisallowedCharacters))]
    [Trait("ProductRequirement", "PR_2803")]
    public void SettingsValidation_WhenPrefixContainsDisallowedCharacter_ShouldBeInvalid(string propertyName, string value)
    {
        var property = typeof(TubeBarcodesSettings).GetProperty(propertyName);

        var isValid = ValueValidator.IsValidValue(property, value);

        isValid.Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(PrefixProperties))]
    [Trait("ProductRequirement", "PR_2803")]
    public void SettingsValidation_WhenPrefixContainsOtherCharacters_ShouldBeValid(string propertyName)
    {
        var property = typeof(TubeBarcodesSettings).GetProperty(propertyName);

        var isValid = ValueValidator.IsValidValue(property, "A~\u00E4-09");

        isValid.Should().BeTrue();
    }

    private static TubeBarcodesSettings CreateSettingsWithPrefix(string propertyName, string value)
    {
        var settings = new TubeBarcodesSettings();
        typeof(TubeBarcodesSettings).GetProperty(propertyName)!.SetValue(settings, value);
        return settings;
    }

    private static List<ValidationResult> Validate(TubeBarcodesSettings settings)
    {
        var validationResults = new List<ValidationResult>();
        Validator.TryValidateObject(settings, new ValidationContext(settings), validationResults, validateAllProperties: true);
        return validationResults;
    }
}
