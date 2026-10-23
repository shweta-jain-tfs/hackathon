using Pharos.Common.Web.Components.DataGrid.Columns;
using Pharos.Common.Web.Models.DataGrid;

namespace Pharos.Common.Web.Components.DataGrid.Filters.Models;

public class DateTimeColumnFilter<TGridItem, TProp> : ColumnFilterBase<TGridItem, TProp, DateTimeFilterState>
{
    public DateTimeFilterValues Today { get; set; }

    public DateTimeFilterValues LastDays { get; set; }

    public DateTimeFilterValues Between { get; set; }

    public bool IsMonthRange { get; set; }

    public DateTimeColumnFilter(Column<TGridItem, TProp> column) : base(column)
    {
        LogicalOperator = LogicalFilterOperator.And;

        Today = NewMode();
        LastDays = NewMode();
        Between = NewMode();

        ApplyDefault();
    }

    /// <summary>
    /// A mode starts unselected and pre-filled with today's date, so a date picker bound to it
    /// never opens on a meaningless value.
    /// </summary>
    private static DateTimeFilterValues NewMode() => new()
    {
        StartValue = DateTimeOffset.Now.Date,
        EndValue = DateTimeOffset.Now.Date
    };

    /// <summary>
    /// Every date mode filters an inclusive range, so all three share a single expression path.
    /// <para>
    /// The modes are not mutually exclusive here on purpose: the filter modal binds a separate
    /// checkbox to each one, so a user can combine them. They are AND-combined via
    /// <see cref="LogicalFilterOperator.And"/>. The quick filter, in contrast, uses the
    /// <c>Select*</c> methods below, which do enforce one mode at a time.
    /// </para>
    /// </summary>
    public override IEnumerable<PropertyFilterExpression> FilterExpressions
    {
        get
        {
            if (Between.IsSelected)
            {
                yield return Expression(PropertyFilterOperator.GreaterThanOrEqualTo, Between.StartValue);
                yield return Expression(PropertyFilterOperator.LessThanOrEqualTo, Between.EndValue);
            }

            if (LastDays.IsSelected)
            {
                yield return Expression(PropertyFilterOperator.GreaterThanOrEqualTo, ResolveLastDaysStart());
                yield return Expression(PropertyFilterOperator.LessThanOrEqualTo, LastDays.EndValue);
            }

            if (Today.IsSelected)
            {
                yield return Expression(PropertyFilterOperator.GreaterThanOrEqualTo, Today.StartValue);
                yield return Expression(PropertyFilterOperator.LessThanOrEqualTo, Today.EndValue);
            }
        }
    }

    /// <summary>
    /// Converts the "last N days" count into a concrete start date. A count large enough to run off
    /// the calendar falls back to one day after <see cref="DateTimeOffset.MinValue"/>, because
    /// nswag cannot parse <see cref="DateTimeOffset.MinValue"/> itself.
    /// </summary>
    private DateTimeOffset ResolveLastDaysStart()
    {
        try
        {
            return DateTimeOffset.Now.AddDays(-Math.Abs(LastDays.LastDays)).Date;
        }
        catch (ArgumentOutOfRangeException)
        {
            return DateTimeOffset.MinValue.AddDays(1);
        }
    }

    /// <summary>
    /// Each mode contributes one user-visible filter but two expressions (a range), so this cannot
    /// fall back to counting <see cref="FilterExpressions"/>.
    /// </summary>
    public override int ActiveFilterCount
    {
        get
        {
            var count = Between.IsSelected ? 1 : 0;
            count += LastDays.IsSelected ? 1 : 0;
            count += Today.IsSelected ? 1 : 0;
            return count;
        }
    }

    public override void Clear()
    {
        ClearSelection();

        // Reset values
        Between.StartValue = DateTimeOffset.Now.Date;
        Between.EndValue = DateTimeOffset.Now.Date;
        LastDays.LastDays = 0;

        OnClearInvoked();
    }

    /// <summary>
    /// Deselects every mode. Each <c>Select*</c> method calls this first, which is what makes the
    /// quick filter single-mode even though the underlying model supports combinations.
    /// </summary>
    public void ClearSelection()
    {
        Today.IsSelected = false;
        LastDays.IsSelected = false;
        Between.IsSelected = false;
        IsMonthRange = false;
    }

    public void SelectToday()
    {
        ClearSelection();
        Today.IsSelected = true;
        Today.StartValue = DateTimeOffset.Now.Date;
        Today.EndValue = DateTimeOffset.Now.Date;
    }

    public void SelectLastDays(double days)
    {
        ClearSelection();
        LastDays.IsSelected = true;
        LastDays.LastDays = Math.Abs(days);
        LastDays.EndValue = DateTimeOffset.Now.Date;
    }

    public void SelectBetween(DateOnly from, DateOnly to)
    {
        ClearSelection();
        Between.IsSelected = true;
        Between.StartValue = ToDateTimeOffset(from);
        Between.EndValue = ToDateTimeOffset(to);
    }

    public void SelectMonthRange(DateOnly from, DateOnly to)
    {
        ClearSelection();
        Between.IsSelected = true;
        Between.StartValue = ToDateTimeOffset(from);
        Between.EndValue = ToDateTimeOffset(to);
        IsMonthRange = true;
    }

    private static DateTimeOffset ToDateTimeOffset(DateOnly date)
    {
        return new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue));
    }

    public override void ApplyDefault()
    {
        // Apply the default value as "Last Days" if the default is an integer.
        if (EffectiveQuickFilterDefault is int lastDays)
        {
            SelectLastDays(lastDays);
        }
    }

    protected override void ApplyTypedState(DateTimeFilterState state)
    {
        // All three modes can be active at the same time (see the filter modal), so each one is
        // restored independently rather than through a single mode switch.
        Today.IsSelected = state.TodaySelected;

        // LastDaysCount is null when the mode was not active, so it doubles as the selection flag.
        if (state.LastDaysCount is { } lastDaysCount)
        {
            LastDays.IsSelected = true;
            LastDays.LastDays = lastDaysCount;
        }
        else
        {
            LastDays.IsSelected = false;
        }

        // The between bounds are only ever persisted as a pair.
        if (state.BetweenStartDate is { } start && state.BetweenEndDate is { } end)
        {
            Between.IsSelected = true;
            Between.StartValue = start;
            Between.EndValue = end;
            IsMonthRange = state.IsMonthRange;
        }
        else
        {
            Between.IsSelected = false;
        }
    }

    protected override DateTimeFilterState CreateState()
    {
        return new DateTimeFilterState
        {
            // Only the values needed to rebuild the selection are stored.
            TodaySelected = Today.IsSelected,
            LastDaysCount = LastDays.IsSelected ? (int)LastDays.LastDays : null,
            BetweenStartDate = Between.IsSelected ? Between.StartValue : null,
            BetweenEndDate = Between.IsSelected ? Between.EndValue : null,
            IsMonthRange = IsMonthRange
        };
    }
}

public class DateTimeFilterValues
{
    public bool IsSelected { get; set; }
    public double LastDays { get; set; }
    public DateTimeOffset StartValue { get; set; }
    public DateTimeOffset EndValue { get; set; }
}