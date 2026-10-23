using System.Linq.Expressions;
using Microsoft.AspNetCore.Components.Forms;
using Pharos.Common.Web.Components.FormElements.Enums;
using Pharos.Common.Web.Components.Label;
using Pharos.Common.Web.Extensions;
using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.FormElements;

/// <summary>
/// Represents a text area component with various customizable parameters.
/// </summary>
public partial class PhTextArea
{
    [CascadingParameter] public EditContext? CascadedEditContext { get; set; }

    /// <summary>
    /// Gets or sets the ID of the text area.
    /// </summary>
    [Parameter] public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the value of the text area.
    /// </summary>
    [Parameter] public string? Value { get; set; }

    /// <summary>
    /// Gets or sets the expression that identifies the value.
    /// </summary>
    [Parameter] public Expression<Func<string?>>? ValueExpression { get; set; }

    /// <summary>
    /// Gets or sets the callback that is invoked when the value changes.
    /// </summary>
    [Parameter] public EventCallback<string?> ValueChanged { get; set; }

    /// <summary>
    /// Gets or sets the label of the text area.
    /// </summary>
    [Parameter] public string Label { get; set; } = "";

    /// <summary>
    /// Gets or sets the placeholder text of the text area.
    /// </summary>
    [Parameter] public string PlaceHolder { get; set; } = "";

    /// <summary>
    /// Gets or sets the number of rows in the text area.
    /// </summary>
    [Parameter] public int Rows { get; set; } = 3;

    /// <summary>
    /// Gets or sets the size of the text area.
    /// </summary>
    [Parameter] public TextAreaSize Size { get; set; } = TextAreaSize.Medium;

    /// <summary>
    /// Gets or sets the maximum length of the text area.
    /// </summary>
    [Parameter] public int? MaxLength { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the text area is disabled.
    /// </summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>
    /// Renders the component as display-only (non-interactive, no automatic validation).
    /// Overrides <see cref="Disabled"/>, but keeps the disabled label and value color.
    /// </summary>
    [Parameter] public bool DisplayOnly { get; set; }

    /// <summary>
    /// Mark the field as having validation errors. This is set automatically when used in an EditForm with validation (PhValidationContext).
    /// </summary>
    [Parameter] public bool Error { get; set; }

    /// <summary>
    /// Error text to display when the field has validation errors.
    /// </summary>
    [Parameter] public string? ErrorText { get; set; }

    /// <summary>
    /// Help text to display below the input field.
    /// </summary>
    [Parameter] public string? HelpText { get; set; } = "";

    private bool _error;
    private FieldIdentifier? _fieldIdentifier;
    protected bool HasErrorText => _error && !string.IsNullOrEmpty(ErrorText);
    protected Expression<Func<object>>? ValidationForExpression { get; set; }
    protected bool HasLabel => !string.IsNullOrEmpty(Label);

    /// <summary>
    /// Gets the CSS class names for the text area.
    /// </summary>
    protected string Classname => new CssBuilder("ph-textarea")
        .AddClass("ph-textarea--error", _error)
        .AddClass("ph-textarea--size-small", Size == TextAreaSize.Small)
        .AddClass("ph-textarea--size-medium", Size == TextAreaSize.Medium)
        .AddClass("ph-textarea--size-large", Size == TextAreaSize.Large)
        .AddClass("ph-textarea--size-xlarge", Size == TextAreaSize.ExtraLarge)
        .Build();

    protected string? AutomatedTestId { get; set; }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (!_fieldIdentifier.HasValue && ValueExpression is not null)
        {
            _fieldIdentifier = FieldIdentifier.Create(ValueExpression);
            AutomatedTestId = this.CreateAutomatedTestId(_fieldIdentifier.Value);
        }

        if (DisplayOnly)
        {
            _error = Error;
        }
        else
        {
            UpdateValidationError();

            if (HasLabel && ValueExpression is not null && ValidationForExpression is null)
            {
                ValidationForExpression = PhLabel.CreateValidationForExpression(ValueExpression);
            }
        }
    }

    private void UpdateValidationError()
    {
        if (!Error && CascadedEditContext is not null && _fieldIdentifier.HasValue)
        {
            _error = !CascadedEditContext.IsValid(_fieldIdentifier.Value);
        }
        else
        {
            _error = Error;
        }
    }

    private void TriggerRevalidation()
    {
        if (_fieldIdentifier.HasValue)
        {
            CascadedEditContext?.NotifyFieldChanged(_fieldIdentifier.Value);
            UpdateValidationError();
        }
    }

    private async Task OnInput(ChangeEventArgs args)
    {
        if (_error)
        {
            await ValueChanged.InvokeAsync(args.Value?.ToString());
            TriggerRevalidation();
        }
    }
}
