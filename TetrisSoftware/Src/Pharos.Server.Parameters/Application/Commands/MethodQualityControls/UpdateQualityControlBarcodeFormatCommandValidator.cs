using FluentValidation;
using Pharos.Server.Domain;

namespace Pharos.Server.Parameters.Application.Commands.MethodQualityControls;

public class UpdateQualityControlBarcodeFormatCommandValidator : AbstractValidator<UpdateQualityControlBarcodeFormatCommand>
{
    public UpdateQualityControlBarcodeFormatCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(Constants.QualityControlBarcodeFormatNameLength);

        RuleFor(x => x.TotalLength)
            .GreaterThan(0)
            .LessThanOrEqualTo(32)
            .Must((item, _) => item.QcStartPosition - 1 + item.QcLength <= item.TotalLength);

        RuleFor(x => x.QcStartPosition)
            .GreaterThan(0)
            .LessThanOrEqualTo(32)
            .Must((item, _) => item.QcStartPosition - 1 + item.QcLength <= item.TotalLength);

        RuleFor(x => x.QcLength)
            .GreaterThan(0)
            .LessThanOrEqualTo(32)
            .Must((item, _) => item.QcStartPosition - 1 + item.QcLength <= item.TotalLength);

        When(x => x.ContainsLotInformation, () =>
        {
            RuleFor(x => x.TotalLength)
                .Must((item, _) => item.LotStartPosition - 1 + item.LotLength <= item.TotalLength);

            RuleFor(x => x.QcStartPosition)
                .Must((item, _) => !HasOverlap(item));

            RuleFor(x => x.QcLength)
                .Must((item, _) => !HasOverlap(item));

            RuleFor(x => x.LotStartPosition)
                .GreaterThan(0)
                .LessThanOrEqualTo(32)
                .Must((item, _) => item.LotStartPosition - 1 + item.LotLength <= item.TotalLength)
                .Must((item, _) => !HasOverlap(item));

            RuleFor(x => x.LotLength)
                .GreaterThan(0)
                .LessThanOrEqualTo(32)
                .Must((item, _) => item.LotStartPosition - 1 + item.LotLength <= item.TotalLength)
                .Must((item, _) => !HasOverlap(item));
        });
    }

    private static bool HasOverlap(UpdateQualityControlBarcodeFormatCommand item)
    {
        var qcStart = item.QcStartPosition - 1;
        var qcEnd = qcStart + item.QcLength;
        var lotStart = item.LotStartPosition - 1;
        var lotEnd = lotStart + item.LotLength;

        return !(qcEnd <= lotStart || lotEnd <= qcStart);
    }
}
