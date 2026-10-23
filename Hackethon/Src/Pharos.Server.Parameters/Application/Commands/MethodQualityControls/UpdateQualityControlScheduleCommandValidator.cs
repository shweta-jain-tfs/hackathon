using FluentValidation;
using Pharos.Server.Domain.Parameters.Methods;

namespace Pharos.Server.Parameters.Application.Commands.MethodQualityControls;

public class UpdateQualityControlScheduleCommandValidator : AbstractValidator<UpdateQualityControlScheduleCommand>
{
    public UpdateQualityControlScheduleCommandValidator()
    {
        When(x => x.Periodically == Periodically.YesByNumberOfTests, () =>
        {
            RuleFor(x => x.StartAfter)
                .InclusiveBetween(0, 10000);

            RuleFor(x => x.RunEvery)
                .InclusiveBetween(1, 10000);
        });

        When(x => x.Periodically == Periodically.YesByTime, () =>
        {
            RuleFor(x => x.StartAfter)
                .InclusiveBetween(0, 1440); //24*60

            RuleFor(x => x.RunEvery)
                .InclusiveBetween(1, 1440); //24*60
        });
    }
}