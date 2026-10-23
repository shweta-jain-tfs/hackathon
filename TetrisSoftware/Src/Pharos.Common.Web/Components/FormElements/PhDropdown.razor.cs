using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components.Forms;
using Pharos.Common.Web.Components.FormElements.Enums;
using Pharos.Common.Web.Components.Label;
using Pharos.Common.Web.Components.Popover;
using Pharos.Common.Web.Components.Status;
using Pharos.Common.Web.Extensions;
using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.FormElements;

/// <summary>
/// A generic dropdown/select component with label, help text, sizing, status symbols, and validation support.
/// </summary>
public partial class PhDropdown<TValue>
{
    [CascadingParameter] public EditContext? CascadedEditContext { get; set; }

    /// <summary>
    /// The dropdown element ID (optional).
    /// </summary>
    [Parameter] public string? Id { get; set; }

    /// <summary>
    /// Optional title or placeholder text for the dropdown.
    /// </summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>
    /// The currently selected value.
    /// </summary>
    [Parameter] public TValue? Value { get; set; }

    /// <summary>
    /// Represents the bound value and is set automatically when using @bind.
    /// </summary>
    [Parameter] public Expression<Func<TValue>>? ValueExpression { get; set; }

    /// <summary>
    /// Disables the dropdown.
    /// </summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>
    /// Renders the component as display-only (non-interactive, no automatic validation).
    /// Overrides <see cref="Disabled"/>, but keeps the disabled label and value color.
    /// </summary>
    [Parameter] public bool DisplayOnly { get; set; }

    /// <summary>
    /// Controls the width/size of the dropdown.
    /// </summary>
    [Parameter] public DropdownSize Size { get; set; } = DropdownSize.MediumInputWidth;

    /// <summary>
    /// The list of options available for selection.
    /// </summary>
    [Parameter] public List<DropDownOption<TValue>> Options { get; set; } = new();

    /// <summary>
    /// Raised when the selected value changes.
    /// </summary>
    [Parameter] public EventCallback<TValue?> ValueChanged { get; set; }

    /// <summary>
    /// Manually marks the field as having validation errors, overriding the EditContext validation state.
    /// </summary>
    [Parameter] public bool Error { get; set; }

    /// <summary>
    /// Label displayed above the dropdown.
    /// </summary>
    [Parameter] public string Label { get; set; } = "";

    /// <summary>
    /// Custom validation message to display for this field.
    /// Overrides validation messages from the EditContext.
    /// </summary>
    [Parameter] public string? ErrorText { get; set; }

    /// <summary>
    /// Help text displayed below the dropdown.
    /// Replaced by error text when a validation error is present.
    /// </summary>
    [Parameter] public string? HelpText { get; set; } = "";

    private DropDownOption<TValue>? SelectedOption { get; set; }
    private bool _isOpen;
    private PhPopover? _popover;
    private bool SymbolsExists { get; set; }
    [MemberNotNullWhen(true, nameof(SelectedOption))]
    private bool HasSelectedStatus => SelectedOption is not null && SelectedOption.StatusType != StatusType.None;
    private FieldIdentifier? _fieldIdentifier;
    private bool _error;

    protected string CssClasses =>
        new CssBuilder("ph-dropdown")
            .AddClass("ph-dropdown--error", _error)
            .AddClass("ph-dropdown--open", _isOpen)
            .AddClass("ph-dropdown--placeholder", SelectedOption is null)
            .AddClass("ph-dropdown--size-small", Size == DropdownSize.SmallInputWidth)
            .AddClass("ph-dropdown--size-medium", Size == DropdownSize.MediumInputWidth)
            .AddClass("ph-dropdown--size-large", Size == DropdownSize.LargeInputWidth)
            .AddClass("ph-dropdown--size-fit-content", Size == DropdownSize.FitContent)
            .AddClass("ph-dropdown--many-options", HasManyItems)
            .Build();

    protected string? AutomatedTestId { get; set; }

    // Add PhLabel support properties
    protected Expression<Func<object>>? ValidationForExpression { get; set; }
    protected bool HasLabel => !string.IsNullOrEmpty(Label);
    protected bool HasErrorText => _error && !string.IsNullOrEmpty(ErrorText);
    protected bool HasManyItems => Options.Count > 8;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        SelectedOption = Options?.FirstOrDefault(opt => opt.Value?.ToString() == Value?.ToString());
        if (Value is not null && SelectedOption == null)
        {
            // show current (non-empty) value even if it isn't in the list of options
            SelectedOption = new()
            {
                Text = Value.ToString(),
                Value = Value
            };
        }

        if (Options != null)
        {
            SymbolsExists = Options.Any(x => x.StatusType != StatusType.None);
        }

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
            if (!Error && CascadedEditContext is not null && _fieldIdentifier.HasValue)
            {
                _error = !CascadedEditContext.IsValid(_fieldIdentifier.Value);
            }
            else
            {
                _error = Error;
            }

            // Add validation expression setup for PhLabel
            if (HasLabel && ValueExpression is not null && ValidationForExpression is null)
            {
                ValidationForExpression = PhLabel.CreateValidationForExpression(ValueExpression);
            }
        }
    }

    private async void SelectOption(DropDownOption<TValue> option)
    {
        if (option.Disabled)
        {
            return;
        }

        CloseDropdown();

        if (DisplayOnly)
        {
            return;
        }

        if (option != SelectedOption)
        {
            SelectedOption = option;
            Value = option.Value;
            if (ValueChanged.HasDelegate)
            {
                await ValueChanged.InvokeAsync(Value);
            }

            if (_fieldIdentifier.HasValue)
            {
                CascadedEditContext?.NotifyFieldChanged(_fieldIdentifier.Value);
            }
        }
    }

    private void CloseDropdown()
    {
        _popover?.Hide();
    }

    private void SetOpenState(bool value)
    {
        // Set open state from pressing the toggle button and soft dismiss
        _isOpen = value;
    }
}

public class DropDownOption<T>
{
    public bool Disabled { get; set; }
    public StatusType StatusType { get; set; } = StatusType.None;
    public string? Text { get; set; }
    public T? Value { get; set; }
}