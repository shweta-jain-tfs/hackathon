using Pharos.Common.Web.Models.DataGrid;

namespace Pharos.Common.Web.Components.DataGrid.Filters.Models;

/// <summary>
/// Reusable quick filter for a single typed value.
/// </summary>
public sealed class SingleValueQuickFilter<TValue> : QuickFilterBase
{
    private readonly bool _hasDefaultValue;
    private readonly TValue? _defaultValue;

    public SingleValueQuickFilter(
        string propertyName,
        string title,
        IEnumerable<TValue>? availableValues = null,
        Func<TValue, string>? displayNameSelector = null,
        TValue? defaultValue = default,
        bool hasDefaultValue = false)
        : base(propertyName, title, LogicalFilterOperator.And)
    {
        _defaultValue = defaultValue;
        _hasDefaultValue = hasDefaultValue;

        Choices = BuildChoices(availableValues, displayNameSelector);

        if (_hasDefaultValue && _defaultValue != null)
        {
            Value = _defaultValue;
        }
    }

    /// <summary>
    /// Available choices used by the presentation component when this filter behaves like a selector.
    /// </summary>
    public IReadOnlyList<QuickFilterChoice<TValue>> Choices { get; }

    /// <summary>
    /// The selected value. <see cref="HasValue"/> distinguishes an intentionally empty filter
    /// from valid values such as <c>0</c> or <c>false</c>.
    /// </summary>
    public TValue Value { get; set; } = default!;

    public bool HasValue { get; set; }

    public override IEnumerable<PropertyFilterExpression> FilterExpressions
    {
        get
        {
            if (!HasValue)
            {
                yield break;
            }

            yield return new PropertyFilterExpression<TValue>(PropertyName, PropertyFilterOperator.EqualsTo, Value);
        }
    }

    public override int ActiveFilterCount => HasValue ? 1 : 0;

    public override void Clear()
    {
        HasValue = false;
        Value = default!;
        OnClearInvoked();
    }

    public override void ResetToDefault()
    {
        if (_hasDefaultValue)
        {
            Value = _defaultValue!;
            HasValue = true;
        }
        else
        {
            Clear();
            return;
        }

        OnClearInvoked();
    }

    public override void ApplyState(ColumnFilterState? stateObject)
    {
        if (stateObject is not SingleValueQuickFilterState state)
        {
            return;
        }

        HasValue = state.HasValue;
        Value = HasValue
            ? QuickFilterStateConverter.FromJsonElement<TValue>(state.Value)!
            : default!;
    }

    public override ColumnFilterState SaveState() => new SingleValueQuickFilterState
    {
        PropertyName = PropertyName,
        HasValue = HasValue,
        Value = HasValue ? QuickFilterStateConverter.ToJsonElement(Value) : null
    };

    private static List<QuickFilterChoice<TValue>> BuildChoices(
        IEnumerable<TValue>? availableValues,
        Func<TValue, string>? displayNameSelector)
    {
        var values = ResolveAvailableValues(availableValues);
        if (values.Count == 0)
        {
            return new List<QuickFilterChoice<TValue>>();
        }

        displayNameSelector ??= value => value?.ToString() ?? string.Empty;
        return values
            .Select(value => new QuickFilterChoice<TValue>(value, displayNameSelector(value)))
            .ToList();
    }

    private static List<TValue> ResolveAvailableValues(IEnumerable<TValue>? availableValues)
    {
        if (availableValues is not null)
        {
            return availableValues.ToList();
        }

        if (typeof(TValue).IsEnum)
        {
            return Enum.GetValues(typeof(TValue)).Cast<TValue>().ToList();
        }

        return [];
    }
}
