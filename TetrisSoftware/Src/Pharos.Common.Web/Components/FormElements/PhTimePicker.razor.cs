using System.Globalization;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Pharos.Common.Web.Components.FormElements.Enums;
using Pharos.Common.Web.Components.Label;
using Pharos.Common.Web.Components.Popover;
using Pharos.Common.Web.Extensions;
using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.FormElements;

/// <summary>
/// Time input component. Time can be entered via keyboard or via selection in a dropdown.
/// The component is culture aware and will show the time in the current culture format.
/// </summary>
public partial class PhTimePicker
{
    [CascadingParameter] public EditContext? CascadedEditContext { get; set; }
    [Parameter] public string? Id { get; set; }
    [Parameter] public TimeOnly? Value { get; set; }
    [Parameter] public Expression<Func<TimeOnly?>>? ValueExpression { get; set; }
    [Parameter] public EventCallback<TimeOnly?> ValueChanged { get; set; }
    [Parameter] public int MinuteInterval { get; set; } = _defaultMinuteInterval;
    [Parameter] public bool Disabled { get; set; }
    /// <summary>
    /// Renders the component as display-only (non-interactive, no automatic validation).
    /// Overrides <see cref="Disabled"/>, but keeps the disabled label and value color.
    /// </summary>
    [Parameter] public bool DisplayOnly { get; set; }
    [Parameter] public bool Error { get; set; }
    [Parameter] public TimePickerSize Size { get; set; } = TimePickerSize.SmallInputWidth;
    [Parameter] public bool IncludeNull { get; set; } = true;

    /// <summary>
    /// Optional culture to override the UI culture. Mostly for testing.
    /// </summary>
    [Parameter] public string? Culture { get; set; }

    // Add PhLabel support parameters
    [Parameter] public string Label { get; set; } = "";
    /// <summary>
    /// Error text to display when the field has validation errors.
    /// </summary>
    [Parameter] public string? ErrorText { get; set; }
    /// <summary>
    /// Help text to display below the time picker field.
    /// </summary>
    [Parameter] public string? HelpText { get; set; } = "";

    private IJSObjectReference? _domUtils;

    private static readonly Regex _hour24Pattern = StartsWithHourPattern();
    private static readonly Regex _minutePattern = StartsWithMinutePattern();
    private const int _defaultMinuteInterval = 30;

    private ElementReference? _inputMinuteRef;
    private ElementReference? _inputHourRef;
    private PhPopover? _popoverRef;
    private bool _blurMinuteField;
    private bool _moveToMinuteField;
    private bool _moveToHourField;
    private bool _preventEnterKey;

    protected string? AutomatedTestId { get; set; }
    private bool PostMeridian { get; set; }
    private string Am { get; set; } = "am";
    private string Pm { get; set; } = "pm";
    private string TimeSeparator { get; set; } = ":";
    private string TrimmedTimeFormat { get; set; } = "HH:mm";
    private string FullTimeFormat { get; set; } = "HH:mm";
    private List<TimeOnly?> TimeOptions { get; set; } = new();

    private CultureInfo _culture = CultureInfo.CurrentCulture;
    private bool _use24HourClock;

    private FieldIdentifier? _fieldIdentifier;
    private TimeOnly? _editedTime;
    private bool _isOpen;
    private bool _error;

    // Add PhLabel support properties
    protected Expression<Func<object>>? ValidationForExpression { get; set; }
    protected bool HasLabel => !string.IsNullOrEmpty(Label);
    protected bool HasErrorText => _error && !string.IsNullOrEmpty(ErrorText);

    private string ClassNames => new CssBuilder("ph-time-picker")
            .AddClass("ph-time-picker--disabled", Disabled)
            .AddClass("ph-time-picker--error", _error)
            .AddClass("ph-time-picker--size-small", Size == TimePickerSize.SmallInputWidth)
            .AddClass("ph-time-picker--size-medium", Size == TimePickerSize.MediumInputWidth)
            .AddClass("ph-time-picker--size-large", Size == TimePickerSize.LargeInputWidth)
            .AddClass("ph-time-picker--open", _isOpen)
            .Build();


    private string? _hour;
    private string? Hour
    {
        get => _hour;
        set
        {
            if (value != _hour && TryFormatHour24(value, out _hour))
            {
                if (IsCompletedHourInput(_hour))
                {
                    _moveToMinuteField = true;
                }

                _hour = FormatHourRegional(_hour);
            }

            TrySetTime();
        }
    }

