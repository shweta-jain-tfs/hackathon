using FluentValidation;

namespace Pharos.Client.App.Request.ViewModels;

/// <summary>
/// Validator for <see cref="TestOrderViewModel"/>.
/// This is picked up automatically by the fluent validation validator
/// inside <see cref="Common.App.Validation.PhSimpleValidationContext"/>
/// </summary>
public class TestOrderValidator : AbstractValidator<TestOrderViewModel>
{
    public TestOrderValidator(IStringLocalizer<Translations> loc)
    {
        RuleFor(x => x.NumberOfReplicates)
            .InclusiveBetween(1, 50)
            .WithMessage(loc["RequestDetailPage_NumberOfReplicates_ValidationRange"].Value.Replace("{0}", "1-50"));
    }
}
