using Pharos.Common.Web.Components.DataGrid.Columns;
using Pharos.Common.Web.Models.DataGrid;

namespace Pharos.Common.Web.Components.DataGrid.Filters.Models;

public class StringColumnFilter<TGridItem, TProp> : ColumnFilterBase<TGridItem, TProp, StringFilterState>
{
    private readonly bool _isExactMatch;

    public StringColumnFilter(Column<TGridItem, TProp> column, bool isExactMatch = false) : base(column)
    {
        LogicalOperator = LogicalFilterOperator.And;
        _isExactMatch = isExactMatch;
        ApplyDefault();
    }

    public string? Value { get; set; }

    /// <summary>
    /// Emits a single text expression. An empty value means "no filter", so the grid never
    /// sends a no-op such as <c>Contains("")</c> to the server.
    /// </summary>
    public override IEnumerable<PropertyFilterExpression> FilterExpressions
    {
        get
        {
            if (string.IsNullOrEmpty(Value))
            {
                yield break;
            }

            var filterOperator = _isExactMatch ? PropertyFilterOperator.EqualsTo : PropertyFilterOperator.Contains;
            yield return Expression(filterOperator, Value);
        }
    }

    public override void Clear()
    {
        Value = string.Empty;
        OnClearInvoked();
    }

    public override void ApplyDefault()
    {
        if (EffectiveQuickFilterDefault is not null)
        {
            Value = EffectiveQuickFilterDefault.ToString();
        }
    }

    protected override void ApplyTypedState(StringFilterState state)
    {
        // Only apply the string value if it's explicitly present and not null
        if (state.Value is not null)
        {
            Value = state.Value;
        }
    }

    protected override StringFilterState CreateState()
    {
        return new StringFilterState { Value = Value ?? string.Empty };
    }
}
