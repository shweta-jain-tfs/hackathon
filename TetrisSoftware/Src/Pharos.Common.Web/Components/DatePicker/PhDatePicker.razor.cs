using System.Globalization;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components.Forms;
using Pharos.Common.Web.Components.FormElements.Enums;
using Pharos.Common.Web.Components.Label;
using Pharos.Common.Web.Components.Popover;
using Pharos.Common.Web.Extensions;
using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.DatePicker;

/// <summary>
/// Date picker component for selecting and binding a date value.
/// Supports <see cref="DateTime"/>, <see cref="DateTimeOffset"/>, and <see cref="DateOnly"/> (including nullable variants).
/// Automatically reports value changes and displays validation errors when used inside an <see cref="EditContext"/>.
/// </summary>
public partial class PhDatePicker<TValue>
{
    #region Parameters

    [CascadingParameter] public EditContext? CascadedEditContext { get; set; }
    [Parameter] public string? Id { get; set; }

    /// <summary>
    /// The current value. Supported types: DateTime/DateTime?, DateTimeOffset/DateTimeOffset?, DateOnly/DateOnly?.
    /// </summary>
    [Parameter] public TValue Value { get; set; } = default!;

    /// <summary>
    /// This is set automatically when binding to <see cref="Value"/>.
    /// </summary>
    [Parameter] public Expression<Func<TValue>>? ValueExpression { get; set; }

    [Parameter] public EventCallback<TValue> ValueChanged { get; set; }

    [Parameter]
    public DateOnly? MinDate { get; set; }

    [Parameter]
    public DateOnly? MaxDate { get; set; }

    [Parameter] public bool Disabled { get; set; }

    /// <summary>
    /// Renders the component as display-only (non-interactive, no automatic validation).
    /// Overrides <see cref="Disabled"/>, but keeps the disabled label and value color.
    /// </summary>
    [Parameter] public bool DisplayOnly { get; set; }

    [Parameter] public DatePickerSize Size { get; set; } = DatePickerSize.MediumInputWidth;

    /// <summary>
    /// Mark the field as having validation errors. This is set automatically when used in an EditForm with validation (PhValidationContext).
    /// </summary>
    [Parameter] public bool Error { get; set; }

    /// <summary>
    /// Optional forced culture to use instead of the current culture. Used for testing.
    /// </summary>
    [Parameter] public string? Culture { get; set; }

    // PhLabel support parameters
    [Parameter] public string Label { get; set; } = "";
    [Parameter] public string? ErrorText { get; set; }
    [Parameter] public string? HelpText { get; set; } = "";

    #endregion

    public string BindValue { get; set; } = string.Empty;

    private static readonly Type _valueType = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);

    private PhPopover? _popover;
    private bool _isOpen;
    private FieldIdentifier? _fieldIdentifier;
    private bool _error;
    private CultureInfo _culture = CultureInfo.CurrentCulture;
    private string _dateFormat = "yyyy-MM-dd";

    private string PlaceholderText => _dateFormat.ToUpperInvariant();

    protected string CssClassItems => new CssBuilder("ph-datepicker")
        .AddClass("ph-datepicker--error", _error)
        .AddClass("ph-datepicker--open", _isOpen)
        .AddClass("ph-datepicker--disabled", Disabled)
        .AddClass("ph-datepicker--size-medium", Size == DatePickerSize.MediumInputWidth)
        .AddClass("ph-datepicker--size-large", Size == DatePickerSize.LargeInputWidth)
        .Build();

    protected string? AutomatedTestId { get; set; }

    protected Expression<Func<object>>? ValidationForExpression { get; set; }
    protected bool HasLabel => !string.IsNullOrEmpty(Label);
    protected bool HasErrorText => _error && !string.IsNullOrEmpty(ErrorText);

    // Panel now uses DateOnly?
    private DateOnly? SelectedDateForPanel => ConvertValueToPanelDate(Value);

    protected override void OnInitialized()
    {
        base.OnInitialized();

        if (_valueType != typeof(DateTime) &&
            _valueType != typeof(DateTimeOffset) &&
            _valueType != typeof(DateOnly))
        {
            throw new NotSupportedException(
                $"{typeof(PhDatePicker<TValue>).Name} only supports DateTime, DateTimeOffset, and DateOnly (including nullable variants).");
        }
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

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

        SetDateFormat(_culture.DateTimeFormat);

        BindValue = FormatDate(SelectedDateForPanel);

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

    private void SetDateFormat(DateTimeFormatInfo dateTimeFormat)
    {
        _dateFormat = dateTimeFormat.ShortDatePattern;
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

    private async Task SelectedDateHandler(DateOnly? date)
    {
        ClosePopover();

        BindValue = FormatDate(date);
        Value = ConvertPanelDateToValue(date);

        if (ValueChanged.HasDelegate)
        {
            await ValueChanged.InvokeAsync(Value);
        }

        if (!DisplayOnly && _fieldIdentifier.HasValue)
        {
            CascadedEditContext?.NotifyFieldChanged(_fieldIdentifier.Value);
            UpdateValidationError();
        }
    }

    private void SetOpenState(bool value)
    {
        _isOpen = value;
    }

    private void ClosePopover()
    {
        _popover?.Hide();
    }

    private static bool IsDefaultValue(TValue value)
    {
        return EqualityComparer<TValue>.Default.Equals(value, default!);
    }

    private static DateOnly? ConvertValueToPanelDate(TValue value)
    {
        // Treat default (incl null for nullable TValue) as "no date selected"
        if (IsDefaultValue(value))
        {
            return null;
        }

        return value switch
        {
            DateTimeOffset dto => DateOnly.FromDateTime(dto.Date),
            DateTime dt => DateOnly.FromDateTime(dt.Date),
            DateOnly d => d,
            _ => throw new NotSupportedException($"{typeof(PhDatePicker<TValue>).Name} does not support binding to {typeof(TValue).FullName}.")
        };
    }

    private static TValue ConvertPanelDateToValue(DateOnly? date)
    {
        if (!date.HasValue || date.Value == default)
        {
            return default!;
        }

        if (_valueType == typeof(DateTimeOffset))
        {
            var dto = new DateTimeOffset(date.Value.ToDateTime(TimeOnly.MinValue));
            return (TValue)(object)dto;
        }

        if (_valueType == typeof(DateTime))
        {
            var dt = date.Value.ToDateTime(TimeOnly.MinValue);
            return (TValue)(object)dt;
        }

        if (_valueType == typeof(DateOnly))
        {
            return (TValue)(object)date.Value;
        }

        throw new NotSupportedException($"{typeof(PhDatePicker<TValue>).Name} does not support binding to {typeof(TValue).FullName}.");
    }

    private string FormatDate(DateOnly? date)
    {
        if (!date.HasValue || date == default(DateOnly))
        {
            return "";
        }

        return date.Value.ToString(_dateFormat, _culture);
    }
}
