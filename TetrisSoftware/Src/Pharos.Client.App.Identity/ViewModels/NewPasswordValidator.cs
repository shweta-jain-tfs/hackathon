using FluentValidation;
using Pharos.Client.App.Identity.Helpers;

namespace Pharos.Client.App.Identity.ViewModels;

/// <summary>
/// Validator for <see cref="NewPasswordViewModel"/> with client-side validation rules.
/// This is picked up automatically by the fluent validation validator
/// inside <see cref="Common.App.Validation.PhSimpleValidationContext"/>
/// </summary>
public class NewPasswordValidator : AbstractValidator<NewPasswordViewModel>
{
    public NewPasswordValidator(IStringLocalizer<Translations> loc)
    {
        RuleFor(x => x.TemporaryPassword)
            .Must((model, password) =>
                PasswordHelper.IsPasswordValid(
                    password,
                    model.PasswordPolicyContext?.UiSettings,
                    model.UserName))
            .WithMessage(loc["ApplicationUsersPage_PasswordDoesNotMeetRequirements"]);
    }
}
