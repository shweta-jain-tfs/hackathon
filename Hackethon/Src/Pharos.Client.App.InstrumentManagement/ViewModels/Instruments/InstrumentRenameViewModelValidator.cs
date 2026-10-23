using FluentValidation;
using Microsoft.Extensions.Localization;
using Pharos.Client.Common.Localization;

namespace Pharos.Client.App.InstrumentManagement.ViewModels.Instruments;

/// <summary>
/// Validator for <see cref="InstrumentRenameViewModel"/>.
/// This is picked up automatically by the fluent validation validator
/// inside <see cref="Pharos.Client.Common.App.Validation.PhSimpleValidationContext"/>
/// </summary>
public class InstrumentRenameViewModelValidator : AbstractValidator<InstrumentRenameViewModel>
{
    public InstrumentRenameViewModelValidator(IStringLocalizer<Translations> loc)
    {
        RuleFor(x => x.UpdatedDisplayName)
            .NotEmpty().WithMessage(loc["InstrumentsPage_RenameDialog_Validation_NameRequired"])
            .MaximumLength(8).WithMessage(loc["InstrumentsPage_RenameDialog_Validation_MaxLength"]);
    }
}