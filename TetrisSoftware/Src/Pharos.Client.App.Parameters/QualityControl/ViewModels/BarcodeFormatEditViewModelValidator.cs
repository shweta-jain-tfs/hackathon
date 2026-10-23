using FluentValidation;

namespace Pharos.Client.App.Parameters.QualityControl.ViewModels;

public class BarcodeFormatEditViewModelValidator : AbstractValidator<BarcodeFormatEditViewModel>
{
    public BarcodeFormatEditViewModelValidator(IStringLocalizer<Translations> loc)
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage(loc["BarcodeFormatValidator_NameRequired"])
            .MaximumLength(15)
            .WithMessage(string.Format(loc["BarcodeFormatValidator_NameMaxLength"], 15));

        RuleFor(x => x.TotalLength)
            .GreaterThan(0)
            .WithMessage(loc["BarcodeFormatValidator_TotalLengthRequired"])
            .LessThanOrEqualTo(32)
            .WithMessage(string.Format(loc["BarcodeFormatValidator_MaxLength"], 32))
            .Must((item, _) => item.QcStartPosition - 1 + item.QcLength <= item.TotalLength)
            .WithMessage(loc["BarcodeFormatValidator_QcExceedsTotalLength"]);

        RuleFor(x => x.QcStartPosition)
            .GreaterThan(0)
            .WithMessage(loc["BarcodeFormatValidator_QcStartPositionInvalid"])
            .LessThanOrEqualTo(32)
            .WithMessage(string.Format(loc["BarcodeFormatValidator_MaxLength"], 32))
            .Must((item, _) => item.QcStartPosition - 1 + item.QcLength <= item.TotalLength)
            .WithMessage(loc["BarcodeFormatValidator_QcExceedsTotalLength"]);

        RuleFor(x => x.QcLength)
            .GreaterThan(0)
            .WithMessage(loc["BarcodeFormatValidator_QcLengthRequired"])
            .LessThanOrEqualTo(32)
            .WithMessage(string.Format(loc["BarcodeFormatValidator_MaxLength"], 32))
            .Must((item, _) => item.QcStartPosition - 1 + item.QcLength <= item.TotalLength)
            .WithMessage(loc["BarcodeFormatValidator_QcExceedsTotalLength"]);

        When(x => x.ContainsLotInformation, () =>
        {
            RuleFor(x => x.TotalLength)
                .Must((item, _) => item.LotStartPosition - 1 + item.LotLength <= item.TotalLength)
                .WithMessage(loc["BarcodeFormatValidator_LotExceedsTotalLength"]);

            RuleFor(x => x.QcStartPosition)
                .Must((item, _) => !HasOverlap(item))
                .WithMessage(loc["BarcodeFormatValidator_SectionsOverlap"]);

            RuleFor(x => x.QcLength)
                .Must((item, _) => !HasOverlap(item))
                .WithMessage(loc["BarcodeFormatValidator_SectionsOverlap"]);

            RuleFor(x => x.LotStartPosition)
                .GreaterThan(0)
                .WithMessage(loc["BarcodeFormatValidator_LotStartPositionInvalid"])
                .LessThanOrEqualTo(32)
                .WithMessage(string.Format(loc["BarcodeFormatValidator_MaxLength"], 32))
                .Must((item, _) => item.LotStartPosition - 1 + item.LotLength <= item.TotalLength)
                .WithMessage(loc["BarcodeFormatValidator_LotExceedsTotalLength"])
                .Must((item, _) => !HasOverlap(item))
                .WithMessage(loc["BarcodeFormatValidator_SectionsOverlap"]);

            RuleFor(x => x.LotLength)
                .GreaterThan(0)
                .WithMessage(loc["BarcodeFormatValidator_LotLengthRequired"])
                .LessThanOrEqualTo(32)
                .WithMessage(string.Format(loc["BarcodeFormatValidator_MaxLength"], 32))
                .Must((item, _) => item.LotStartPosition - 1 + item.LotLength <= item.TotalLength)
                .WithMessage(loc["BarcodeFormatValidator_LotExceedsTotalLength"])
                .Must((item, _) => !HasOverlap(item))
                .WithMessage(loc["BarcodeFormatValidator_SectionsOverlap"]);
        });
    }

    private static bool HasOverlap(BarcodeFormatEditViewModel item)
    {
        var qcStart = item.QcStartPosition - 1;
        var qcEnd = qcStart + item.QcLength;
        var lotStart = item.LotStartPosition - 1;
        var lotEnd = lotStart + item.LotLength;

        return !(qcEnd <= lotStart || lotEnd <= qcStart);
    }
}