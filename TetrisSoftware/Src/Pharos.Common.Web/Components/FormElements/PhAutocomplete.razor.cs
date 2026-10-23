using System.Globalization;
using System.Timers;
using Microsoft.JSInterop;
using Pharos.Common.Web.Components.FormElements.Enums;
using Pharos.Common.Web.Components.Popover;
using Pharos.Common.Web.Extensions;
using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.FormElements;

public partial class PhAutocomplete<TValue>
{
    [Parameter] public int Debounce { get; set; } = 300;
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public bool Error { get; set; }
    [Parameter] public string PlaceHolder { get; set; } = "";
    [Parameter] public int MaximumSuggestions { get; set; } = 3;
    [Parameter] public int MinimumLength { get; set; } = 1;

    [Parameter] public TValue? Value { get; set; }
    [Parameter] public EventCallback<TValue?> ValueChanged { get; set; }

    /// <summary>
    /// Optional. Gets the text displayed for the selected value.
    /// Defaults to Row1Text, Row2Text, or "Row1, Row2" when both are available.
    /// </summary>
    [Parameter] public Func<TValue, string>? SelectedText { get; set; }
    [Parameter, EditorRequired] public Func<TValue, string?> Row1Text { get; set; } = null!;
    [Parameter] public Func<TValue, string?>? Row2Text { get; set; }
    [Parameter, EditorRequired] public Func<string, Task<Tuple<IEnumerable<TValue>, int>>> Result { get; set; } = null!;
    [Parameter] public TextBoxSize Size { get; set; } = TextBoxSize.Medium;

    [Parameter] public string NoResultsText { get; set; } = string.Empty;
    [Parameter] public string FooterTextFormat { get; set; } = string.Empty;

    private System.Timers.Timer? _debounceTimer;
    private ElementReference _inputRef;
    private PhPopover? _popover;
    private bool _showPopoverRequested;
    private bool _hidePopoverRequested;
    private string _lastValueText = string.Empty;
    private bool _hasFocus;
    private IJSObjectReference? _jsUtils;

    private string SearchText { get; set; } = string.Empty;
    private int SelectedIndex { get; set; }
    private TValue[] ResultList { get; set; } = Array.Empty<TValue>();
    private int ResultTotalCount { get; set; }

    public override async ValueTask DisposeAsync()
    {
        await _jsUtils.SafeDisposeAsync();
        _debounceTimer?.Dispose();
        GC.SuppressFinalize(this);
    }

    protected string Classname => new CssBuilder("ph-autocomplete")
        .AddClass("ph-autocomplete--error", Error)
        .AddClass("ph-autocomplete--has-text", !string.IsNullOrEmpty(SearchText))
        .AddClass("ph-autocomplete--size-xsmall", Size == TextBoxSize.ExtraSmall)
        .AddClass("ph-autocomplete--size-small", Size == TextBoxSize.Small)
        .AddClass("ph-autocomplete--size-medium", Size == TextBoxSize.Medium)
        .AddClass("ph-autocomplete--size-large", Size == TextBoxSize.Large)
        .AddClass("ph-autocomplete--size-xlarge", Size == TextBoxSize.ExtraLarge)
        .Build();

    private bool ShowNotFound => ResultList.Length == 0;

    private string FooterText => string.IsNullOrWhiteSpace(FooterTextFormat) ? string.Empty : FooterTextFormat
                                                                                             .Replace("{0}", ResultList.Length.ToString(CultureInfo.CurrentCulture))
                                                                                             .Replace("{1}", ResultTotalCount.ToString(CultureInfo.CurrentCulture));

    protected override void OnInitialized()
    {
        if (Result is null)
        {
            throw new InvalidOperationException($"{GetType()} requires a {nameof(Result)} parameter.");
        }

        if (Row1Text is null)
        {
            throw new InvalidOperationException($"{GetType()} requires a {nameof(Row1Text)} parameter.");
        }

        _debounceTimer = new System.Timers.Timer
        {
            Interval = Debounce,
            AutoReset = false
        };
        _debounceTimer.Elapsed += Search!;
    }

    protected override void OnParametersSet()
    {
        var text = GetSelectedText(Value);

        // Avoid overwriting user input unless the value changed
        if (!_hasFocus || text != _lastValueText)
        {
            SearchText = text;
            _lastValueText = text;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _jsUtils = await ImportCommonUtilityJSAsync("domUtils.js");
        }

        if (_popover is null)
        {
            return;
        }

        if (_hidePopoverRequested)
        {
            _hidePopoverRequested = false;
            await _popover.Hide();
        }
        else if (_showPopoverRequested)
        {
            _showPopoverRequested = false;
            await _popover.Show();
        }
    }

    private void RequestShowPopover()
    {
        _showPopoverRequested = true;
        _hidePopoverRequested = false;
    }

    private void RequestHidePopover()
    {
        _hidePopoverRequested = true;
        _showPopoverRequested = false;
    }

