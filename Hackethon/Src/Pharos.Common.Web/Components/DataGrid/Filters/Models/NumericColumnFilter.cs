using Pharos.Common.Web.Components.DataGrid.Columns;
using Pharos.Common.Web.Models.DataGrid;

namespace Pharos.Common.Web.Components.DataGrid.Filters.Models;

public class NumericColumnFilter<TGridItem, TProp> : ColumnFilterBase<TGridItem, TProp, NumericFilterState>
{
    public NumericColumnFilter(Column<TGridItem, TProp> column) : base(column)
    {
        LogicalOperator = LogicalFilterOperator.And;
    }

    public NumericFilterValues GreaterThanValue { get; set; } = new NumericFilterValues();

    public NumericFilterValues LessThanValue { get; set; } = new NumericFilterValues();

    public NumericFilterValues EqualToValue { get; set; } = new NumericFilterValues();

    /// <summary>
    /// Emits one expression per selected comparison. A value that carries input but is not
    /// selected is deliberately ignored, so the user keeps their typed number without it
    /// affecting the result. The emission order (equal, greater, less) is part of the contract.
    /// </summary>
    public override IEnumerable<PropertyFilterExpression> FilterExpressions
    {
        get
        {
            if (EqualToValue.IsSelected)
            {
                yield return Expression(PropertyFilterOperator.EqualsTo, EqualToValue.Value);
            }

            if (GreaterThanValue.IsSelected)
            {
                yield return Expression(PropertyFilterOperator.GreaterThan, GreaterThanValue.Value);
            }

            if (LessThanValue.IsSelected)
            {
                yield return Expression(PropertyFilterOperator.LessThan, LessThanValue.Value);
            }
        }
    }

    public override void Clear()
    {
        LessThanValue.IsSelected = false;
        GreaterThanValue.IsSelected = false;
        EqualToValue.IsSelected = false;

        //Clear values
        LessThanValue.Value = 0;
        GreaterThanValue.Value = 0;
        EqualToValue.Value = 0;

        OnClearInvoked();
    }

    public override void ApplyDefault()
    {
        // No default value is applied for numeric filters.
        // Instead, clear the filter to ensure a known state and avoid ambiguity.
        Clear();
    }

    protected override void ApplyTypedState(NumericFilterState state)
    {
        // Copy values instead of replacing the instances, because the UI binds directly to the
        // existing NumericFilterValues objects.
        Copy(state.GreaterThan, GreaterThanValue);
        Copy(state.LessThan, LessThanValue);
        Copy(state.EqualTo, EqualToValue);

        static void Copy(NumericFilterValues source, NumericFilterValues target)
        {
            target.IsSelected = source.IsSelected;
            target.Value = source.Value;
        }
    }

    protected override NumericFilterState CreateState()
    {
        return new NumericFilterState
        {
            // Always save all numeric filter values regardless of IsSelected state
            // This ensures the complete filter state is preserved in serialization
            GreaterThan = GreaterThanValue,
            LessThan = LessThanValue,
            EqualTo = EqualToValue
        };
    }
}

public class NumericFilterValues
{
    public bool IsSelected { get; set; }
    public double Value { get; set; }
}
