using System.Globalization;
using FluentValidation;
using Microsoft.Extensions.Localization;
using Pharos.Client.Common.Localization;

namespace Pharos.Client.App.Settings.ViewModels;
public class IntegerViewModel(IStringLocalizer<Translations> loc, ISettingsDataService<SettingItemBase> dataService) : OptionViewModelBase(loc: loc, validator: new IntegerViewModelValidator(loc, dataService))
{
    public int MaxInt { get; set; } = int.MaxValue;
    public int MinInt { get; set; } = int.MinValue;
    public string UnitResourceName { get; set; } = string.Empty;

    public int Value
    {
        get;
        set
        {
            field = value;
            IsModified = ValueAsString != OriginalValue;

            Validate();
            OnPropertyChanged(nameof(Value));
        }
    }

    public override bool ResetToDefault()
    {
        Value = int.Parse(DefaultValue, CultureInfo.InvariantCulture);
        UpdateModelValue();
        return true;
    }
    protected override bool MatchesDependencyValue(string? expectedValue)
    {
        return ValueAsString == expectedValue;
    }

    public override string ValueAsString => Value.ToString(CultureInfo.InvariantCulture);
}

/// <summary>
/// Validator for integer-value options.
/// This validator extends the general validations provided by OptionViewModelBaseValidator.
/// </summary>
public class IntegerViewModelValidator : OptionViewModelBaseValidator
{
    public IntegerViewModelValidator(IStringLocalizer<Translations> loc, ISettingsDataService<SettingItemBase> dataService) : base(loc, dataService)
    {
        RuleSet(nameof(IntegerViewModel.Value), () =>
        {
            //Validate value is less or equal to Maximum value
            RuleFor(p => ((IntegerViewModel)p).Value)
                .LessThanOrEqualTo(p => ((IntegerViewModel)p).MaxInt)
                .WithMessage(p => loc["Validation_Settings_ValueLessThanOrEqual", p.Label, ((IntegerViewModel)p).MaxInt].Value);
            RuleFor(p => ((IntegerViewModel)p).Value)
                .GreaterThanOrEqualTo(p => ((IntegerViewModel)p).MinInt)
                .WithMessage(p => loc["Validation_Settings_ValueGreaterThanOrEqual", p.Label, ((IntegerViewModel)p).MinInt].Value);
        });
    }
}