    private string? _minute;
    private string? Minute
    {
        get => _minute; set
        {
            TryFormatMinute(value, out _minute);
            var isValidTime = TrySetTime() && IsCompletedMinuteInput(_minute);
            var isEmpty = string.IsNullOrEmpty(_minute) && string.IsNullOrEmpty(_hour);
            if (isValidTime || isEmpty)
            {
                NotifyValueChanged();
            }
        }
    }

    protected override void OnParametersSet()
    {
        _preventEnterKey = false;
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

        var formatInfo = _culture.DateTimeFormat;
        SetTimeFormat(formatInfo);
        TimeOptions = CreateTimeOptions(MinuteInterval, IncludeNull);

        _editedTime = Value;
        ReformatInputs(_editedTime, true);

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

            // Add validation expression setup for PhLabel
            if (HasLabel && ValueExpression is not null && ValidationForExpression is null)
            {
                ValidationForExpression = PhLabel.CreateValidationForExpression(ValueExpression);
            }
        }
    }

    private static List<TimeOnly?> CreateTimeOptions(int minuteInterval, bool includeNull)
    {
        var items = new List<TimeOnly?>();
        if (minuteInterval is < 5 or > 60)
        {
            minuteInterval = _defaultMinuteInterval;
        }

        if (includeNull)
        {
            items.Add(null);
        }

        for (var hour = 0; hour < 24; hour++)
        {
            for (var minute = 0; minute < 60; minute += minuteInterval)
            {
                items.Add(new TimeOnly(hour, minute));
            }
        }

        return items;
    }

    private void SetTimeFormat(DateTimeFormatInfo formatInfo)
    {
        Am = formatInfo.AMDesignator;
        Pm = formatInfo.PMDesignator;
        TimeSeparator = formatInfo.TimeSeparator;

        FullTimeFormat = formatInfo.ShortTimePattern;
        _use24HourClock = FullTimeFormat.Contains('H');

        // Ignore am/pm
        TrimmedTimeFormat = FullTimeFormat.TrimEnd(' ', 't');
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _domUtils = await ImportCommonUtilityJSAsync("domUtils.js");
        }

        if (_moveToMinuteField && _inputMinuteRef.HasValue)
        {
            _preventEnterKey = false;
            _moveToMinuteField = false;
            await _inputMinuteRef.Value.FocusAsync();
        }
        else if (_moveToHourField && _inputHourRef.HasValue)
        {
            _moveToHourField = false;
            await _inputHourRef.Value.FocusAsync();
        }
        else if (_blurMinuteField && _inputMinuteRef.HasValue && _domUtils is not null)
        {
            _blurMinuteField = false;
            await _domUtils.InvokeVoidAsync("blur", _inputMinuteRef.Value);
        }
    }

    private void SetOpenState(bool value)
    {
        // Set open state from pressing the toggle button and soft dismiss
        _isOpen = value;
    }

    private async Task SelectTime(TimeOnly? value)
    {
        await CloseDropdown();
        _editedTime = value;
        ReformatInputs(_editedTime, true);
        NotifyValueChanged();
    }

    private void HourKeyPress(KeyboardEventArgs args)
    {
        if (IsBackspaceOrDelete(args))
        {
            Hour = string.Empty;
        }
        else if (args.Key is "ArrowRight" or "Enter" or " " or "." or ":")
        {
            _moveToMinuteField = true;
        }

        PreventFormPostOnEnter(args);
    }

    private void HourNumKeyPress(KeyboardEventArgs args)
    {
        if (IsNumericKey(args))
        {
            // Reset hour input if two digits have already been typed in (note: only works in WASM)
            if (Hour?.Length == 2)
            {
                _hour = string.Empty;
            }
        }
    }

    private void MinuteKeyPress(KeyboardEventArgs args)
    {
        if (IsBackspaceOrDelete(args))
        {
            if (string.IsNullOrEmpty(Minute) && !string.IsNullOrEmpty(Hour))
            {
                Hour = string.Empty;
                _moveToHourField = true;
            }
            else
            {
                Minute = string.Empty;
            }
        }
        else if (args.Key is "ArrowLeft")
        {
            _moveToHourField = true;
        }
        else if (args.Key is "Enter")
        {
            _blurMinuteField = true;
            if (string.IsNullOrEmpty(_minute) && !string.IsNullOrEmpty(_hour))
            {
                Minute = "00";
            }
        }

        PreventFormPostOnEnter(args);
    }

    private void MinuteNumKeyPress(KeyboardEventArgs args)
    {
        if (IsNumericKey(args))
        {
            // Reset minute input if two digits have already been typed in (note: only works in WASM)
            if (Minute?.Length == 2)
            {
                _minute = string.Empty;
            }
        }
    }

    private void TogglePm()
    {
        PostMeridian = !PostMeridian;
        if (TrySetTime())
        {
            NotifyValueChanged();
        }
    }

    private void FieldBlur()
    {
        var bothFieldsEmpty = string.IsNullOrEmpty(Hour) && string.IsNullOrEmpty(Minute);
        if (_editedTime.HasValue || bothFieldsEmpty)
        {
            ReformatInputs(_editedTime);
            NotifyValueChanged();
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

    private void NotifyValueChanged()
    {
        if (DisplayOnly)
        {
            return;
        }

        if (ValueChanged.HasDelegate && _editedTime != Value)
        {
            ValueChanged.InvokeAsync(_editedTime);

            if (_fieldIdentifier.HasValue)
            {
                CascadedEditContext?.NotifyFieldChanged(_fieldIdentifier.Value);
                UpdateValidationError();
            }
        }
    }

    private static bool IsBackspaceOrDelete(KeyboardEventArgs args)
    {
        return args.Key is "Backspace" or "Delete";
    }

    private static bool IsNumericKey(KeyboardEventArgs args)
    {
        return args.Key[0] is <= '9' and >= '0';
    }

    private void PreventFormPostOnEnter(KeyboardEventArgs args)
    {
        // Prevent enter key from submitting the form
        // Note: this only works in WASM
        _preventEnterKey = args.Key is "Enter";
    }

    private async Task CloseDropdown()
    {
        if (_popoverRef is not null)
        {
            await _popoverRef.Hide();
        }
    }

    private static bool IsCompletedHourInput(string? hour)
    {
        if (int.TryParse(hour, out var hourNum))
        {
            return hourNum > 2 || hour.Length == 2;
        }

        return false;
    }

    private static bool IsCompletedMinuteInput(string? minute)
    {
        if (int.TryParse(minute, out var minNum))
        {
            return minNum > 5 || minute.Length == 2;
        }

        return false;
    }

    private bool TrySetTime()
    {
        if (int.TryParse(Hour, out var hour) && int.TryParse(Minute, out var minute))
        {
            if (!_use24HourClock)
            {
                if (hour == 12 && !PostMeridian)
                {
                    hour = 0;
                }
                else if (hour != 12 && PostMeridian)
                {
                    hour += 12;
                }
            }

            var invariantTime = $"{hour:00}:{minute:00}";
            if (TimeOnly.TryParseExact(invariantTime, "HH:mm", out var time))
            {
                _editedTime = time;

                return true;
            }
        }

        _editedTime = null;
        return false;
    }

    private void ReformatInputs(TimeOnly? time, bool clearIfNull = false)
    {
        if (!time.HasValue)
        {
            if (clearIfNull)
            {
                _hour = string.Empty;
                _minute = string.Empty;
            }

            return;
        }

        var formattedTime = time.Value.ToString(TrimmedTimeFormat, _culture);

        var parts = formattedTime.Split(TimeSeparator, 2);
        _hour = parts[0];
        _minute = parts[1];
        PostMeridian = time.Value.Hour >= 12;
    }

    private static bool TryFormatHour24(string? value, out string hour)
    {
        hour = string.Empty;
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        var match = _hour24Pattern.Match(value);
        if (match.Success)
        {
            hour = match.Value;
            return true;
        }

        return false;
    }

    private static bool TryFormatMinute(string? value, out string minute)
    {
        minute = string.Empty;
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        var match = _minutePattern.Match(value);
        if (match.Success)
        {
            minute = match.Value;
            return true;
        }

        return false;
    }

    private string FormatHourRegional(string hour24)
    {
        if (_use24HourClock)
        {
            return hour24;
        }

        if (int.TryParse(hour24, out var hourNum))
        {
            if (hourNum > 12)
            {
                PostMeridian = true;
                hourNum -= 12;
                return hourNum.ToString(CultureInfo.InvariantCulture);
            }
            else if (hour24 == "00")
            {
                PostMeridian = false;
                return "12";
            }
        }

        return hour24;
    }

    public override async ValueTask DisposeAsync()
    {
        await _domUtils.SafeDisposeAsync();

        GC.SuppressFinalize(this);
    }

    [GeneratedRegex(@"^(2[0-3]|[0-1][0-9]|[0-9])")]
    private static partial Regex StartsWithHourPattern();

    [GeneratedRegex(@"^([0-5]?[0-9])")]
    private static partial Regex StartsWithMinutePattern();
}
