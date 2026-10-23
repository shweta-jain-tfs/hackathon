namespace Pharos.Client.Common.App.Validation;

/// <summary>
/// Validation context for a single model. Use this for simple forms with one model.
/// </summary>
public partial class PhSimpleValidationContext
{
    /// <summary>
    /// Model to validate. Needs a corresponding <see cref="FluentValidation.AbstractValidator{T}"/>.
    /// </summary>
    [Parameter, EditorRequired] public object? Model { get; set; }

    /// <summary>
    /// Optional comma separated list of rulesets to validate if only validating a subset of rules.
    /// These rule sets are specified in the <see cref="FluentValidation.AbstractValidator{T}"/> for <see cref="Model"/>.
    /// </summary>
    [Parameter] public string? RuleSets { get; set; }
}