using System.Globalization;
using Pharos.Common.Web.Components.DataGrid.Columns;
using Pharos.Common.Web.Models.DataGrid;

namespace Pharos.Common.Web.Components.DataGrid.Filters.Models;

public class EnumColumnFilter<TGridItem, TProp> : ColumnFilterBase<TGridItem, TProp, EnumFilterState>
{
    public EnumColumnFilter(
        Column<TGridItem, TProp> column,
        Func<TProp, string>? enumDisplayNameSelector = null) : base(column)
    {
        LogicalOperator = LogicalFilterOperator.Or;

        SelectableValues = new();
        foreach (TProp enumValue in Enum.GetValues(typeof(TProp)))
        {
            var displayName = enumDisplayNameSelector?.Invoke(enumValue) ?? enumValue.ToString() ?? string.Empty;

            SelectableValues.Add(new SelectableEnum(Convert.ToInt32(enumValue, CultureInfo.InvariantCulture), displayName));
        }

        ApplyDefault();
    }

    public List<SelectableEnum> SelectableValues { get; }

    public override IEnumerable<PropertyFilterExpression> FilterExpressions
    {
        get
        {
            foreach (var selectable in SelectableValues)
            {
                if (selectable.IsSelected)
                {
                    yield return new PropertyFilterExpression<int>(Column.PropertyName, PropertyFilterOperator.EqualsTo, selectable.Value);
                }
            }
        }
    }

    public override int ActiveFilterCount => SelectableValues.Count(x => x.IsSelected);

    public override void Clear()
    {
        SelectableValues.ForEach(x => x.IsSelected = false);
        OnClearInvoked();
    }

    public override void ApplyDefault()
    {
        if (EffectiveQuickFilterDefault is not null)
        {
            var intValue = Convert.ToInt32(EffectiveQuickFilterDefault, CultureInfo.InvariantCulture);
            SelectableValues.ForEach(x => x.IsSelected = x.Value == intValue);
        }
    }


    protected override void ApplyTypedState(EnumFilterState state)
    {
        var selectedValuesSet = state.SelectedValues.ToHashSet();

        foreach (var selectableValue in SelectableValues)
        {
            selectableValue.IsSelected = selectedValuesSet.Contains(selectableValue.Value);
        }
    }

    protected override EnumFilterState CreateState()
    {
        return new EnumFilterState
        {
            // Only serialize selected enum values as a list of integers
            SelectedValues = SelectableValues.Where(x => x.IsSelected).Select(x => x.Value).ToList()
        };
    }
}

public class SelectableEnum
{
    public SelectableEnum(int value, string displayName)
    {
        Value = value;
        DisplayName = displayName;
    }

    public bool IsSelected { get; set; }
    public int Value { get; }
    public string DisplayName { get; }
}