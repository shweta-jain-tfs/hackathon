using Microsoft.JSInterop;

namespace Pharos.Common.Web.Components.DatePicker;

public partial class PhDatePickerPanelYear
{
    [Parameter] public int SelectedYear { get; set; }
    [Parameter] public EventCallback<int> YearSelected { get; set; }
    [Parameter] public DateOnly? MinDate { get; set; }
    [Parameter] public DateOnly? MaxDate { get; set; }

    private List<int> Years { get; set; } = new();
    private bool _shouldScrollToYear;
    private int _previousSelectedYear;
    private IJSObjectReference? _scrollModule;

    protected override void OnParametersSet()
    {
        if (Years.Count == 0)
        {
            Years = GenerateYearsList();
        }

        // Set flag to scroll if selected year changed or on first render
        if (SelectedYear != _previousSelectedYear)
        {
            _shouldScrollToYear = true;
            _previousSelectedYear = SelectedYear;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _scrollModule = await ImportCommonUtilityJSAsync("scroll.js");
        }

        if (_shouldScrollToYear || firstRender)
        {
            _shouldScrollToYear = false;
            if (_scrollModule != null)
            {
                try
                {
                    await _scrollModule.InvokeVoidAsync("scrollToElement", $"year-{SelectedYear}");
                }
                catch
                {
                    // Ignore errors if element not found
                }
            }
        }
    }

    private async Task OnYearClicked(int year)
    {
        if (YearSelected.HasDelegate)
        {
            await YearSelected.InvokeAsync(year);
        }
    }

    private List<int> GenerateYearsList()
    {
        var currentYear = DateTime.Now.Year;
        var firstYear = currentYear - 200;
        var lastYear = currentYear;

        // Include allowed range
        if (MinDate is { } minDate)
        {
            firstYear = Math.Min(firstYear, minDate.Year);
        }

        if (MaxDate is { } maxDate)
        {
            lastYear = Math.Max(lastYear, maxDate.Year);
        }

        // Keep an externally supplied selected year visible.
        firstYear = Math.Min(firstYear, SelectedYear);
        lastYear = Math.Max(lastYear, SelectedYear);

        var years = new List<int>();

        // Generate years in descending order (current year at top)
        for (var year = lastYear; year >= firstYear; year--)
        {
            years.Add(year);
        }

        return years;
    }
    private bool IsYearOutOfRange(int year)
    {
        return (MinDate is { } minDate && year < minDate.Year)
            || (MaxDate is { } maxDate && year > maxDate.Year);
    }

    public override async ValueTask DisposeAsync()
    {
        if (_scrollModule != null)
        {
            await _scrollModule.DisposeAsync();
        }
        GC.SuppressFinalize(this);
    }
}




