using System.Linq.Expressions;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Pharos.Common.Web.Components.FormElements.Enums;
using Pharos.Common.Web.Components.Label;
using Pharos.Common.Web.Extensions;
using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.FormElements;

/// <summary>
/// The standard text input field used throughout the Pharos UI.
/// Supports labels, help text, placeholders, validation, and optional formatting behavior.
/// </summary>
public partial class PhTextBox
{
    [CascadingParameter] public EditContext? CascadedEditContext { get; set; }
    [Parameter] public string? Id { get; set; }
    [Parameter] public string Label { get; set; } = "";
    [Parameter] public string? PlaceHolder { get; set; }
    [Parameter] public string? Value { get; set; }
    [Parameter] public int? MaxLength { get; set; }
    [Parameter] public Expression<Func<string?>>? ValueExpression { get; set; }
    [Parameter] public EventCallback<string?> ValueChanged { get; set; }
    [Parameter] public EventCallback<string?> OnInput { get; set; }
    [Parameter] public EventCallback<FocusEventArgs> OnFocus { get; set; }
    [Parameter] public TextBoxType Type { get; set; } = TextBoxType.Text;
    [Parameter] public TextBoxSize Size { get; set; } = TextBoxSize.Medium;
    [Parameter] public bool Disabled { get; set; }
    /// <summary>
    /// Renders the component as display-only (non-interactive, no automatic validation).
    /// Overrides <see cref="Disabled"/>, but keeps the disabled label and value color.
    /// </summary>
    [Parameter] public bool DisplayOnly { get; set; }
    [Parameter] public int Debounce { get; set; } = 300;
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

    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    private IJSObjectReference? _domUtils;

    private InputText? _inputElementRef;
    private FieldIdentifier? _fieldIdentifier;
    string? _lastValue;
    private bool _preventEnterKey;
    private bool _debounced;
    private bool _error;

    protected string Classname => new CssBuilder("ph-textbox")
        .AddClass("ph-textbox--error", _error)
        .AddClass("ph-textbox--size-xsmall", Size == TextBoxSize.ExtraSmall)
        .AddClass("ph-textbox--size-small", Size == TextBoxSize.Small)
        .AddClass("ph-textbox--size-medium", Size == TextBoxSize.Medium)
        .AddClass("ph-textbox--size-large", Size == TextBoxSize.Large)
        .AddClass("ph-textbox--size-xlarge", Size == TextBoxSize.ExtraLarge)
        .Build();

    protected string? AutomatedTestId { get; set; }
    protected Expression<Func<object>>? ValidationForExpression { get; set; }
    protected bool HasLabel => !string.IsNullOrEmpty(Label);
    protected bool HasErrorText => _error && !string.IsNullOrEmpty(ErrorText);

    protected override void OnParametersSet()
    {
        if (DisplayOnly)
        {
            // Cancel any pending debounced callbacks so they don't fire after switching to display-only.
            _cts.Cancel();
            _debounced = false;
        }

        if (_debounced)
        {
            // ignore values set while debounced (oninput) to avoid trimming text as the user types
            Value = _lastValue;
        }
        else
        {
            _lastValue = Value;
        }

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

        _preventEnterKey = false;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _domUtils = await ImportCommonUtilityJSAsync("domUtils.js");
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

    public async Task FocusAsync()
    {
        if (_inputElementRef?.Element != null)
        {
            await _inputElementRef.Element.Value.FocusAsync();
        }
    }

    private async Task BlurAsync()
    {
        if (_inputElementRef?.Element is not null && _domUtils is not null)
        {
            await _domUtils.InvokeVoidAsync("blur", _inputElementRef.Element);
        }
    }

    private async Task OnInputChange(ChangeEventArgs args)
    {
        if (ValueChanged.HasDelegate || OnInput.HasDelegate)
        {
            await OnValueChangedWithDebounce(args, Debounce);
        }
    }

    private async Task OnInputFocus(FocusEventArgs e)
    {
        if (OnFocus.HasDelegate)
        {
            await OnFocus.InvokeAsync(e);
        }
    }

    private CancellationTokenSource _cts = new();

    private CancellationToken _ct;

    private async Task OnValueChangedWithDebounce(ChangeEventArgs e, int debounce)
    {
        _cts.Cancel();
        _cts = new CancellationTokenSource();
        _ct = _cts.Token;

        try
        {
            await Task.Delay(debounce, _ct);

            await InvokeAsync(async () =>
            {
                _debounced = true;

                var newValue = FixValue(e?.Value?.ToString());

                if (ValueChanged.HasDelegate)
                {
                    await ValueChanged.InvokeAsync(newValue);
                }

                if (OnInput.HasDelegate)
                {
                    await OnInput.InvokeAsync(newValue);
                }

                // Trigger validation to clear the error as soon as possible
                if (_error && _fieldIdentifier.HasValue)
                {
                    CascadedEditContext?.NotifyFieldChanged(_fieldIdentifier.Value);
                    UpdateValidationError();
                }

            });
        }
        catch (TaskCanceledException)
        {
            // Expected when user types before debounce completes
        }
    }

    private async Task OnValueChanged(string? newValue)
    {
        // make sure there aren't any delayed changes coming from debounce after onchange was triggered
        _cts.Cancel();
        _debounced = false;

        newValue = FixValue(newValue);

        // Update the Value property
        Value = newValue;

        // Notify the parent component about the value change
        if (ValueChanged.HasDelegate)
        {
            await ValueChanged.InvokeAsync(Value);
        }

        // Notify any additional input event handlers (mainly for searching)
        if (OnInput.HasDelegate)
        {
            await OnInput.InvokeAsync(Value);
        }
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

    private static string? FixValue(string? value)
    {
        return value?.Trim();
    }

    public override async ValueTask DisposeAsync()
    {
        _cts.Dispose();
        await _domUtils.SafeDisposeAsync();

        GC.SuppressFinalize(this);
    }
}