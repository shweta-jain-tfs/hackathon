using System.Globalization;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Pharos.Common.Web.Components.FormElements.Enums;
using Pharos.Common.Web.Components.Label;
using Pharos.Common.Web.Extensions;
using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.FormElements;

/// <summary>
/// Numeric input field with label, help text, culture-aware formatting, debounce, and validation support.
/// </summary>
public partial class PhNumberField<TValue>
{
    #region Parameters

    [CascadingParameter] public EditContext? CascadedEditContext { get; set; }

    /// <summary>
    /// The input element ID (optional).
    /// </summary>
    [Parameter] public string? Id { get; set; }

    /// <summary>
    /// Label displayed above the number field.
    /// </summary>
    [Parameter] public string Label { get; set; } = "";

    /// <summary>
    /// Help text displayed below the field.
    /// Replaced by error text when a validation error is present.
    /// </summary>
    [Parameter] public string? HelpText { get; set; } = "";

    /// <summary>
    /// Custom validation message to display for this field.
    /// When set, it overrides validation messages from the <see cref="EditContext"/>.
    /// </summary>
    [Parameter] public string? ErrorText { get; set; }

    /// <summary>
    /// Placeholder text shown when there is no value.
    /// </summary>
    [Parameter] public string? PlaceHolder { get; set; }

    /// <summary>
    /// The current numeric value.
    /// </summary>
    [Parameter] public TValue? Value { get; set; }

    /// <summary>
    /// Represents the bound value and is set automatically when using @bind.
    /// </summary>
    [Parameter] public Expression<Func<TValue>>? ValueExpression { get; set; }

    /// <summary>
    /// Raised when the value changes.
    /// </summary>
    [Parameter] public EventCallback<TValue> ValueChanged { get; set; }

    /// <summary>
    /// Disables the input.
    /// </summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>
    /// Renders the component as display-only (non-interactive, no automatic validation).
    /// Overrides <see cref="Disabled"/>, but keeps the disabled label and value color.
    /// </summary>
    [Parameter] public bool DisplayOnly { get; set; }

    /// <summary>
    /// Controls the visual size of the field.
    /// </summary>
    [Parameter] public TextBoxSize Size { get; set; } = TextBoxSize.ExtraSmall;

    /// <summary>
    /// Debounce delay in milliseconds before propagating value changes while typing.
    /// </summary>
    [Parameter] public int Debounce { get; set; } = 300;

    /// <summary>
    /// Trigger <see cref="ValueChanged" /> as soon as the input value changes (oninput event).
    /// Set to false to only trigger when the user leaves the field or presses enter (onchange event).
    /// </summary>
    /// <remarks>
    /// This is ignored if there's a validation error, which will trigger value changed more often to clear the error asap.
    /// </remarks>
    [Parameter] public bool ValueChangedOnInput { get; set; } = true;

    /// <summary>
    /// Manually marks the field as having validation errors, overriding the EditContext validation state.
    /// </summary>
    [Parameter] public bool Error { get; set; }

    /// <summary>
    /// Optional culture name to use instead of the current culture (e.g. "sv-SE").
    /// Used for testing.
    /// </summary>
    [Parameter] public string? Culture { get; set; }

    /// <summary>
    /// Maximum number of digits allowed before the decimal separator.
    /// </summary>
    [Parameter] public int MaxNumberOfDigits { get; set; } = 5;

    /// <summary>
    /// Allows negative numbers if set to true.
    /// </summary>
    [Parameter] public bool AllowNegativeNumbers { get; set; } = false;

    /// <summary>
    /// Maximum number of digits allowed after the decimal separator.
    /// </summary>
    [Parameter] public int? MaxDecimals { get; set; } = null;

    #endregion

    private IJSObjectReference? _domUtils;

    private ElementReference? _inputElementRef;
    private FieldIdentifier? _fieldIdentifier;
    private bool _debounced;
    private bool _error;
    private bool _preventEnterKey;
    private bool _hasBeenSetFromExternalValue;
    private CultureInfo _culture = CultureInfo.CurrentCulture;
    private static readonly Regex _integerPattern = IntegerPattern();
    private static readonly Regex _decimalPattern = DecimalPattern();
    private CancellationTokenSource _cts = new();
    private CancellationToken _ct;
    private string? _currentTextValue;
    private TValue? _currentParsedValue;

    private string CssClasses => new CssBuilder("ph-numberfield")
        .AddClass("ph-numberfield--error", _error)
        .AddClass("ph-numberfield--size-xsmall", Size == TextBoxSize.ExtraSmall)
        .AddClass("ph-numberfield--size-small", Size == TextBoxSize.Small)
        .AddClass("ph-numberfield--size-medium", Size == TextBoxSize.Medium)
        .AddClass("ph-numberfield--size-large", Size == TextBoxSize.Large)
        .Build();

