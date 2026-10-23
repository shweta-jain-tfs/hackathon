using FluentValidation;
namespace Pharos.Client.App.Parameters.Methods.ViewModels;

public sealed class MethodDetailsViewModelValidator : AbstractValidator<MethodDetailsViewModel>
{
    private const int _methodNameLisMaxLength = 20;

    public MethodDetailsViewModelValidator(IStringLocalizer<Translations> loc)
    {
        RuleFor(x => x.MethodNameLis)
            .NotEmpty()
            .WithMessage(loc["MethodDetailsGeneralTab_Validation_LISMethodIDRequired"]);

        RuleFor(x => x.MethodNameLis)
            .MaximumLength(_methodNameLisMaxLength)
            .WithMessage(string.Format(
                loc["MethodDetailsGeneralTab_Validation_LISMethodIDMaxLength"],
                _methodNameLisMaxLength));
    }
}

public sealed class MethodReportViewModelValidator : AbstractValidator<MethodReportViewModel>
{
    private const int _cutOffTextMaxLength = 8;
    private const int _quantitativeTextMaxLength = 7;
    private const int _minConcentrationLowerLimit = 0;
    private const int _minConcentrationUpperLimit = 99999;

    public MethodReportViewModelValidator(IStringLocalizer<Translations> loc)
    {
        RuleFor(x => x.MinConcentrationToReport)
            .InclusiveBetween(_minConcentrationLowerLimit, _minConcentrationUpperLimit)
            .WithMessage(string.Format(
                loc["MethodDetailsReportTab_ValidationError_Range"],
                _minConcentrationLowerLimit,
                _minConcentrationUpperLimit));

        RuleFor(x => x.TextBelowCutOff)
            .MaximumLength(8)
            .WithMessage(string.Format(loc["MethodDetailsReportTab_ValidationError_MaxLength"], _cutOffTextMaxLength));

        RuleFor(x => x.TextAboveCutOff)
            .MaximumLength(8)
            .WithMessage(string.Format(loc["MethodDetailsReportTab_ValidationError_MaxLength"], _cutOffTextMaxLength));

        RuleFor(x => x.TextQuantitativeBelow)
            .MaximumLength(7)
            .WithMessage(string.Format(loc["MethodDetailsReportTab_ValidationError_MaxLength"], _quantitativeTextMaxLength));

        RuleFor(x => x.TextQuantitativeDoubtful)
            .MaximumLength(7)
            .WithMessage(string.Format(loc["MethodDetailsReportTab_ValidationError_MaxLength"], _quantitativeTextMaxLength));

        RuleFor(x => x.TextQuantitativeAbove)
            .MaximumLength(7)
            .WithMessage(string.Format(loc["MethodDetailsReportTab_ValidationError_MaxLength"], _quantitativeTextMaxLength));
    }
}

public sealed class MethodCalibratorsViewModelValidator : AbstractValidator<MethodCalibratorsViewModel>
{
    private const int _maxNumberOfdaysLowerLimit = 0;

    public MethodCalibratorsViewModelValidator(IStringLocalizer<Translations> loc)
    {
        RuleFor(x => x.MaxNumberOfDays)
            .Must((viewModel, value) => value >= _maxNumberOfdaysLowerLimit && value <= viewModel.DefaultMaxNumberOfDays)
            .WithMessage(viewModel => string.Format(loc["MethodDetailsCalibratorsTab_MaxNumberOfDaysError"], _maxNumberOfdaysLowerLimit, viewModel.DefaultMaxNumberOfDays));
    }
}

public sealed class MethodClassTableDetailsViewModelValidator : AbstractValidator<MethodClassTableDetailsViewModel>
{
    private const int _classTextMaxLength = 8;

    public MethodClassTableDetailsViewModelValidator(IStringLocalizer<Translations> loc)
    {
        RuleForEach(x => x.Rows)
            .SetValidator(new MethodClassTableDetailsRowViewModelValidator(loc));
    }

    private sealed class MethodClassTableDetailsRowViewModelValidator : AbstractValidator<MethodClassTableDetailsRowViewModel>
    {
        public MethodClassTableDetailsRowViewModelValidator(IStringLocalizer<Translations> loc)
        {
            RuleFor(x => x.Text)
                .NotEmpty();

            RuleFor(x => x.Text)
                .MaximumLength(_classTextMaxLength)
                .WithMessage(string.Format(loc["MethodDetailsReportTab_ValidationError_MaxLength"], _classTextMaxLength));
        }
    }
}
