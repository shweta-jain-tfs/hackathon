namespace Pharos.Common.Web.Components.DatePicker;

internal class CalendarDate
{
    public CalendarDate(DateOnly date, bool isVisibleMonth = false)
    {
        Date = date;
        IsToday = date == DateOnly.FromDateTime(DateTime.Today.Date);
        IsVisibleMonth = isVisibleMonth;
    }

    public DateOnly Date { get; set; }
    public bool IsToday { get; set; }
    public bool IsVisibleMonth { get; set; }
}