namespace Pharos.Common.Web.Components.DateFilter;

public partial class PhDateFilter
{
    [Parameter] public DateFilterValue Value { get; set; } = DateFilterValue.Empty;
    [Parameter] public EventCallback<DateFilterValue> ValueChanged { get; set; }
    [Parameter, EditorRequired] public DateFilterTexts? Texts { get; set; }
    [Parameter] public DateOnly? MinDate { get; set; }
    /// <summary>
    /// The latest selectable date. Defaults to today when not specified.
    /// </summary>
    [Parameter] public DateOnly? MaxDate { get; set; }
    [Parameter] public DateFilterOptions Options { get; set; } = DateFilterOptions.Default;

    private readonly string _idPrefix =
    $"ph-date-filter-{Guid.NewGuid():N}";

    private string FromId => $"{_idPrefix}-from";
    private string ToId => $"{_idPrefix}-to";

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

    private DateOnly EffectiveMaxDate => MaxDate ?? Today;

    private DateFilterTexts Strings { get; set; } = new();

    protected override void OnParametersSet()
    {
        Strings = Texts ?? throw new InvalidOperationException(
            $"{nameof(PhDateFilter)} requires {nameof(Texts)}.");

        if (MinDate > EffectiveMaxDate)
        {
            throw new ArgumentException($"{nameof(MinDate)} must be earlier than or equal to {nameof(MaxDate)}.");
        }
    }

    private async Task ModeChanged(DateFilterMode mode)
    {
        var to = Value.To;
        var from = Value.From;
        var lastDays = Value.LastDays;

        if (lastDays is not > 0)
        {
            lastDays = DateFilterValue.DefaultLastDays;
        }

        if (mode == DateFilterMode.DateRange)
        {
            if (to is null || from is null)
            {
                from = to = ClampDate(Today, MinDate, EffectiveMaxDate); ;
            }
            else
            {
                from = ClampDate(from.Value, MinDate, EffectiveMaxDate);
                to = ClampDate(to.Value, MinDate, EffectiveMaxDate);
            }
        }
        else if (mode == DateFilterMode.Month)
        {
            var month = from ?? ClampDate(Today, MinDate, EffectiveMaxDate);
            from = new DateOnly(month.Year, month.Month, 1);
            to = from.Value.AddMonths(1).AddDays(-1);
        }

        await ValueChanged.InvokeAsync(Value with
        {
            Mode = mode,
            LastDays = lastDays,
            From = from,
            To = to
        });
    }

    private async Task LastDaysChanged(int? lastDays)
    {
        await ValueChanged.InvokeAsync(Value with
        {
            LastDays = lastDays
        });
    }

    private async Task MonthChanged(DateOnly? month)
    {
        await ValueChanged.InvokeAsync(Value with
        {
            From = month,
            To = month?.AddMonths(1).AddDays(-1)
        });
    }

    private Task FromChanged(DateOnly from)
    {
        // Defensive clamp. PhDatePicker owns user-input constraints and display updates.
        from = ClampDate(from, MinDate, EffectiveMaxDate);

        var to = Value.To is { } currentTo
            ? ClampDate(currentTo, MinDate, EffectiveMaxDate)
            : from;

        // User-selected value wins. Maintain a valid range.
        if (to < from)
        {
            to = from;
        }

        return ValueChanged.InvokeAsync(Value with
        {
            From = from,
            To = to
        });
    }

    private Task ToChanged(DateOnly to)
    {
        // Defensive clamp. PhDatePicker owns user-input constraints and display updates.
        to = ClampDate(to, MinDate, EffectiveMaxDate);

        var from = Value.From is { } currentFrom
            ? ClampDate(currentFrom, MinDate, EffectiveMaxDate)
            : to;

        // User-selected value wins. Maintain a valid range.
        if (from > to)
        {
            from = to;
        }

        return ValueChanged.InvokeAsync(Value with
        {
            From = from,
            To = to
        });
    }

    private static DateOnly ClampDate(
        DateOnly date,
        DateOnly? minDate,
        DateOnly? maxDate)
    {
        if (minDate is { } min && date < min)
        {
            return min;
        }

        if (maxDate is { } max && date > max)
        {
            return max;
        }

        return date;
    }

    private bool ShouldRenderMode(DateFilterMode mode)
    {
        if (Value.Mode == mode)
        {
            return true;
        }

        return mode switch
        {
            DateFilterMode.Today => Options.HasFlag(DateFilterOptions.Today),
            DateFilterMode.LastDays => Options.HasFlag(DateFilterOptions.LastDays),
            DateFilterMode.Month => Options.HasFlag(DateFilterOptions.Month),
            DateFilterMode.DateRange => Options.HasFlag(DateFilterOptions.DateRange),
            _ => false
        };
    }
}