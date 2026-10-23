namespace Pharos.Common.Web.Components.DatePicker;

internal static class CalendarGenerator
{
    private const int _weeksToShow = 6;
    private const int _daysToShow = 7 * _weeksToShow;

    public static IList<CalendarDate> GetCalendar(int year, int month, DayOfWeek startWeekOnDay = DayOfWeek.Monday)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(year);

        if (month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month));
        }

        var currentMonth = new DateOnly(year, month, 1);
        var visibleMonthDates = GetDaysInMonth(year, month)
            .Select(date => new CalendarDate(date, isVisibleMonth: true))
            .ToList();

        var daysBefore = GetDaysDifference(startWeekOnDay, visibleMonthDates.First().Date.DayOfWeek);
        var previousMonth = currentMonth.AddMonths(-1);
        var lastMonthDates = GetDaysInMonth(previousMonth.Year, previousMonth.Month)
            .TakeLast(daysBefore)
            .Select(date => new CalendarDate(date))
            .ToList();

        var daysAfter = Math.Max(0, _daysToShow - lastMonthDates.Count - visibleMonthDates.Count);
        var nextMonth = currentMonth.AddMonths(1);
        var nextMonthDates = GetDaysInMonth(nextMonth.Year, nextMonth.Month)
            .Take(daysAfter)
            .Select(date => new CalendarDate(date));

        return lastMonthDates.Concat(visibleMonthDates).Concat(nextMonthDates).ToList();
    }

    public static int GetDaysDifference(DayOfWeek startDay, DayOfWeek endDay)
    {
        var end = endDay <= startDay ? 7 + (int)endDay : (int)endDay;
        return end - (int)startDay;
    }

    private static List<DateOnly> GetDaysInMonth(int year, int month)
    {


        return Enumerable.Range(1, DateTime.DaysInMonth(year, month))
            .Select(day => new DateOnly(year, month, day))
            .ToList();
    }
}