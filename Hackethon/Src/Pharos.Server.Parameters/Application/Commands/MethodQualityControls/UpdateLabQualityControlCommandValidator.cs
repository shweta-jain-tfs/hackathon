using FluentValidation;
using Pharos.Server.Domain;

namespace Pharos.Server.Parameters.Application.Commands.MethodQualityControls;

internal class UpdateLabQualityControlCommandValidator : AbstractValidator<UpdateLabQualityControlCommand>
{
    public UpdateLabQualityControlCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);

        RuleFor(x => x.IdOnBarcode)
            .NotEmpty()
            .MaximumLength(Constants.QualityControlIdOnBarcodeLength);

        RuleFor(x => x.Tests)
            .NotEmpty()
            .Must(tests => tests.Select(t => t.TestId).Distinct().Count() == tests.Count)
            .WithMessage("Duplicate tests are not allowed.");

        RuleForEach(x => x.Tests).SetValidator(new LabQualityControlUpdateTestValidator());
    }
}