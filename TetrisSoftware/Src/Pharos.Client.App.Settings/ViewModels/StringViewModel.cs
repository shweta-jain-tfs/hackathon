using System.Text.RegularExpressions;
using FluentValidation;
using Microsoft.Extensions.Localization;
using Pharos.Client.Common.Localization;

namespace Pharos.Client.App.Settings.ViewModels;
public class StringViewModel(IStringLocalizer<Translations> loc, ISettingsDataService<SettingItemBase> dataService) : OptionViewModelBase(loc: loc, validator: new StringViewModelValidator(loc, dataService))
{
    public string Value
    {
        get;
        set
        {
            field = value.Trim();
            IsModified = ValueAsString != OriginalValue;

            OnPropertyChanged(nameof(Value));
        }
    } = string.Empty;

    public int MaxLength { get; set; } = int.MaxValue;

    public string? DisallowedCharactersPattern { get; set; }

    public int? InputMaxLength => MaxLength is > 0 and < int.MaxValue
        ? MaxLength
        : null;

    public override bool ResetToDefault()
    {
        Value = DefaultValue;
        UpdateModelValue();
        return true;
    }

    protected override bool MatchesDependencyValue(string? expectedValue)
    {
        return Value == expectedValue;
    }

    public override string ValueAsString => Value;

    /// <summary>
    /// Validator for string-value options.
    /// This validator extends the general validations provided by OptionViewModelBaseValidator.
    /// </summary>
    public class StringViewModelValidator : OptionViewModelBaseValidator
    {
        public StringViewModelValidator(IStringLocalizer<Translations> loc, ISettingsDataService<SettingItemBase> dataService) : base(loc, dataService)
        {
            RuleSet(nameof(StringViewModel.Value), () =>
            {
                //Validate value is less or equal to MaxLength
                RuleFor(p => ((StringViewModel)p).Value.Length)
                    .LessThanOrEqualTo(p => ((StringViewModel)p).MaxLength)
                    .WithMessage(p => loc["Validation_Settings_LengthLessThanOrEqual", p.Label, ((StringViewModel)p).MaxLength].Value);

                RuleFor(p => ((StringViewModel)p).Value)
                    .Must((p, value) =>
                    {
                        var pattern = ((StringViewModel)p).DisallowedCharactersPattern;
                        return string.IsNullOrEmpty(value)
                            || string.IsNullOrEmpty(pattern)
                            || !Regex.IsMatch(value, pattern);
                    })
                    .WithMessage(p => loc["Validation_Settings_DisallowedCharacters", p.Label].Value);
            });
        }
    }
}
