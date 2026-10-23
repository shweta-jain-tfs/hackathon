using FluentValidation;

namespace Pharos.Client.App.Parameters.Reflex.ViewModels;

/// <summary>
/// Validator for <see cref="ReflexTest"/>.
/// This is picked up automatically by the fluent validation validator
/// inside <see cref="Client.Common.App.Validation.PhSimpleValidationContext"/>
/// </summary>
public class ReflexTestValidator : AbstractValidator<ReflexTest>
{
    public ReflexTestValidator(IStringLocalizer<Translations> loc)
    {
        RuleFor(x => x.NumberOfReplicates)
            .InclusiveBetween(1, 50)
            .WithMessage(loc["ReflexDetailPage_NumberOfReplicates_ValidationRange"].Value.Replace("{0}", "1-50"));
    }
}
