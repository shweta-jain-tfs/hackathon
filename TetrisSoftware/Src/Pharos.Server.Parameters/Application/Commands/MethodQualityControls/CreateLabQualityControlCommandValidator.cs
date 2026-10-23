using FluentValidation;
using Pharos.Server.Domain;

namespace Pharos.Server.Parameters.Application.Commands.MethodQualityControls;

internal class CreateLabQualityControlCommandValidator : AbstractValidator<CreateLabQualityControlCommand>
{
    public CreateLabQualityControlCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(Constants.QualityControlNameLength);

        RuleFor(x => x.IdOnBarcode)
            .NotEmpty()
            .MaximumLength(Constants.QualityControlIdOnBarcodeLength);

        RuleFor(x => x.Tests)
            .NotEmpty()
            .Must(tests => tests.Select(t => t.TestId).Distinct().Count() == tests.Count);

        RuleForEach(x => x.Tests).SetValidator(new LabQualityControlCreateTestValidator());
    }
}