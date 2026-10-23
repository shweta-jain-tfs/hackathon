using System.Globalization;
using FluentValidation;
using Microsoft.Extensions.Localization;
using Pharos.Client.Common.Localization;

namespace Pharos.Client.App.Settings.ViewModels;
public class DoubleViewModel(IStringLocalizer<Translations> loc) : OptionViewModelBase(loc: loc, validator: new DoubleViewModelValidator(loc))
{
    public double MaxDouble { get; set; } = double.MaxValue;
    public double MinDouble { get; set; } = double.MinValue;
    public string UnitResourceName { get; set; } = string.Empty;

    public double Value
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
        Value = double.Parse(DefaultValue, CultureInfo.InvariantCulture);
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
/// Validator for double-value options.
/// This validator extends the general validations provided by OptionViewModelBaseValidator.
/// </summary>
public class DoubleViewModelValidator : OptionViewModelBaseValidator
{
    public DoubleViewModelValidator(IStringLocalizer<Translations> loc) : base(loc)
    {
        RuleSet(nameof(DoubleViewModel.Value), () =>
        {
            RuleFor(p => ((DoubleViewModel)p).Value)
                .LessThanOrEqualTo(p => ((DoubleViewModel)p).MaxDouble)
                .WithMessage(p => loc["Validation_Settings_ValueLessThanOrEqual", p.Label, ((DoubleViewModel)p).MaxDouble].Value);
            RuleFor(p => ((DoubleViewModel)p).Value)
                .GreaterThanOrEqualTo(p => ((DoubleViewModel)p).MinDouble)
                .WithMessage(p => loc["Validation_Settings_ValueGreaterThanOrEqual", p.Label, ((DoubleViewModel)p).MinDouble].Value);
        });
    }
}