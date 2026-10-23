using FluentValidation;
using Pharos.Client.Common.App.Interfaces;

namespace Pharos.Client.App.Request.ViewModels;

/// <summary>
/// Validator for <see cref="PatientViewModel"/>.
/// This is picked up automatically by the fluent validation validator
/// inside <see cref="Common.App.Validation.PhValidationSection"/>
/// </summary>
public class PatientViewModelValidator : AbstractValidator<PatientViewModel>
{
    public PatientViewModelValidator(IStringLocalizer<Translations> loc, IDateTime dateTime)
    {
        var today = DateOnly.FromDateTime(dateTime.Now.Date);

        // Patient birthdate validation
        RuleFor(x => x.BirthDate)
            .Must(date => !date.HasValue || date.Value <= today)
            .WithMessage(loc["RequestDetailPage_Patient_BirthDate_Future"]);
    }
}