    protected string? AutomatedTestId { get; set; }
    protected Expression<Func<object>>? ValidationForExpression { get; set; }
    protected bool HasLabel => !string.IsNullOrEmpty(Label);
    protected bool HasErrorText => _error && !string.IsNullOrEmpty(ErrorText);

    private string? TextValue
    {
        get => _currentTextValue;
        set
        {
            if (_currentTextValue != value)
            {
                var inputText = value ?? "";

                // Strip negative sign if negative numbers are not allowed
                if (!AllowNegativeNumbers && inputText.StartsWith('-'))
                {
                    inputText = inputText.TrimStart('-');
                }

                // Apply constraints when MaxNumberOfDigits or MaxDecimals is configured
                if (MaxNumberOfDigits > 0 || (!IsInteger && MaxDecimals.HasValue))
                {
                    inputText = ApplyMaxDigitsConstraint(inputText);
                }

                var regex = IsInteger ? _integerPattern : _decimalPattern;

                // trim down to allowed characters
                var match = regex.Match(inputText);
                inputText = match.Success ? match.Value : "";

                if (_currentTextValue != inputText)
                {
                    _currentTextValue = inputText;
                    var number = ParseValue(_currentTextValue);
                    if (!EqualityComparer<TValue>.Default.Equals(number, _currentParsedValue))
                    {
                        _currentParsedValue = number;
                        _ = NotifyInput(_currentParsedValue);
                    }
                }
            }
        }
    }

    private string ApplyMaxDigitsConstraint(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        // Use MaxDecimals if set (only for non-integer types), otherwise fall back to culture default
        var numberOfDecimals = !IsInteger && MaxDecimals.HasValue ? MaxDecimals.Value : _culture.NumberFormat.NumberDecimalDigits;

        // Handle negative sign
        var isNegative = input.StartsWith('-');
        var workingInput = isNegative ? input[1..] : input;

        // Find decimal separator position
        var decimalIndex = workingInput.IndexOfAny(['.', ',']);
        var integerPart = decimalIndex >= 0 ? workingInput[..decimalIndex] : workingInput;
        var decimalPart = decimalIndex >= 0 ? workingInput[(decimalIndex + 1)..] : "";

        // Enforce MaxNumberOfDigits constraint on integer part
        if (MaxNumberOfDigits > 0 && integerPart.Length > MaxNumberOfDigits)
        {
            integerPart = integerPart[..MaxNumberOfDigits];
        }

        // Enforce numberOfDecimals constraint on decimal part
        if (!string.IsNullOrEmpty(decimalPart) && decimalPart.Length > numberOfDecimals)
        {
            decimalPart = decimalPart[..numberOfDecimals];
        }

        // Reconstruct the string
        if (decimalIndex >= 0)
        {
            var decimalSeparator = workingInput[decimalIndex];
            workingInput = integerPart + decimalSeparator + decimalPart;
        }
        else
        {
            workingInput = integerPart;
        }

        return isNegative ? "-" + workingInput : workingInput;
    }

    private string InputMode => IsInteger ? "numeric" : "decimal";

    private bool IsInteger { get; set; }

    private bool IsNullable { get; set; }

    protected override void OnInitialized()
    {
        IsInteger = CheckIsInteger();
        IsNullable = CheckIsNullable();
    }

    protected override void OnParametersSet()
    {
        if (!string.IsNullOrWhiteSpace(Culture))
        {
            try
            {
                _culture = CultureInfo.GetCultureInfo(Culture);
            }
            catch (CultureNotFoundException)
            {
                _culture = CultureInfo.CurrentCulture;
            }
        }
        else
        {
            _culture = CultureInfo.CurrentCulture;
        }

        // Update internal value only when external Value changes (not when user types)
        // Use flag to handle first load correctly for all values including zero
        var shouldUpdate = !_hasBeenSetFromExternalValue ||
                          !object.Equals(Value, _currentParsedValue);

        if (shouldUpdate)
        {
            _currentParsedValue = Value;
            SetTextValue(_currentParsedValue);
            _hasBeenSetFromExternalValue = true;
        }

        if (!_fieldIdentifier.HasValue && ValueExpression is not null)
        {
            _fieldIdentifier = FieldIdentifier.Create(ValueExpression);
            AutomatedTestId = this.CreateAutomatedTestId(_fieldIdentifier.Value);
        }

        if (DisplayOnly)
        {
            // Cancel any pending debounced callbacks so they don't fire after switching to display-only.
            _cts.Cancel();
            _debounced = false;

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

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _domUtils = await ImportCommonUtilityJSAsync("domUtils.js");
        }
    }

