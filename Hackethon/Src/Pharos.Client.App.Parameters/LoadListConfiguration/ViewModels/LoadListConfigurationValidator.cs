using FluentValidation;
using Pharos.Client.Common.App.Validation;

namespace Pharos.Client.App.Parameters.LoadListConfiguration.ViewModels;

/// <summary>
/// Validator for <see cref="LoadListConfiguration"/>.
/// This is picked up automatically by the fluent validation validator
/// inside <see cref="PhSimpleValidationContext"/>
/// </summary>
public class LoadListConfigurationValidator : AbstractValidator<ConfigurationItem>
{
    public LoadListConfigurationValidator(IStringLocalizer<Translations> loc)
    {
        RuleFor(x => x.TriggerLevel)
            .InclusiveBetween(0, 100000)
            .When(x => x.TriggerLevel.HasValue)
            .WithMessage(loc["LoadListConfiguration_TriggerLevel_ValidationText"]);

        RuleFor(x => x.RefillLevel)
            .InclusiveBetween(0, 100000)
            .When(x => x.RefillLevel.HasValue)
            .WithMessage(loc["LoadListConfiguration_RefillLevel_ValidationText"]);

        RuleFor(x => x.TriggerLevel)
            .LessThan(x => x.RefillLevel)
            .WithMessage(loc["LoadListConfiguration_TriggerLessThanRefill_ValidationMessage"])
            .When(x => x.RefillLevel.HasValue && x.TriggerLevel.HasValue && (x.RefillLevel.Value > 0 || x.TriggerLevel.Value > 0));

        RuleFor(x => x.TriggerLevel)
            .Must((item, triggerLevel) => !triggerLevel.HasValue || item.ConfigurationsToEdit == null || 
                  item.ConfigurationsToEdit.All(config => !config.RefillLevel.HasValue || triggerLevel.Value < config.RefillLevel.Value))
            .WithMessage(loc["LoadListConfiguration_TriggerLessThanConfigurationsRefill_ValidationMessage"])
            .When(x => x.TriggerLevel.HasValue && x.ConfigurationsToEdit is { Count: > 0 } && x is not { TriggerLevel: not null, RefillLevel: not null });

        RuleFor(x => x.RefillLevel)
            .Must((item, refillLevel) => !refillLevel.HasValue || item.ConfigurationsToEdit == null || 
                  item.ConfigurationsToEdit.All(config => !config.TriggerLevel.HasValue || config.TriggerLevel.Value < refillLevel.Value))
            .WithMessage(loc["LoadListConfiguration_ConfigurationsTriggerLessThanRefill_ValidationMessage"])
            .When(x => x.RefillLevel.HasValue && x.ConfigurationsToEdit is { Count: > 0 } && x is not { TriggerLevel: not null, RefillLevel: not null });
    }
}

/// <summary>
/// Class used for validation context
/// </summary>
public class ConfigurationItem
{
    public int? TriggerLevel { get; set; }
    public int? RefillLevel { get; set; }
    public HashSet<Client.Common.Models.LoadListConfiguration>? ConfigurationsToEdit { get; set; }
}