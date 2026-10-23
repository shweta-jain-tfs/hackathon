using FluentValidation;

namespace Pharos.Client.App.Parameters.Methods.ViewModels;

public sealed class MethodTestDetailsReportViewModelValidator : AbstractValidator<MethodTestDetailsReportViewModel>
{
    private const int _quantitativeTextMaxLength = 7;
    private const int _cutOffConcentrationLowerLimit = 0;
    private const int _cutOffConcentrationUpperLimit = 99999;

    public MethodTestDetailsReportViewModelValidator(IStringLocalizer<Translations> loc)
    {
        RuleFor(x => x.MethodConcentrationUnitId)
            .GreaterThan(0)
            .When(x => x.ReportConcentration);

        RuleFor(x => x.CutOffConcentration)
            .InclusiveBetween(_cutOffConcentrationLowerLimit, _cutOffConcentrationUpperLimit)
            .WithMessage(string.Format(
                loc["MethodDetailsReportTab_ValidationError_Range"],
                _cutOffConcentrationLowerLimit,
                _cutOffConcentrationUpperLimit))
            .When(x => x.ReportCutOff);

        RuleFor(x => x.ReportClassId)
            .GreaterThan(0)
            .When(x => x.ReportClass);

        RuleFor(x => x.TextQuantitativeBelow)
            .MaximumLength(_quantitativeTextMaxLength)
            .WithMessage(string.Format(loc["MethodDetailsReportTab_ValidationError_MaxLength"], _quantitativeTextMaxLength));

        RuleFor(x => x.TextQuantitativeAbove)
            .MaximumLength(_quantitativeTextMaxLength)
            .WithMessage(string.Format(loc["MethodDetailsReportTab_ValidationError_MaxLength"], _quantitativeTextMaxLength));

        RuleFor(x => x.TextQuantitativeBelowForDilutedTest)
            .MaximumLength(_quantitativeTextMaxLength)
            .WithMessage(string.Format(loc["MethodDetailsReportTab_ValidationError_MaxLength"], _quantitativeTextMaxLength));

        RuleFor(x => x.TextQuantitativeAboveForDilutedTest)
            .MaximumLength(_quantitativeTextMaxLength)
            .WithMessage(string.Format(loc["MethodDetailsReportTab_ValidationError_MaxLength"], _quantitativeTextMaxLength));
    }
}
