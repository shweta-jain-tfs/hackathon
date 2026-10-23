using System.Globalization;

namespace Pharos.Common.Web.Components.DatePicker;

public enum PickerMode
{
    Day,
    Month,
    Year
}

public partial class PhDatePickerPanel
{
    [Parameter] public EventCallback<DateOnly?> SelectedDateChanged { get; set; }
    [Parameter] public DateOnly? SelectedDate { get; set; }
    [Parameter] public DateOnly? MinDate { get; set; }
    [Parameter] public DateOnly? MaxDate { get; set; }

    /// <summary>
    /// Optional forced culture to use instead of the current culture. Used for testing.
    /// </summary>
    [Parameter] public string? Culture { get; set; }

    private DateOnly _visibleMonth = DateOnly.FromDateTime(DateTime.Now.Date);
    private IList<CalendarDate> _calendar = new List<CalendarDate>(0);
    private List<string> _daysOfWeek = new(7);
    private CultureInfo _culture = CultureInfo.CurrentCulture;
    private PickerMode _pickerMode = PickerMode.Day;

    private bool IsPreviousMonthDisabled =>
        MinDate is { } minDate &&
        FirstDayOfMonth(_visibleMonth) <= FirstDayOfMonth(minDate);

    private bool IsNextMonthDisabled =>
        MaxDate is { } maxDate &&
        FirstDayOfMonth(_visibleMonth) >= FirstDayOfMonth(maxDate);

    protected override void OnParametersSet()
    {
        _pickerMode = PickerMode.Day;

        var previousCulture = _culture.LCID;
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

        var cultureChanged = previousCulture != _culture.LCID;
        var today = DateOnly.FromDateTime(DateTime.Now.Date);

        var monthToShow = SelectedDate ?? today;
        if (monthToShow == default)
        {
            // Treat default date (year 1) as null and show the current month instead
            monthToShow = today;
        }

        if (_calendar.Count == 0 || monthToShow.Year != _visibleMonth.Year || monthToShow.Month != _visibleMonth.Month || cultureChanged)
        {
            _visibleMonth = monthToShow;
            _calendar = GetCalendar(_visibleMonth);

            if (_daysOfWeek.Count != 7 || cultureChanged)
            {
                _daysOfWeek = GetDaysOfWeek(_calendar);
            }
        }
    }

    private async Task OnDayClicked(DateOnly day)
    {
        if (day != SelectedDate)
        {
            SelectedDate = day;
            if (SelectedDateChanged.HasDelegate)
            {
                await SelectedDateChanged.InvokeAsync(day);
            }
        }
    }

    private void OnChangeMonth(int monthDifference)
    {
        _visibleMonth = _visibleMonth.AddMonths(monthDifference);
        _calendar = GetCalendar(_visibleMonth);
    }

    private void SetPickerMode(PickerMode mode)
    {
        _pickerMode = mode;
    }

    private void OnYearSelected(int year)
    {
        _visibleMonth = new DateOnly(year, _visibleMonth.Month, 1);
        _calendar = GetCalendar(_visibleMonth);
        _pickerMode = PickerMode.Month;
    }

    private void OnMonthSelected(int month)
    {
        _visibleMonth = new DateOnly(_visibleMonth.Year, month, 1);
        _calendar = GetCalendar(_visibleMonth);
        _pickerMode = PickerMode.Day;
    }

    private IList<CalendarDate> GetCalendar(DateOnly month)
    {
        return CalendarGenerator.GetCalendar(month.Year, month.Month, _culture.DateTimeFormat.FirstDayOfWeek);
    }

    private List<string> GetDaysOfWeek(IList<CalendarDate> calendar)
    {
        return calendar.Take(7).Select(GetShortDayName).ToList();
    }

    private string GetShortDayName(CalendarDate date)
    {
        var abbreviatedDay = _culture.DateTimeFormat.GetAbbreviatedDayName(date.Date.DayOfWeek);
        return abbreviatedDay.Length > 2 ? abbreviatedDay.Substring(0, 2) : abbreviatedDay;
    }

    private static DateOnly FirstDayOfMonth(DateOnly date)
    {
        return new(date.Year, date.Month, 1);
    }
}