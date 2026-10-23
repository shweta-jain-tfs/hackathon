using FluentValidation;
using Pharos.Client.App.Identity.Helpers;

namespace Pharos.Client.App.Identity.ViewModels;

/// <summary>
/// Validator for <see cref="NewUserValidator"/> with client-side validation rules.
/// This is picked up automatically by the fluent validation validator
/// inside <see cref="Common.App.Validation.PhSimpleValidationContext"/>
/// </summary>
public class NewUserValidator : AbstractValidator<NewUserViewModel>
{
    public NewUserValidator(IStringLocalizer<Translations> loc)
    {
        RuleFor(x => x.UserName)
            .NotEmpty()
            .WithMessage(loc["ApplicationUsersPage_AddDialog_UserNameRequired"])
            .Must(ApplicationUserHelper.AreAllUsernameCharactersValid)
            .WithMessage(loc["ApplicationUsersPage_AddDialog_InvalidUserName"])
            .Must((model, username) => !model.ExistingUserNames.Contains(username, StringComparer.OrdinalIgnoreCase))
            .WithMessage(loc["ApplicationUsersPage_AddDialog_UserExists"]);

        RuleFor(x => x.Password)
            .Must((model, password) =>
                PasswordHelper.IsPasswordValid(
                    password,
                    model.PasswordPolicyContext?.UiSettings,
                    model.UserName))
            .WithMessage(loc["ApplicationUsersPage_PasswordDoesNotMeetRequirements"]);
    }
}
