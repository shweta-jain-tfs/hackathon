using Pharos.Common.Web.Models.DataGrid;

namespace Pharos.Common.Web.Components.DataGrid.Filters.Models;

/// <summary>
/// Reusable quick filter for multiple typed selections.
/// </summary>
public sealed class MultiSelectQuickFilter<TValue> : QuickFilterBase
{
    private readonly HashSet<TValue> _defaultSelectedValues;

    public MultiSelectQuickFilter(
        string propertyName,
        string title,
        IEnumerable<TValue>? availableValues = null,
        Func<TValue, string>? displayNameSelector = null,
        IEnumerable<TValue>? defaultSelectedValues = null)
        : base(propertyName, title, LogicalFilterOperator.Or)
    {
        _defaultSelectedValues = defaultSelectedValues is null
            ? []
            : defaultSelectedValues.ToHashSet();

        SelectableValues = BuildSelectableValues(availableValues, displayNameSelector, _defaultSelectedValues);
    }

    /// <summary>
    /// Available values in display order.
    /// </summary>
    public IReadOnlyList<SelectableQuickFilterValue<TValue>> SelectableValues { get; }

    public override IEnumerable<PropertyFilterExpression> FilterExpressions
    {
        get
        {
            foreach (var selectable in SelectableValues)
            {
                if (selectable.IsSelected)
                {
                    yield return new PropertyFilterExpression<TValue>(PropertyName, PropertyFilterOperator.EqualsTo, selectable.Value);
                }
            }
        }
    }

    public override int ActiveFilterCount => SelectableValues.Count(x => x.IsSelected);

    public override void Clear()
    {
        foreach (var selectable in SelectableValues)
        {
            selectable.IsSelected = false;
        }

        OnClearInvoked();
    }

    public override void ResetToDefault()
    {
        foreach (var selectable in SelectableValues)
        {
            selectable.IsSelected = _defaultSelectedValues.Contains(selectable.Value);
        }

        OnClearInvoked();
    }

    public override void ApplyState(ColumnFilterState? stateObject)
    {
        if (stateObject is not MultiSelectQuickFilterState state)
        {
            return;
        }

        var selected = QuickFilterStateConverter.FromJsonElements<TValue>(state.SelectedValues);
        foreach (var selectable in SelectableValues)
        {
            selectable.IsSelected = selected.Contains(selectable.Value);
        }
    }

    public override ColumnFilterState SaveState() => new MultiSelectQuickFilterState
    {
        PropertyName = PropertyName,
        SelectedValues = QuickFilterStateConverter.ToJsonElements(SelectableValues.Where(x => x.IsSelected).Select(x => x.Value))
    };

    private static List<SelectableQuickFilterValue<TValue>> BuildSelectableValues(
        IEnumerable<TValue>? availableValues,
        Func<TValue, string>? displayNameSelector,
        HashSet<TValue> defaultSelectedValues)
    {
        var values = ResolveAvailableValues(availableValues, defaultSelectedValues);
        if (values.Count == 0)
        {
            return new List<SelectableQuickFilterValue<TValue>>();

        }

        displayNameSelector ??= value => value?.ToString() ?? string.Empty;
        return values
            .Select(value => new SelectableQuickFilterValue<TValue>(value, displayNameSelector(value))
            {
                IsSelected = defaultSelectedValues.Contains(value)
            })
            .ToList();
    }

    private static List<TValue> ResolveAvailableValues(IEnumerable<TValue>? availableValues, HashSet<TValue> defaultSelectedValues)
    {
        if (availableValues is not null)
        {
            return availableValues.ToList();
        }

        if (typeof(TValue).IsEnum)
        {
            return Enum.GetValues(typeof(TValue)).Cast<TValue>().ToList();
        }

        if (defaultSelectedValues.Count > 0)
        {
            return defaultSelectedValues.ToList();
        }

        return [];
    }
}
