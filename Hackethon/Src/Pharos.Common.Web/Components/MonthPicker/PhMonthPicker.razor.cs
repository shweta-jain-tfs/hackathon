using System.Globalization;
using Pharos.Common.Web.Components.FormElements;
using Pharos.Common.Web.Components.FormElements.Enums;

namespace Pharos.Common.Web.Components.MonthPicker;

public partial class PhMonthPicker
{
    [Parameter] public DateOnly? MinDate { get; set; }
    /// <summary>
    /// Defaults to the current month if not specified.
    /// </summary>
    [Parameter] public DateOnly? MaxDate { get; set; }
    [Parameter] public DateOnly? Value { get; set; }
    [Parameter] public EventCallback<DateOnly?> ValueChanged { get; set; }

    [Parameter] public DropdownSize Size { get; set; } = DropdownSize.SmallInputWidth;
    [Parameter] public bool Disabled { get; set; }
    /// <summary>
    /// Optional culture name to use instead of the current culture (e.g. "sv-SE").
    /// Used for testing.
    /// </summary>
    [Parameter] public string? Culture { get; set; }

    private CultureInfo _culture = CultureInfo.CurrentCulture;
    private DateOnly _minMonth;
    private DateOnly _maxMonth;
    private readonly List<DropDownOption<int>> _months = new();
    private readonly List<DropDownOption<int>> _years = new();
    private DateOnly _selected;
    private bool _hasPendingValueChange;

    protected override void OnParametersSet()
    {
        var currentMonth = ToFirstOfMonth(DateOnly.FromDateTime(DateTime.Today));

        _maxMonth = ToFirstOfMonth(MaxDate ?? currentMonth);

        _minMonth = MinDate.HasValue
            ? ToFirstOfMonth(MinDate.Value)
            : Min(ToFirstOfMonth(_maxMonth.AddYears(-9)), currentMonth);

        if (_minMonth > _maxMonth)
        {
            throw new ArgumentException($"{nameof(MinDate)} must be earlier than or equal to {nameof(MaxDate)}.");
        }

        SetCulture();
        SetSelectionFromValue(Value, currentMonth);
        PopulateDropdowns();
        UpdateMonthDisabledStates();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_hasPendingValueChange)
        {
            _hasPendingValueChange = false;
            await ValueChanged.InvokeAsync(_selected);
        }
    }

    private Task MonthChanged(int month)
    {
        if (IsMonthOutsideRange(_selected.Year, month))
        {
            return Task.CompletedTask;
        }

        _selected = new DateOnly(_selected.Year, month, 1);
        return ValueChanged.InvokeAsync(_selected);
    }

    private Task YearChanged(int year)
    {
        _selected = ClampToRange(new DateOnly(year, _selected.Month, 1));
        UpdateMonthDisabledStates();
        return ValueChanged.InvokeAsync(_selected);
    }

    private void SetSelectionFromValue(DateOnly? value, DateOnly currentMonth)
    {
        var normalizedMonth = value.HasValue
            ? ToFirstOfMonth(value.Value)
            : ToFirstOfMonth(currentMonth);

        _selected = ClampToRange(normalizedMonth);

        // We modified the value to a different month than what was passed in,
        // so we need to notify the parent component of the change.
        _hasPendingValueChange = !value.HasValue || _selected != normalizedMonth;
    }

    private void SetCulture()
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
    }

    private void PopulateDropdowns()
    {
        _years.Clear();
        for (var year = _maxMonth.Year; year >= _minMonth.Year; year--)
        {
            _years.Add(new DropDownOption<int>
            {
                Text = year.ToString(CultureInfo.InvariantCulture),
                Value = year
            });
        }

        _months.Clear();
        for (var month = 1; month <= 12; month++)
        {
            _months.Add(new DropDownOption<int>
            {
                Text = _culture.DateTimeFormat.GetMonthName(month),
                Value = month
            });
        }
    }

    private bool IsMonthOutsideRange(int year, int month)
    {
        var date = new DateOnly(year, month, 1);
        return date < _minMonth || date > _maxMonth;
    }

    private void UpdateMonthDisabledStates()
    {
        foreach (var monthOption in _months)
        {
            monthOption.Disabled = IsMonthOutsideRange(_selected.Year, monthOption.Value);
        }
    }

    private DateOnly ClampToRange(DateOnly month)
    {
        if (month < _minMonth)
        {
            return _minMonth;
        }

        if (month > _maxMonth)
        {
            return _maxMonth;
        }

        return month;
    }

    private static DateOnly ToFirstOfMonth(DateOnly date)
    {
        return new(date.Year, date.Month, 1);
    }

    private static DateOnly Min(DateOnly a, DateOnly b)
    {
        return a < b ? a : b;
    }
}