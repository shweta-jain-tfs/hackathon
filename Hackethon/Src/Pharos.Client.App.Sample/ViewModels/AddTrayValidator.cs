using System.Globalization;
using FluentValidation;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.Sample.Models;
using Pharos.Client.Common.Localization;

namespace Pharos.Client.App.Sample.ViewModels;

/// <summary>
/// Validator for <see cref="AddTrayViewModel"/> with client-side validation rules.
/// This is picked up automatically by the fluent validation validator
/// inside <see cref="Common.App.Validation.PhSimpleValidationContext"/>
/// </summary>
public class AddTrayValidator : AbstractValidator<AddTrayViewModel>
{
    public AddTrayValidator(IStringLocalizer<Translations> loc)
    {
        RuleFor(x => x.TrayId)
            .Must(value => !string.IsNullOrEmpty(value)
                           && value.Length == SampleConstants.TrayIdLength
                           && int.TryParse(value, out _))
            .WithMessage(loc["TraysPage_AddDialog_EnterDigits"].Value.Replace("{0}", SampleConstants.TrayIdLength.ToString(CultureInfo.InvariantCulture)));
    }
}
