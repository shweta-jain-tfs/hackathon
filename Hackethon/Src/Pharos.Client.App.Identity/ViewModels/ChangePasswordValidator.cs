using FluentValidation;
using Pharos.Client.App.Identity.Helpers;

namespace Pharos.Client.App.Identity.ViewModels;

/// <summary>
/// Validator for <see cref="ChangePasswordViewModel"/> with client-side validation rules.
/// This is picked up automatically by the fluent validation validator
/// inside <see cref="Common.App.Validation.PhSimpleValidationContext"/>
/// </summary>
public class ChangePasswordValidator : AbstractValidator<ChangePasswordViewModel>
{
    public ChangePasswordValidator(IStringLocalizer<Translations> loc)
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty()
            .WithMessage(loc["ApplicationUsersPage_ChangePassword_CurrentPasswordRequired"]);

        RuleFor(x => x.NewPassword)
            .Must((model, password) =>
                PasswordHelper.IsPasswordValid(
                    password,
                    model.PasswordPolicyContext?.UiSettings,
                    model.UserName))
            .WithMessage(loc["ApplicationUsersPage_PasswordDoesNotMeetRequirements"])
            .NotEqual(x => x.CurrentPassword)
            .WithMessage(loc["ApplicationUsersPage_ChangePassword_PasswordsAreTheSame"]);

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.NewPassword)
            .WithMessage(loc["ApplicationUsersPage_ChangePassword_PasswordsDoNotMatch"]);
    }
}
