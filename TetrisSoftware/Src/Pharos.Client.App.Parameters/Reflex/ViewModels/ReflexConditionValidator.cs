using FluentValidation;

namespace Pharos.Client.App.Parameters.Reflex.ViewModels;

/// <summary>
/// Validator for <see cref="ReflexConditionViewModel"/>.
/// This is picked up automatically by the fluent validation validator
/// inside <see cref="Client.Common.App.Validation.PhSimpleValidationContext"/>
/// </summary>
public class ReflexConditionValidator : AbstractValidator<ReflexConditionViewModel>
{
    public ReflexConditionValidator(IStringLocalizer<Translations> loc)
    {
        // Step 1: TestName is always required
        RuleFor(x => x.TestName)
            .NotEmpty()
            .WithMessage(loc["ReflexDetailPage_TestNameRequired"]);

        // Step 2: ResultType is required when on step 2 or later
        RuleFor(x => x.NullableResultType)
            .NotNull()
            .WithMessage(loc["ReflexDetailPage_ResultTypeRequired"])
            .When(x => x.CurrentStep >= 2);

        // Step 3: CompareType is required when on step 3 or later
        RuleFor(x => x.NullableCompareType)
            .NotNull()
            .WithMessage(loc["ReflexDetailPage_ComparerRequired"])
            .When(x => x.CurrentStep >= 3);

        // Step 4: Validate Concentration - single value scenarios (Above/Below)
        When(x => x.CurrentStep >= 4 && (x.CompareType == CompareType.Above || x.CompareType == CompareType.Below) && x.ResultType == ResultType.Concentration, () =>
        {
            RuleFor(x => x.ConcentrationCompareValue)
                .NotNull()
                .WithMessage(loc["ReflexDetailPage_ValueRequired"]);

            RuleFor(x => x.ConcentrationCompareValue)
                .InclusiveBetween(0, 10000)
                .WithMessage(string.Format(loc["ReflexDetailPage_ValueOutOfRange"], 0, 10000))
                .When(x => x.ConcentrationCompareValue.HasValue);
        });

        // Validate Concentration - Between scenario
        When(x => x.CurrentStep >= 4 && x.CompareType == CompareType.Between && x.ResultType == ResultType.Concentration, () =>
        {
            // Validate the tuple directly - required checks first, then comparison
            RuleFor(x => x.ConcentrationCompareRange)
                .Must(range => range.Min.HasValue)
                .WithMessage(loc["ReflexDetailPage_MinValueRequired"]);

            RuleFor(x => x.ConcentrationCompareRange)
                .Must(range => range.Max.HasValue)
                .WithMessage(loc["ReflexDetailPage_MaxValueRequired"])
                .When(x => x.ConcentrationCompareRange.Min.HasValue); // Only check max if min is present

            RuleFor(x => x.ConcentrationCompareRange)
                .Must(range => range.Max > range.Min)
                .WithMessage(loc["ReflexDetailPage_MinValueMustBeLessThanMaxValue"])
                .When(x => x.ConcentrationCompareRange.Min.HasValue && x.ConcentrationCompareRange.Max.HasValue);

            RuleFor(x => x.ConcentrationCompareRange)
                .Must(range => range.Min >= 0 && range.Min <= 10000)
                .WithMessage(string.Format(loc["ReflexDetailPage_MinValueOutOfRange"], 0, 10000))
                .When(x => x.ConcentrationCompareRange.Min.HasValue);

            RuleFor(x => x.ConcentrationCompareRange)
                .Must(range => range.Max >= 0 && range.Max <= 10000)
                .WithMessage(string.Format(loc["ReflexDetailPage_MaxValueOutOfRange"], 0, 10000))
                .When(x => x.ConcentrationCompareRange.Max.HasValue);
        });

        // Validate ConcentrationRaw - single value scenarios (Above/Below)
        When(x => x.CurrentStep >= 4 && (x.CompareType == CompareType.Above || x.CompareType == CompareType.Below) && x.ResultType == ResultType.ConcentrationRaw, () =>
        {
            RuleFor(x => x.ConcentrationRawCompareValue)
                .NotNull()
                .WithMessage(loc["ReflexDetailPage_ValueRequired"]);

            RuleFor(x => x.ConcentrationRawCompareValue)
                .InclusiveBetween(0, 10000)
                .WithMessage(string.Format(loc["ReflexDetailPage_ValueOutOfRange"], 0, 10000))
                .When(x => x.ConcentrationRawCompareValue.HasValue);
        });

        // Validate ConcentrationRaw - Between scenario
        When(x => x.CurrentStep >= 4 && x.CompareType == CompareType.Between && x.ResultType == ResultType.ConcentrationRaw, () =>
        {
            // Validate the tuple directly - required checks first, then comparison
            RuleFor(x => x.ConcentrationRawCompareRange)
                .Must(range => range.Min.HasValue)
                .WithMessage(loc["ReflexDetailPage_MinValueRequired"]);

            RuleFor(x => x.ConcentrationRawCompareRange)
                .Must(range => range.Max.HasValue)
                .WithMessage(loc["ReflexDetailPage_MaxValueRequired"])
                .When(x => x.ConcentrationRawCompareRange.Min.HasValue); // Only check max if min is present

            RuleFor(x => x.ConcentrationRawCompareRange)
                .Must(range => range.Max > range.Min)
                .WithMessage(loc["ReflexDetailPage_MinValueMustBeLessThanMaxValue"])
                .When(x => x.ConcentrationRawCompareRange.Min.HasValue && x.ConcentrationRawCompareRange.Max.HasValue);

            RuleFor(x => x.ConcentrationRawCompareRange)
                .Must(range => range.Min >= 0 && range.Min <= 10000)
                .WithMessage(string.Format(loc["ReflexDetailPage_MinValueOutOfRange"], 0, 10000))
                .When(x => x.ConcentrationRawCompareRange.Min.HasValue);

            RuleFor(x => x.ConcentrationRawCompareRange)
                .Must(range => range.Max >= 0 && range.Max <= 10000)
                .WithMessage(string.Format(loc["ReflexDetailPage_MaxValueOutOfRange"], 0, 10000))
                .When(x => x.ConcentrationRawCompareRange.Max.HasValue);
        });

        // Validate Class - value required
        When(x => x.CurrentStep >= 4 && x.CompareType == CompareType.EqualTo && x.ResultType == ResultType.Class, () =>
        {
            RuleFor(x => x.ClassCompareValue)
                .NotEmpty()
                .WithMessage(loc["ReflexDetailPage_ClassValueRequired"]);
        });

        // Validate CutOff - value required
        When(x => x.CurrentStep >= 4 && x.CompareType == CompareType.EqualTo && x.ResultType == ResultType.CutOff, () =>
        {
            RuleFor(x => x.CutOffCompareValue)
                .NotEmpty()
                .WithMessage(loc["ReflexDetailPage_CutOffValueRequired"]);
        });

        // Validate Quotient - single value scenarios (Above/Below)
        When(x => x.CurrentStep >= 4 && (x.CompareType == CompareType.Above || x.CompareType == CompareType.Below) && x.ResultType == ResultType.Quotient, () =>
        {
            RuleFor(x => x.QuotientCompareValue)
                .NotNull()
                .WithMessage(loc["ReflexDetailPage_QuotientValueRequired"]);

            RuleFor(x => x.QuotientCompareValue)
                .InclusiveBetween(0, 10000)
                .WithMessage(string.Format(loc["ReflexDetailPage_ValueOutOfRange"], 0, 10000))
                .When(x => x.QuotientCompareValue.HasValue);
        });

        // Validate Quotient - Between scenario
        When(x => x.CurrentStep >= 4 && x.CompareType == CompareType.Between && x.ResultType == ResultType.Quotient, () =>
        {
            // Validate the tuple directly - required checks first, then comparison
            RuleFor(x => x.QuotientCompareRange)
                .Must(range => range.Min.HasValue)
                .WithMessage(loc["ReflexDetailPage_MinValueRequired"]);

            RuleFor(x => x.QuotientCompareRange)
                .Must(range => range.Max.HasValue)
                .WithMessage(loc["ReflexDetailPage_MaxValueRequired"])
                .When(x => x.QuotientCompareRange.Min.HasValue); // Only check max if min is present

            RuleFor(x => x.QuotientCompareRange)
                .Must(range => range.Max > range.Min)
                .WithMessage(loc["ReflexDetailPage_MinValueMustBeLessThanMaxValue"])
                .When(x => x.QuotientCompareRange.Min.HasValue && x.QuotientCompareRange.Max.HasValue);

            RuleFor(x => x.QuotientCompareRange)
                .Must(range => range.Min >= 0 && range.Min <= 10000)
                .WithMessage(string.Format(loc["ReflexDetailPage_MinValueOutOfRange"], 0, 10000))
                .When(x => x.QuotientCompareRange.Min.HasValue);

            RuleFor(x => x.QuotientCompareRange)
                .Must(range => range.Max >= 0 && range.Max <= 10000)
                .WithMessage(string.Format(loc["ReflexDetailPage_MaxValueOutOfRange"], 0, 10000))
                .When(x => x.QuotientCompareRange.Max.HasValue);
        });

        // Validate RU - value required
        When(x => x.CurrentStep >= 4 && x.CompareType == CompareType.Below && x.ResultType == ResultType.RU, () =>
        {
            RuleFor(x => x.RuCompareValue)
                .NotNull()
                .WithMessage(loc["ReflexDetailPage_RuValueRequired"]);

            RuleFor(x => x.RuCompareValue)
                .InclusiveBetween(6, 100)
                .WithMessage(string.Format(loc["ReflexDetailPage_ValueOutOfRange"], 6, 100))
                .When(x => x.RuCompareValue.HasValue);
        });
    }
}
