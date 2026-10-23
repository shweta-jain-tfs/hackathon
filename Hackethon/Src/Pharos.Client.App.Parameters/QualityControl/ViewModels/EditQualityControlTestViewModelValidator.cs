using System.Globalization;
using FluentValidation;

namespace Pharos.Client.App.Parameters.QualityControl.ViewModels;

/// <summary>
/// Validator for <see cref="EditQualityControlTestViewModel"/>.
/// This is picked up automatically by the fluent validation validator
/// inside <see cref="Client.Common.App.Validation.PhSimpleValidationContext"/>
/// </summary>
public class EditQualityControlTestViewModelValidator : AbstractValidator<EditQualityControlTestViewModel>
{
    public EditQualityControlTestViewModelValidator(IStringLocalizer<Translations> loc)
    {
        RuleFor(x => x.Replicates)
            .InclusiveBetween(1, 5)
            .WithMessage(string.Format(CultureInfo.InvariantCulture, loc["EditQualityControlTestView_ValidationError_Range"], 1, 5));

        RuleFor(x => x.ReplicateCvLimit)
            .InclusiveBetween(0, 50)
            .WithMessage(string.Format(CultureInfo.InvariantCulture, loc["EditQualityControlTestView_ValidationError_Range"], 0, 50))
            .Must(HasAtMostOneDecimal)
            .WithMessage(loc["EditQualityControlTestView_ValidationError_OneDecimal"]);

        RuleFor(x => x.PreDilutionFactor)
            .InclusiveBetween(1, 1000)
            .WithMessage(string.Format(CultureInfo.InvariantCulture, loc["EditQualityControlTestView_ValidationError_Range"], 1, 1000));

        When(x => !x.EditingPhadiaControl, () =>
        {
            RuleFor(x => x.InstrumentDilutionFactor)
                .Must((model, i) => model.InstrumentDilutionFactors.Select(x => x.Value).Contains(i))
                .WithMessage(loc["EditQualityControlTestView_ValidationError_SelectAValue"]);
        });

        When(x => x.ApplyAcceptanceRange, () =>
        {
            When(x => !x.UseAcceptanceRange, () =>
            {
                RuleFor(x => x.MaxCv)
                    .InclusiveBetween(1, 100)
                    .WithMessage(string.Format(CultureInfo.InvariantCulture, loc["EditQualityControlTestView_ValidationError_Range"], 1, 100))
                    .Must(HasAtMostOneDecimal)
                    .WithMessage(loc["EditQualityControlTestView_ValidationError_OneDecimal"]);

                RuleFor(x => x.NumberOfSd)
                    .InclusiveBetween(1, 3)
                    .WithMessage(loc["EditQualityControlTestView_ValidationError_SelectAValue"]);
            });

            When(x => x.UseAcceptanceRange && !x.EditingPhadiaControl, () =>
            {
                RuleFor(x => x.LowerLimit)
                    .InclusiveBetween(0, 10000)
                    .WithMessage(string.Format(CultureInfo.InvariantCulture, loc["EditQualityControlTestView_ValidationError_Range"], 0, 10000))
                    .Must(x => HasAtMostOneDecimal(x!.Value))
                    .When(x => x.LowerLimit.HasValue)
                    .WithMessage(loc["EditQualityControlTestView_ValidationError_OneDecimal"]);

                RuleFor(x => x.LowerLimit)
                    .LessThan(x => x.UpperLimit)
                    .When(x => x.LowerLimit.HasValue && x.UpperLimit.HasValue)
                    .WithMessage(loc["EditQualityControlTestView_ValidationError_LessThanUpperLimit"]);

                RuleFor(x => x.UpperLimit)
                    .InclusiveBetween(0, 10000)
                    .WithMessage(string.Format(CultureInfo.InvariantCulture, loc["EditQualityControlTestView_ValidationError_Range"], 0, 10000))
                    .Must(x => HasAtMostOneDecimal(x!.Value))
                    .When(x => x.UpperLimit.HasValue)
                    .WithMessage(loc["EditQualityControlTestView_ValidationError_OneDecimal"]);

                RuleFor(x => x.UpperLimit)
                    .GreaterThan(x => x.LowerLimit)
                    .When(x => x.UpperLimit.HasValue && x.LowerLimit.HasValue)
                    .WithMessage(loc["EditQualityControlTestView_ValidationError_MoreThanLowerLimit"]);
            });
        });

        When(x => x.UseLotQualification, () =>
        {
            RuleFor(x => x.NumberOfTests)
                .InclusiveBetween(5, 50)
                .WithMessage(string.Format(CultureInfo.InvariantCulture, loc["EditQualityControlTestView_ValidationError_Range"], 5, 50));
        });
    }

    private static bool HasAtMostOneDecimal(double value)
    {
        return Math.Abs(Math.Round(value, 1) - value) < 0.001;
    }
}