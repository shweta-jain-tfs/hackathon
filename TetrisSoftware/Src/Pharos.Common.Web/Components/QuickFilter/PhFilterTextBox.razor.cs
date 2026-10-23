using Microsoft.JSInterop;
using Pharos.Common.Web.Extensions;
using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.QuickFilter;

public partial class PhFilterTextBox
{
    [Parameter] public string? Id { get; set; }
    [Parameter] public string? UiName { get; set; }
    [Parameter] public string? Placeholder { get; set; }
    [Parameter] public string? Value { get; set; }
    [Parameter] public EventCallback<string?> ValueChanged { get; set; }
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public int Debounce { get; set; } = 300;
    [Parameter] public int? MaxLength { get; set; } = 50;
    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    private IJSObjectReference? _domUtils;
    private ElementReference _inputElementRef;
    private readonly Debouncer<string?> _debouncer = new();
    private string? _currentValue;
    private bool _preventEscapeKey;

    private string CssClasses => new CssBuilder("ph-filter-textbox")
        .AddClass("ph-filter-textbox--has-text", !string.IsNullOrEmpty(_currentValue))
        .Build();

    protected override void OnParametersSet()
    {
        if (!_debouncer.IsDebouncing)
        {
            _currentValue = Value;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _domUtils = await ImportCommonUtilityJSAsync("domUtils.js");
        }
    }

    private async Task OnInputChange(ChangeEventArgs args)
    {
        _currentValue = args.Value?.ToString() ?? string.Empty;

        await _debouncer.InvokeAsync(
            _currentValue,
            Debounce,
            async value =>
            {
                await InvokeAsync(async () =>
                {
                    if (value != _currentValue)
                    {
                        return;
                    }

                    var collapsedWhitespace = !string.IsNullOrEmpty(value) && string.IsNullOrWhiteSpace(value);
                    if (collapsedWhitespace)
                    {
                        value = string.Empty;
                        _currentValue = string.Empty;
                    }

                    if (value != Value)
                    {
                        await ValueChanged.InvokeAsync(value);
                    }
                });
            });
    }

    private async Task ResetInput()
    {
        await _debouncer.CancelAsync();
        _currentValue = string.Empty;
        await ValueChanged.InvokeAsync(string.Empty);
        await _inputElementRef.FocusAsync();
    }

    private async Task BlurAsync()
    {
        if (_domUtils is not null)
        {
            await _domUtils.InvokeVoidAsync("blur", _inputElementRef);
        }
    }

    private async Task HandleKeyDown(KeyboardEventArgs args)
    {
        _preventEscapeKey = false;
        switch (args.Key)
        {
            case "Enter":
                await BlurAsync();
                break;

            case "Escape":
                _preventEscapeKey = true; // avoid closing modal
                if (!string.IsNullOrEmpty(_currentValue))
                {
                    await ResetInput();
                }
                else
                {
                    await BlurAsync();
                }
                break;
        }
    }

    public override async ValueTask DisposeAsync()
    {
        await _debouncer.DisposeAsync();
        await _domUtils.SafeDisposeAsync();

        GC.SuppressFinalize(this);
    }
}