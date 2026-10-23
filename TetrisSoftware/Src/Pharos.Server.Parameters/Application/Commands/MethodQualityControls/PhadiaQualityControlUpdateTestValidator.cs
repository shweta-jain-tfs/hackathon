using FluentValidation;

namespace Pharos.Server.Parameters.Application.Commands.MethodQualityControls;

internal class PhadiaQualityControlUpdateTestValidator : AbstractValidator<PhadiaQualityControlUpdateTest>
{
    public PhadiaQualityControlUpdateTestValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);

        RuleFor(x => x.Replicates)
            .InclusiveBetween(1, 5);

        RuleFor(x => x.ReplicateCvLimit)
            .InclusiveBetween(0, 50)
            .Must(HasAtMostOneDecimal);

        RuleFor(x => x.QualityControlType).IsInEnum();

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
        });

        When(x => x.UseLotQualification, () =>
        {
            RuleFor(x => x.NumberOfTests)
                .InclusiveBetween(5, 50);
        });
    }

    private static bool HasAtMostOneDecimal(double value)
    {
        return Math.Abs(Math.Round(value, 1) - value) < 0.001;
    }
}