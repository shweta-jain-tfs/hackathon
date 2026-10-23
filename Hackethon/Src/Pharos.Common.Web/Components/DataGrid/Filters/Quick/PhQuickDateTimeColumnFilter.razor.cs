using Pharos.Common.Web.Components.DataGrid.Filters.Constraints;
using Pharos.Common.Web.Components.DateFilter;
using Pharos.Common.Web.Components.QuickFilter;
using Pharos.Common.Web.Contexts;

namespace Pharos.Common.Web.Components.DataGrid.Filters.Quick;

public partial class PhQuickDateTimeColumnFilter<TGridItem, TProp>
{
    [CascadingParameter(Name = "UiContext")] public UiContext? UiContext { get; set; }

    public DateFilterOptions Options { get; set; } = DateFilterOptions.Default;

    private DateFilterTexts? Texts { get; set; }

    private DateFilterValue FilterValue { get; set; } = new();

    private QuickFilterPanelSize DateFilterPanelSize =>
        (Options & (DateFilterOptions.Month | DateFilterOptions.DateRange)) != 0
            ? QuickFilterPanelSize.Spacious
            : QuickFilterPanelSize.Default;

    private DateFilterConstraints? DateConstraints =>
        ColumnFilter.Column.FilterConstraints as DateFilterConstraints;

    private DateOnly? MinDate { get; set; }
    private DateOnly? MaxDate { get; set; }

    protected override void OnParametersSet()
    {
        var strings = UiContext?.ComponentStrings.DataGrid ?? DataGridStrings.Default;

        Texts = new DateFilterTexts(
            Today: strings.Today,
            LastDaysPrefix: strings.LastDaysPrefix,
            LastDaysSuffix: strings.LastDaysSuffix,
            Month: strings.Month,
            CustomRange: strings.CustomRange,
            From: strings.From,
            To: strings.To);

        Options = DateConstraints?.Options ?? DateFilterOptions.Default;
        MinDate = DateConstraints?.MinDate;
        MaxDate = DateConstraints?.MaxDate;

        var mappedValue = MapFromColumnFilter();
        var shouldPreserveInvalidLastDays =
            FilterValue.Mode == DateFilterMode.LastDays
            && FilterValue.LastDays is not > 0;

        // Preserve an invalid Last Days value while it is being edited.
        // It is discarded when the panel closes or the in-panel clear action is used.
        if (!shouldPreserveInvalidLastDays)
        {
            FilterValue = mappedValue;
        }
    }

    private async Task FilterValueChanged(DateFilterValue value)
    {
        FilterValue = value;
        await UpdateColumnFilter(value);
    }

    private Task UpdateColumnFilter(DateFilterValue value)
    {
        switch (value.Mode)
        {
            case DateFilterMode.Today:
                ColumnFilter.SelectToday();
                break;

            case DateFilterMode.LastDays when value.LastDays is > 0:
                ColumnFilter.SelectLastDays(value.LastDays.Value);
                break;

            case DateFilterMode.Month when value.From is { } from && value.To is { } to:
                ColumnFilter.SelectMonthRange(from, to);
                break;

            case DateFilterMode.DateRange when value.From is { } from && value.To is { } to:
                ColumnFilter.SelectBetween(from, to);
                break;

            default:
                // invalid value, clear the filter without raising OnClear
                ColumnFilter.ClearSelection();
                break;
        }

        return OnValueChanged.InvokeAsync();
    }

    private DateFilterValue MapFromColumnFilter()
    {
        if (ColumnFilter.Between.IsSelected)
        {
            return FilterValue with
            {
                Mode = ColumnFilter.IsMonthRange ? DateFilterMode.Month : DateFilterMode.DateRange,
                From = DateOnly.FromDateTime(ColumnFilter.Between.StartValue.DateTime),
                To = DateOnly.FromDateTime(ColumnFilter.Between.EndValue.DateTime)
            };
        }

        if (ColumnFilter.LastDays.IsSelected)
        {
            return FilterValue with
            {
                Mode = DateFilterMode.LastDays,
                LastDays = (int)ColumnFilter.LastDays.LastDays
            };
        }

        if (ColumnFilter.Today.IsSelected)
        {
            return FilterValue with
            {
                Mode = DateFilterMode.Today
            };
        }

        return new();
    }

    protected override void ResetLocalState()
    {
        FilterValue = MapFromColumnFilter();
    }
}