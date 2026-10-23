using System.Globalization;

namespace Pharos.Common.Web.Components.DatePicker;

public partial class PhDatePickerPanelMonth
{
    [Parameter] public int SelectedYear { get; set; }
    [Parameter] public int SelectedMonth { get; set; }
    [Parameter] public EventCallback<int> MonthSelected { get; set; }
    [Parameter] public EventCallback ShowYearPicker { get; set; }
    [Parameter] public DateOnly? MinDate { get; set; }
    [Parameter] public DateOnly? MaxDate { get; set; }
    [Parameter] public CultureInfo Culture { get; set; } = CultureInfo.CurrentCulture;

    private async Task OnMonthClicked(int month)
    {
        if (MonthSelected.HasDelegate)
        {
            await MonthSelected.InvokeAsync(month);
        }
    }

    private async Task OnYearClick()
    {
        if (ShowYearPicker.HasDelegate)
        {
            await ShowYearPicker.InvokeAsync();
        }
    }

    private bool IsMonthOutOfRange(int year, int month)
    {
        var monthStart = new DateOnly(year, month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        return (MinDate is { } minDate && monthEnd < minDate)
            || (MaxDate is { } maxDate && monthStart > maxDate);
    }
}