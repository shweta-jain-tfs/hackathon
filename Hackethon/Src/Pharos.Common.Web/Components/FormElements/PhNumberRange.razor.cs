using System.Linq.Expressions;
using Microsoft.AspNetCore.Components.Forms;
using Pharos.Common.Web.Components.FormElements.Enums;

namespace Pharos.Common.Web.Components.FormElements;

/// <summary>
/// Numeric range input field with two PhNumberField components (min and max),
/// and validation support. The component displays error messages at the component level.
/// </summary>
public partial class PhNumberRange<TValue> : IDisposable
{
    #region Parameters

    [CascadingParameter] public EditContext? CascadedEditContext { get; set; }

    /// <summary>
    /// Help text displayed below the fields.
    /// Replaced by error text when a validation error is present.
    /// </summary>
    [Parameter] public string? HelpText { get; set; } = "";

    /// <summary>
    /// Custom validation message to display for this field.
    /// When set, it overrides validation messages from the <see cref="EditContext"/>.
    /// </summary>
    [Parameter] public string? ErrorText { get; set; }

    /// <summary>
    /// Placeholder text shown when the min field is empty.
    /// </summary>
    [Parameter] public string? MinPlaceholder { get; set; }

    /// <summary>
    /// Placeholder text shown when the max field is empty.
    /// </summary>
    [Parameter] public string? MaxPlaceholder { get; set; }

    /// <summary>
    /// The current range value as a tuple (Min, Max).
    /// </summary>
    [Parameter] public (TValue? Min, TValue? Max) Value { get; set; }

    /// <summary>
    /// Represents the bound value and is set automatically when using @bind.
    /// </summary>
    [Parameter] public Expression<Func<(TValue? Min, TValue? Max)>>? ValueExpression { get; set; }

    /// <summary>
    /// Raised when the value changes.
    /// </summary>
    [Parameter] public EventCallback<(TValue? Min, TValue? Max)> ValueChanged { get; set; }

    /// <summary>
    /// Disables the inputs.
    /// </summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>
    /// Renders the component as display-only (non-interactive, no automatic validation).
    /// Overrides <see cref="Disabled"/>, but keeps the disabled label and value color.
    /// </summary>
    [Parameter] public bool DisplayOnly { get; set; }

    /// <summary>
    /// Controls the visual size of the fields.
    /// </summary>
    [Parameter] public TextBoxSize Size { get; set; } = TextBoxSize.ExtraSmall;

    /// <summary>
    /// Optional culture name to use instead of the current culture (e.g. "sv-SE").
    /// Used for testing.
    /// </summary>
    [Parameter] public string? Culture { get; set; }

    #endregion

    private readonly EventHandler<ValidationStateChangedEventArgs> _validationStateChangedHandler;
    private FieldIdentifier? _rangeFieldIdentifier;
    private bool _minError;
    private bool _maxError;
    private string? _computedErrorText;
    private EditContext? _editContext;

    protected bool HasErrorText => (_minError || _maxError) && !string.IsNullOrEmpty(_computedErrorText);

    private TValue? MinValue
    {
        get => Value.Min;
        set
        {
            if (!EqualityComparer<TValue>.Default.Equals(Value.Min, value))
            {
                _ = OnMinValueChanged(value);
            }
        }
    }

    private TValue? MaxValue
    {
        get => Value.Max;
        set
        {
            if (!EqualityComparer<TValue>.Default.Equals(Value.Max, value))
            {
                _ = OnMaxValueChanged(value);
            }
        }
    }

    private async Task OnMinValueChanged(TValue? value)
    {
        await ValueChanged.InvokeAsync((value, Value.Max));

        if (CascadedEditContext is not null && _rangeFieldIdentifier.HasValue)
        {
            CascadedEditContext.NotifyFieldChanged(_rangeFieldIdentifier.Value);
            // Don't call UpdateValidationErrors here - let OnValidationStateChanged handle it
            // after the EditContext has finished running validators
        }
    }

    private async Task OnMaxValueChanged(TValue? value)
    {
        await ValueChanged.InvokeAsync((Value.Min, value));

        if (CascadedEditContext is not null && _rangeFieldIdentifier.HasValue)
        {
            CascadedEditContext.NotifyFieldChanged(_rangeFieldIdentifier.Value);
            // Don't call UpdateValidationErrors here - let OnValidationStateChanged handle it
            // after the EditContext has finished running validators
        }
    }

    public PhNumberRange()
    {
        _validationStateChangedHandler = OnValidateStateChanged;
    }

    protected override void OnParametersSet()
    {
        if (!_rangeFieldIdentifier.HasValue && ValueExpression is not null)
        {
            _rangeFieldIdentifier = FieldIdentifier.Create(ValueExpression);
        }

        // Subscribe to validation state changes
        if (CascadedEditContext is not null && _editContext != CascadedEditContext)
        {
            if (_editContext is not null)
            {
                _editContext.OnValidationStateChanged -= _validationStateChangedHandler;
            }

            _editContext = CascadedEditContext;
            _editContext.OnValidationStateChanged += _validationStateChangedHandler;
        }

        UpdateValidationErrors();
    }

    private void UpdateValidationErrors()
    {
        if (!string.IsNullOrEmpty(ErrorText))
        {
            // Manual error text takes precedence
            _minError = true;
            _maxError = true;
            _computedErrorText = ErrorText;
            return;
        }

        // Reset if no ValidationFor or EditContext
        if (CascadedEditContext is null || !_rangeFieldIdentifier.HasValue)
        {
            _minError = false;
            _maxError = false;
            _computedErrorText = null;
            return;
        }

        // Check if the range field has validation errors
        var isValid = CascadedEditContext.IsValid(_rangeFieldIdentifier.Value);
        if (!isValid)
        {
            var messages = CascadedEditContext.GetValidationMessages(_rangeFieldIdentifier.Value).ToList();
            _computedErrorText = messages.FirstOrDefault();

            // Determine which field(s) should show error icons
            // If one field is empty, show icon only on that field
            // If both empty or both have values (comparison error), show icons on both
            var isMinEmpty = Value.Min is null;
            var isMaxEmpty = Value.Max is null;

            if (isMinEmpty && !isMaxEmpty)
            {
                // Only min is empty
                _minError = true;
                _maxError = false;
            }
            else if (!isMinEmpty && isMaxEmpty)
            {
                // Only max is empty
                _minError = false;
                _maxError = true;
            }
            else
            {
                // Both empty or both have values (comparison error) - show on both
                _minError = true;
                _maxError = true;
            }
        }
        else
        {
            _minError = false;
            _maxError = false;
            _computedErrorText = null;
        }
    }

    private void OnValidateStateChanged(object? sender, ValidationStateChangedEventArgs e)
    {
        var currentError = _computedErrorText;
        var currentMinError = _minError;
        var currentMaxError = _maxError;

        UpdateValidationErrors();

        // Re-render if error text OR error icon states changed
        if (currentError != _computedErrorText || currentMinError != _minError || currentMaxError != _maxError)
        {
            StateHasChanged();
        }
    }

    public void Dispose()
    {
        if (_editContext is not null)
        {
            _editContext.OnValidationStateChanged -= _validationStateChangedHandler;
        }

        GC.SuppressFinalize(this);
    }
}
