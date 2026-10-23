using Microsoft.AspNetCore.Components.Forms;

namespace Pharos.Client.Common.App.Validation;

/// <summary>
/// A section of a form that can be validated independently. Validates one view model.
/// </summary>
/// <remarks>
/// For debugging a <see cref="ValidationSummary"/> can be placed in the <see cref="ChildContent"/>.
/// </remarks>
public partial class PhValidationSection
{
    [CascadingParameter] public PhValidationContext? CascadedValidationContext { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter, EditorRequired] public string? SectionName { get; set; }

    /// <summary>
    /// Model to validate. Needs a corresponding <see cref="FluentValidation.AbstractValidator{T}"/>.
    /// </summary>
    [Parameter, EditorRequired] public object? Model { get; set; }

    /// <summary>
    /// Optional comma separated list of rulesets to validate if only validating a subset of rules.
    /// These rule sets are specified in the <see cref="FluentValidation.AbstractValidator{T}"/> for <see cref="Model"/>.
    /// </summary>
    /// <example>
    /// If half of a model is validated in one tab and half is validated in another tab or outside the tab control,
    /// the validation rules need to be split up into separate rulesets for. One for each validation section.
    /// </example>
    [Parameter] public string? RuleSets { get; set; }

    private string[] _ruleSets = [];
    private EditForm? _editForm;
    private const string _defaultRuleSetName = "default";
    private readonly string[] _defaultRuleSets = [_defaultRuleSetName];
    private EditContext? _currentEditContext;

    public string Id => $"validation-section-{SectionName}";

    protected override void OnParametersSet()
    {
        // Default to "default" ruleset if none specified. This includes all rules not inside a named ruleset in the validator.
        _ruleSets = RuleSets?.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries) ?? _defaultRuleSets;

        if (_currentEditContext is not null && _currentEditContext != _editForm?.EditContext)
        {
            // Refresh validation context in case the model is reset by a rogue field on the page bound to another model.
            // This is a fail-safe to avoid sticky validation errors when the edit context is recreated.
            RegisterInValidationContext();
        }
    }

    protected override void OnAfterRender(bool firstRender)
    {
        var modelChanged = _currentEditContext?.Model != _editForm?.Model;
        if (firstRender || modelChanged)
        {
            RegisterInValidationContext();
        }
    }

    private void RegisterInValidationContext()
    {
        if (CascadedValidationContext is not null && _editForm?.EditContext is not null && SectionName is not null)
        {
            CascadedValidationContext.AddSection(SectionName, _editForm.EditContext);
            _currentEditContext = _editForm.EditContext;
        }
    }
}