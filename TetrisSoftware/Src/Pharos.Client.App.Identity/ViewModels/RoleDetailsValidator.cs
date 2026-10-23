using FluentValidation;

namespace Pharos.Client.App.Identity.ViewModels;

/// <summary>
/// Validator for <see cref="RoleDetailsViewModel"/>.
/// This is picked up automatically by the fluent validation validator
/// inside <see cref="Pharos.Client.Common.App.Validation.PhSimpleValidationContext"/>
/// </summary>
public class RoleDetailsValidator : AbstractValidator<RoleDetailsViewModel>
{
    public RoleDetailsValidator(IStringLocalizer<Translations> loc)
    {
        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .WithMessage(loc["Role_DisplayNameRequired"]);

        RuleFor(x => x.DisplayName)
            .Must((model, displayName) => !model.IsDisplayNameInUse)
            .WithMessage(loc["Role_Exists"])
            .When(x => !string.IsNullOrWhiteSpace(x.DisplayName));
    }
}
