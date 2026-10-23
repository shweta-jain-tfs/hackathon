using FluentValidation;

namespace Pharos.Server.Parameters.Application.Commands.MethodQualityControls;

internal class UpdatePhadiaQualityControlCommandValidator : AbstractValidator<UpdatePhadiaQualityControlCommand>
{
    public UpdatePhadiaQualityControlCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);

        RuleForEach(x => x.Tests).SetValidator(new PhadiaQualityControlUpdateTestValidator());
    }
}