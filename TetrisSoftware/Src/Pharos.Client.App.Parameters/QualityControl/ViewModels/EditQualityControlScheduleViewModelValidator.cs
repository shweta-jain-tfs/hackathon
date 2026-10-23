using System.Globalization;
using FluentValidation;

namespace Pharos.Client.App.Parameters.QualityControl.ViewModels;

public class EditQualityControlScheduleViewModelValidator : AbstractValidator<EditQualityControlScheduleViewModel>
{
    public EditQualityControlScheduleViewModelValidator(IStringLocalizer<Translations> loc)
    {
        When(x => x.Periodically == Periodically.YesByNumberOfTests, () =>
        {
            RuleFor(x => x.StartAfter)
                .InclusiveBetween(0, 10000)
                .WithMessage(string.Format(CultureInfo.InvariantCulture, loc["EditQualityControlScheduleModalView_ValidationError_Range"], 0, 10000));

            RuleFor(x => x.RunEvery)
                .InclusiveBetween(1, 10000)
                .WithMessage(string.Format(CultureInfo.InvariantCulture, loc["EditQualityControlScheduleModalView_ValidationError_Range"], 1, 10000));
        });

        When(x => x.Periodically == Periodically.YesByTime, () =>
        {
            RuleFor(x => x.StartAfter)
                .InclusiveBetween(0, 1440) //24*60
                .WithMessage(string.Format(CultureInfo.InvariantCulture, loc["EditQualityControlScheduleModalView_ValidationError_Range"], 0, 1440));

            RuleFor(x => x.RunEvery)
                .InclusiveBetween(1, 1440) //24*60
                .WithMessage(string.Format(CultureInfo.InvariantCulture, loc["EditQualityControlScheduleModalView_ValidationError_Range"], 1, 1440));
        });
    }
}