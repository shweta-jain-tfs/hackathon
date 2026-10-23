using Pharos.Server.Settings.Application.Metadata;
using Pharos.Server.Settings.Domain;

namespace Pharos.Server.Settings.Infrastructure.ConfigurationProvider;

public class SettingsImportValidatorTests
{
    [Theory]
    [InlineData(OptionType.Integer, false, false, true, SettingImportError.None)]
    [InlineData(OptionType.Integer, false, false, false, SettingImportError.InvalidValue)]
    [InlineData(OptionType.Double, false, false, true, SettingImportError.None)]
    [InlineData(OptionType.Double, false, false, false, SettingImportError.InvalidValue)]
    [InlineData(OptionType.String, false, false, true, SettingImportError.None)]
    [InlineData(OptionType.String, false, false, false, SettingImportError.InvalidValue)]
    [InlineData(OptionType.String, true, false, true, SettingImportError.None)]
    [InlineData(OptionType.String, true, false, false, SettingImportError.RequiredSettingNotDefined)]
    [InlineData(OptionType.String, false, true, true, SettingImportError.None)]
    [InlineData(OptionType.String, false, true, false, SettingImportError.DuplicateValue)]
    [Trait("ModuleRequirement", "MR_5121")]
    public void ValidateOption_ShouldReturnError_WhenNotValid(OptionType optionType, bool isRequired, bool isUnique, bool isExpectedValid, SettingImportError expectedError)
    {
        const string prefix = "Lis:LisChannels";
        var (option, allSettings) = GenerateTestOption(prefix, 0, 0, optionType, isRequired, isUnique, isExpectedValid);

        var error = SettingsImportValidator.ValidateOption(option, allSettings);

        Assert.Equal(expectedError, error);
    }

    [Fact]
    public void ValidateOption_Should_not_return_required_error_when_required_validation_is_disabled()
    {
        var option = new Option
        {
            Key = "DependentRequiredSetting",
            Value = string.Empty,
            Required = true,
            Type = OptionType.String,
            MaxLength = 20
        };

        var error = SettingsImportValidator.ValidateOption(option, [], validateRequired: false);

        Assert.Equal(SettingImportError.None, error);
    }

    [Theory]
    [InlineData("valid value", SettingImportError.None)]
    [InlineData("invalid<value", SettingImportError.InvalidValue)]
    [InlineData("invalid>value", SettingImportError.InvalidValue)]
    [InlineData("", SettingImportError.None)]
    public void ValidateOption_ShouldValidateDisallowedCharacters(string value, SettingImportError expectedError)
    {
        var option = new Option
        {
            Key = "StringSetting",
            Value = value,
            Type = OptionType.String,
            MaxLength = 20,
            DisallowedCharactersPattern = @"[<>]"
        };

        var error = SettingsImportValidator.ValidateOption(option, []);

        error.Should().Be(expectedError);
    }

    // Generate test option:
    // prefix: string preceding the numeric id, for ex: "Lis:LisChannels"
    // index: index of setting to generate, for example "Lis:LisChannels:0:*"
    // attributeIndex: used to dynamically generate the fake attribute, for example Setting_0
    private static (Option option, List<Setting>? allSettings) GenerateTestOption(string prefix, int index, int attributeIndex, OptionType optionType, bool isRequired, bool isUnique, bool isValid)
    {
        string value;
        string maxValue;
        string minValue;
        string defaultValue;
        int maxLength;
        switch (optionType)
        {
            case OptionType.Integer:
                value = isValid ? "10" : "100";
                maxValue = "20";
                minValue = "0";
                defaultValue = string.Empty;
                maxLength = 0;
                break;
            case OptionType.Double:
                value = isValid ? "15.3" : "3";
                maxValue = "20";
                minValue = "10";
                defaultValue = string.Empty;
                maxLength = 0;
                break;
            case OptionType.String:
            default:
                if (isValid)
                {
                    value = "TestValue";
                    defaultValue = string.Empty;
                }
                else if (isRequired)
                {
                    value = "";
                    defaultValue = "DefaultValue";
                }
                else if (isUnique)
                {
                    value = "UniqueValue";
                    defaultValue = string.Empty;
                }
                else
                {
                    value = "TooLongValue1234567890";
                    defaultValue = string.Empty;
                }
                maxLength = 20;
                maxValue = string.Empty;
                minValue = string.Empty;
                break;
        }
        var option = new Option
        {
            Key = $"{prefix}:{index}:Setting_{attributeIndex}",
            Value = value,
            Required = isRequired,
            Unique = isUnique,
            Type = optionType,
            MaxValue = maxValue,
            MinValue = minValue,
            DefaultValue = defaultValue,
            MaxLength = maxLength
        };
        var allSettings = isUnique
            ? GenerateTestSettings([($"{prefix}:{(index + 1000)}:Setting_{attributeIndex}", "UniqueValue")])
            : [];

        return (option, allSettings);
    }

    private static List<Setting> GenerateTestSettings((string key, string value)[] keyValues)
    {
        var allSettings = new List<Setting>();
        foreach (var (key, value) in keyValues)
        {
            var setting = new Setting
            {
                Key = key,
                Value = value,
            };
            allSettings.Add(setting);
        }
        return allSettings;
    }
}
