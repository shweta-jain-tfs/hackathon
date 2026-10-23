using Pharos.Common.Web.Components.DataGrid.Columns;
using Pharos.Common.Web.Models.DataGrid;

namespace Pharos.Common.Web.Components.DataGrid.Filters.Models;

public class BoolColumnFilter<TGridItem, TProp> : ColumnFilterBase<TGridItem, TProp, BoolFilterState>
{
    public BoolColumnFilter(Column<TGridItem, TProp> column) : base(column)
    {
        LogicalOperator = LogicalFilterOperator.And;
        ApplyDefault();
    }

    public bool Value { get; set; }

    /// <summary>
    /// Always emits exactly one expression. Unlike the other filters a boolean has no "unset"
    /// state, so <c>false</c> is a real filter value rather than "no filter".
    /// </summary>
    public override IEnumerable<PropertyFilterExpression> FilterExpressions =>
        [Expression(PropertyFilterOperator.Contains, Value)];

    public override void Clear()
    {
        Value = false;
        OnClearInvoked();
    }

    public override void ApplyDefault()
    {
        if (EffectiveQuickFilterDefault is bool defaultValue)
        {
            Value = defaultValue;
        }
    }

    protected override void ApplyTypedState(BoolFilterState state)
    {
        Value = state.Value;
    }

    protected override BoolFilterState CreateState()
    {
        return new BoolFilterState { Value = Value };
    }
}