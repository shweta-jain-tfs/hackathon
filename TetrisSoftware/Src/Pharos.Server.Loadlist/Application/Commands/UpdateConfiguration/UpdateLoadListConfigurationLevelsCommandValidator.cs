using FluentValidation;

namespace Pharos.Server.LoadList.Application.Commands.UpdateConfiguration;

public class UpdateLoadListConfigurationLevelsCommandValidator : AbstractValidator<UpdateLoadListConfigurationLevelsCommand>
{
    public UpdateLoadListConfigurationLevelsCommandValidator()
    {
        // Validate that at least one configuration is selected
        RuleFor(x => x.UpdatedConfigurationsIds)
            .NotNull()
            .NotEmpty();

        RuleFor(x => x.TriggerLevel)
            .InclusiveBetween(0, 100000);

        RuleFor(x => x.RefillLevel)
            .InclusiveBetween(0, 100000);

        RuleFor(x => x.TriggerLevel)
            .LessThan(x => x.RefillLevel)
            .When(x => x.RefillLevel > 0);

        RuleFor(x => x.RefillLevel)
            .GreaterThan(x => x.TriggerLevel)
            .When(x => x.TriggerLevel > 0);
    }
}