    private void SetOpenState(bool isOpen)
    {
        if (isOpen)
        {
            return;
        }

        _debounceTimer?.Stop();
    }

    private string? Row1(TValue item)
    {
        return Row1Text?.Invoke(item);
    }

    private string? Row2(TValue item)
    {
        return Row2Text?.Invoke(item);
    }

    private string GetSelectedText(TValue? item)
    {
        if (item is null)
        {
            return string.Empty;
        }

        // Use SelectedText if provided (no fallback)
        if (SelectedText is not null)
        {
            return SelectedText.Invoke(item) ?? string.Empty;
        }

        // Otherwise fall back to Row1/Row2
        var row1 = Row1(item);
        var row2 = Row2(item);

        if (!string.IsNullOrWhiteSpace(row1) && !string.IsNullOrWhiteSpace(row2))
        {
            return $"{row1}, {row2}";
        }

        if (!string.IsNullOrWhiteSpace(row1))
        {
            return row1;
        }

        if (!string.IsNullOrWhiteSpace(row2))
        {
            return row2;
        }

        return string.Empty;
    }

    private string GetSelectedSuggestionClass(int index)
    {
        return index == SelectedIndex
            ? "ph-autocomplete__dropdown-row-active-item"
            : string.Empty;
    }

    private Task OnInput(ChangeEventArgs e)
    {
        SearchText = e.Value?.ToString() ?? string.Empty;
        return HandleTimer();
    }

    private void HandleFocusIn()
    {
        _hasFocus = true;
    }

    private void HandleFocusOut()
    {
        _hasFocus = false;
    }

    private async Task HandleKeyup(KeyboardEventArgs args)
    {
        switch (args.Key)
        {
            case "ArrowDown":
                MoveSelection(1);
                break;
            case "ArrowUp":
                MoveSelection(-1);
                break;
            case "Escape":
                await RevertInputToValue();
                break;
            case "Enter" when ResultList.Length == 1:
                await SelectTheFirstAndOnlySuggestion();
                break;
            case "Enter" when SelectedIndex >= 0 && SelectedIndex < ResultList.Length:
                await SelectResult(ResultList[SelectedIndex]);
                break;
        }
    }

    private async Task HandleTimer(bool stopTimer = false)
    {
        if (SearchText.Length == 0)
        {
            _debounceTimer?.Stop();

            await ValueChanged.InvokeAsync(default);
            await ClearResults();
            return;
        }

        if (stopTimer)
        {
            _debounceTimer?.Stop();
            SelectedIndex = -1;
            RequestHidePopover();
            await InvokeAsync(StateHasChanged);
        }
        else
        {
            _debounceTimer?.Stop();
            _debounceTimer?.Start();
        }
    }

    private void MoveSelection(int count)
    {
        var index = SelectedIndex + count;

        if (index >= ResultList.Length)
        {
            index = 0;
        }

        if (index < 0)
        {
            index = ResultList.Length - 1;
        }

        SelectedIndex = index;
    }

    private async Task RevertInputToValue()
    {
        _debounceTimer?.Stop();
        SearchText = GetSelectedText(Value);
        await ClearResults();
    }

    private async void Search(object source, ElapsedEventArgs e)
    {
        // when search text is empty or does not meet the minimum length,
        // clear the suggested items and show the "No results found" message.
        if (string.IsNullOrWhiteSpace(SearchText) || SearchText.Length < MinimumLength)
        {
            await ClearResults();
            return;
        }

        var result = await Result?.Invoke(SearchText)!;
        ResultList = result.Item1.Take(MaximumSuggestions).ToArray();
        ResultTotalCount = result.Item2;

        SelectedIndex = -1;

        RequestShowPopover();
        await InvokeAsync(StateHasChanged);
    }

    private async Task ClearResults()
    {
        ResultList = [];
        ResultTotalCount = 0;
        SelectedIndex = -1;

        RequestHidePopover();
        await InvokeAsync(StateHasChanged);
    }

    private async Task ResetInput()
    {
        _debounceTimer?.Stop();
        SearchText = string.Empty;
        await ValueChanged.InvokeAsync(default);
        await ClearResults();
        await _inputRef.FocusAsync();
    }

    private async Task SelectResult(TValue item)
    {
        if (_jsUtils is not null)
        {
            // Blur the input to prevent the virtual keyboard from reopening after selection.
            // Needed for keyboard selection (Enter). Pointer interactions are handled via @onclick:preventDefault.
            await _jsUtils.InvokeVoidAsync("blur", _inputRef);
        }

        await HandleTimer(stopTimer: true);

        SearchText = GetSelectedText(item);

        await ValueChanged.InvokeAsync(item);
    }

    private Task SelectTheFirstAndOnlySuggestion()
    {
        SelectedIndex = 0;
        return SelectResult(ResultList[SelectedIndex]);
    }

    private MarkupString Highlight(string? text)
    {
        return TextHighlighter.Highlight(text, SearchText, "ph-autocomplete__highlight");
    }
}