    private async Task OnChange(ChangeEventArgs args)
    {
        SetValue(true);

        // make sure there aren't any delayed changes coming from debounce after onchange was triggered
        _cts.Cancel();
        _debounced = false;
        await NotifyChange(_currentParsedValue);

        TriggerRevalidation();
    }

    private async Task OnBlur(FocusEventArgs e)
    {
        if (_debounced && !DisplayOnly)
        {
            // Trigger validation in case the value was changed via the oninput event (bypassing onchange)
            TriggerRevalidation();
        }

        await Task.CompletedTask;
    }

    private async Task BlurOnEnter(KeyboardEventArgs args)
    {
        // Prevent enter key from submitting the form
        // Note: this only works in WASM
        _preventEnterKey = args.Key == "Enter";

        if (args.Key == "Enter")
        {
            await BlurAsync();
        }
    }

    private async Task NotifyInput(TValue? value)
    {
        if (!ValueChangedOnInput && !_error)
        {
            return;
        }

        _cts.Cancel();
        _cts.Dispose();
        _cts = new CancellationTokenSource();
        _ct = _cts.Token;

        try
        {
            await Task.Delay(Debounce, _ct);

            await InvokeAsync(async () => // needed to allow InvokeAsync in server side blazor
            {
                _debounced = true;
                await NotifyChange(value);

                if (_error)
                {
                    // revalidate to clear the validation error asap
                    TriggerRevalidation();
                    await InvokeAsync(StateHasChanged);
                }
            });
        }
        catch (TaskCanceledException)
        {
            // Expected when user types before debounce completes
        }

    }

    private async Task NotifyChange(TValue? value)
    {
        if (DisplayOnly)
        {
            return;
        }

        // Notify the parent component about the value change
        if (ValueChanged.HasDelegate)
        {
            await ValueChanged.InvokeAsync(value);
        }
    }

    private void UpdateValidationError()
    {
        if (!string.IsNullOrEmpty(ErrorText))
        {
            // Manual error text takes precedence
            _error = true;
        }
        else if (!Error && CascadedEditContext is not null && _fieldIdentifier.HasValue)
        {
            // Automatic validation from EditContext
            _error = !CascadedEditContext.IsValid(_fieldIdentifier.Value);
        }
        else
        {
            // Manual error state
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

    private void SetValue(bool setValueEvenIfNull = false)
    {
        _currentParsedValue = ParseValue(TextValue);
        if (_currentParsedValue == null && !IsNullable && setValueEvenIfNull)
        {
            _currentParsedValue = default;
        }

        SetTextValue(_currentParsedValue);
    }

    /// <summary>
    /// Format the text value without changing the current parsed value.
    /// </summary>
    /// <param name="value">Number value</param>
    private void SetTextValue(TValue? value)
    {
        _currentTextValue = value == null
            ? ""
            : string.Format(_culture, "{0:0.##########;-0.##########;0}", value);
    }

    private TValue? ParseValue(string? textValue)
    {
        if (string.IsNullOrEmpty(textValue))
        {
            return default;
        }

        // parse with invariant culture decimal separator
        textValue = textValue.Replace(',', '.');

        if (IsInteger)
        {
            if (int.TryParse(textValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integerValue))
            {
                return ChangeType<TValue?>(integerValue);
            }
        }
        else if (double.TryParse(textValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var decimalValue))
        {
            return ChangeType<TValue?>(decimalValue);
        }

        return default;
    }

    private async Task BlurAsync()
    {
        if (_inputElementRef is not null && _domUtils is not null)
        {
            await _domUtils.InvokeVoidAsync("blur", _inputElementRef.Value);
        }
    }

    public override async ValueTask DisposeAsync()
    {
        _cts.Dispose();
        await _domUtils.SafeDisposeAsync();

        GC.SuppressFinalize(this);
    }

    public T? ChangeType<T>(object? value)
    {
        var t = typeof(T?);

        if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            if (value == null)
            {
                return default;
            }

            t = Nullable.GetUnderlyingType(t);
        }

        if (t == null)
        {
            return default;
        }

        return (T?)Convert.ChangeType(value, t, CultureInfo.InvariantCulture);
    }

    private static bool CheckIsInteger()
    {
        return typeof(TValue) == typeof(int)
            || typeof(TValue) == typeof(long)
            || typeof(TValue) == typeof(short)
            || typeof(TValue) == typeof(int?)
            || typeof(TValue) == typeof(long?)
            || typeof(TValue) == typeof(short?);
    }

    private static bool CheckIsNullable()
    {
        return typeof(TValue).IsGenericType && typeof(TValue).GetGenericTypeDefinition() == typeof(Nullable<>);
    }

    [GeneratedRegex(@"^[-]?[\d]*")]
    private static partial Regex IntegerPattern();

    [GeneratedRegex(@"^[-]?[\d]*[\.,]?[\d]*")]
    private static partial Regex DecimalPattern();
}
