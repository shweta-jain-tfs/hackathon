using System.Linq.Expressions;
using Microsoft.AspNetCore.Components.Forms;
using Pharos.Common.Web.Components.FormElements.Enums;
using Pharos.Common.Web.Components.Icons;
using Pharos.Common.Web.Components.Status;
using Pharos.Common.Web.Extensions;
using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.FormElements;

/// <summary>
/// A checkbox component with optional label, help text, sizing, and validation support.
/// </summary>
public partial class PhCheckBox
{
    [CascadingParameter] public EditContext? CascadedEditContext { get; set; }

    /// <summary>
    /// The checkbox element ID (optional).
    /// </summary>
    [Parameter] public string? Id { get; set; }

    /// <summary>
    /// Label displayed next to the checkbox.
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// Optional help text shown below the label.
    /// Replaced by validation or error text when present.
    /// </summary>
    [Parameter] public string? LabelHelpText { get; set; }

    /// <summary>
    /// Controls the visual size of the checkbox.
    /// </summary>
    [Parameter] public CheckBoxSize Size { get; set; } = CheckBoxSize.Large;

    /// <summary>
    /// Renders the control with radio-button visuals instead of checkbox visuals.
    /// </summary>
    [Parameter] public bool RadioStyle { get; set; }

    /// <summary>
    /// Applies a muted border style.
    /// </summary>
    [Parameter] public bool MutedBorder { get; set; }

    /// <summary>
    /// Disables the checkbox.
    /// </summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>
    /// Renders the component as display-only (non-interactive, no automatic validation).
    /// Overrides <see cref="Disabled"/>, but keeps the disabled label and value color.
    /// </summary>
    [Parameter] public bool DisplayOnly { get; set; }

    /// <summary>
    /// The current checked state.
    /// </summary>
    [Parameter] public bool Value { get; set; }

    /// <summary>
    /// Displays the checkbox in an indeterminate (mixed) state.
    /// </summary>
    [Parameter] public bool Indeterminate { get; set; }

    /// <summary>
    /// Raised when the checked state changes.
    /// </summary>
    [Parameter] public EventCallback<bool> ValueChanged { get; set; }

    /// <summary>
    /// Represents the bound value and is set automatically when using @bind.
    /// </summary>
    [Parameter] public Expression<Func<bool>>? ValueExpression { get; set; }

    /// <summary>
    /// Additional callback invoked when the checkbox value changes.
    /// </summary>
    [Parameter] public EventCallback<bool> OnCheckChanged { get; set; }

    /// <summary>
    /// Custom validation message to display for this field.
    /// Overrides validation messages from the <see cref="EditContext"/>.
    /// </summary>
    [Parameter] public string? ErrorText { get; set; }

    /// <summary>
    /// Status icon to display between the checkbox and the label (e.g., alarm, warning).
    /// Default status icon is <see cref="StatusType.None"/>
    /// </summary>
    [Parameter] public StatusType Status { get; set; } = StatusType.None;

    private bool _error;
    private string? _errorText;
    private FieldIdentifier? _fieldIdentifier;

    protected string Classname =>
        new CssBuilder("ph-checkbox")
            .AddClass("ph-checkbox--checked", IsChecked)
            .AddClass("ph-checkbox--error", _error)
            .AddClass("ph-checkbox--size-large", Size == CheckBoxSize.Large)
            .AddClass("ph-checkbox--disabled", Disabled)
            .AddClass("ph-checkbox--display-only", DisplayOnly)
            .AddClass("ph-checkbox--muted-border", MutedBorder)
            .AddClass("ph-checkbox--radio", RadioStyle)
            .Build();

    protected string? AutomatedTestId { get; set; }

    private bool IsChecked => Value;

    private IconSize IconSize => Size == CheckBoxSize.Large ? Icons.IconSize.Sm : Icons.IconSize.Xs;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (!_fieldIdentifier.HasValue && ValueExpression is not null)
        {
            _fieldIdentifier = FieldIdentifier.Create(ValueExpression);
            AutomatedTestId = this.CreateAutomatedTestId(_fieldIdentifier.Value);
        }

        var hasErrorText = !string.IsNullOrEmpty(ErrorText);
        if (DisplayOnly)
        {
            _error = hasErrorText;
            _errorText = ErrorText;
        }
        else
        {
            UpdateErrorState(hasErrorText);
        }
    }

    private void UpdateErrorState(bool hasErrorText)
    {
        if (!hasErrorText && CascadedEditContext is not null && _fieldIdentifier.HasValue)
        {
            _errorText = CascadedEditContext.GetValidationMessages(_fieldIdentifier.Value).FirstOrDefault();
            _error = !CascadedEditContext.IsValid(_fieldIdentifier.Value);
        }
        else
        {
            _error = hasErrorText;
            _errorText = ErrorText;
        }
    }

    private async Task SetCurrentValueAsync(bool value)
    {
        if (ValueChanged.HasDelegate)
        {
            await ValueChanged.InvokeAsync(value);
        }

        if (OnCheckChanged.HasDelegate)
        {
            await OnCheckChanged.InvokeAsync(value);
        }

        NotifyFieldChanged();
    }

    private void NotifyFieldChanged()
    {
        if (DisplayOnly)
        {
            return;
        }

        if (_fieldIdentifier.HasValue)
        {
            CascadedEditContext?.NotifyFieldChanged(_fieldIdentifier.Value);
        }

        UpdateErrorState(!string.IsNullOrEmpty(ErrorText));
    }

    private async Task OnInternalChange(ChangeEventArgs _)
    {
        if (DisplayOnly)
        {
            return;
        }

        // Note: In interactive mode we ignore the native checkbox state.
        // The UI is controlled by Value so this event is treated as a toggle request.
        await SetCurrentValueAsync(!Value);
    }
}