using FluentValidation;

namespace Pharos.Server.Parameters.Application.Commands.MethodQualityControls;

internal class LabQualityControlCreateTestValidator : AbstractValidator<LabQualityControlCreateTest>
{
    public LabQualityControlCreateTestValidator()
    {
        RuleFor(x => x.Replicates)
            .InclusiveBetween(1, 5);

        RuleFor(x => x.ReplicateCvLimit)
            .InclusiveBetween(0, 50)
            .Must(HasAtMostOneDecimal);

        RuleFor(x => x.QualityControlType).IsInEnum();

        RuleFor(x => x.PreDilutionFactor)
            .InclusiveBetween(1, 1000);

        When(x => x.ApplyAcceptanceRange, () =>
        {
            When(x => !x.UseAcceptanceRange, () =>
            {
                RuleFor(x => x.MaxCv)
                    .InclusiveBetween(1, 100)
                    .Must(HasAtMostOneDecimal);

                RuleFor(x => x.NumberOfSd)
                    .InclusiveBetween(1, 3);
            });

            When(x => x.UseAcceptanceRange, () =>
            {
                RuleFor(x => x.LowerLimit)
                    .InclusiveBetween(0, 10000)
                    .Must(x => HasAtMostOneDecimal(x!.Value))
                    .When(x => x.LowerLimit.HasValue);

                RuleFor(x => x.LowerLimit)
                    .LessThan(x => x.UpperLimit)
                    .When(x => x.LowerLimit.HasValue && x.UpperLimit.HasValue);

                RuleFor(x => x.UpperLimit)
                    .InclusiveBetween(0, 10000)
                    .Must(x => HasAtMostOneDecimal(x!.Value))
                    .When(x => x.UpperLimit.HasValue);

                RuleFor(x => x.UpperLimit)
                    .GreaterThan(x => x.LowerLimit)
                    .When(x => x.UpperLimit.HasValue && x.LowerLimit.HasValue);
            });
        });

        When(x => x.UseLotQualification, () =>
        {
            RuleFor(x => x.NumberOfTests)
                .InclusiveBetween(5, 50);
        });

        RuleFor(x => x.TestId)
            .GreaterThan(0);
    }

    private static bool HasAtMostOneDecimal(double value)
    {
        return Math.Abs(Math.Round(value, 1) - value) < 0.001;
    }
}