using FluentValidation;
using Pharos.Client.App.Identity.Helpers;

namespace Pharos.Client.App.Identity.ViewModels;

/// <summary>
/// Validator for <see cref="ApplicationUserDetailsViewModel"/>.
/// This is picked up automatically by the fluent validation validator
/// inside <see cref="Pharos.Client.Common.App.Validation.PhSimpleValidationContext"/>
/// </summary>
public class ApplicationUserDetailsViewModelValidator : AbstractValidator<ApplicationUserDetailsViewModel>
{
    public ApplicationUserDetailsViewModelValidator(IStringLocalizer<Translations> loc)
    {
        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage(loc["ApplicationUsers_UserNameEmpty"])
            .MaximumLength(UserContract.MaximumNameLength).WithMessage(string.Format(loc["ApplicationUsers_UserNameMaxLength"], UserContract.MaximumNameLength))
            .Must(ApplicationUserHelper.AreAllUsernameCharactersValid).WithMessage(loc["ApplicationUsers_UserNameInvalid"])
            .Must((model, _) => model.IsUserNameUnique).WithMessage(loc["ApplicationUsers_UserNameAlreadyExists"]);
    }
}