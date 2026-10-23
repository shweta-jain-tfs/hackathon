using Pharos.Common.Web.Components.DataGrid.Columns;
using Pharos.Common.Web.Models.DataGrid;

namespace Pharos.Common.Web.Components.DataGrid.Filters.Models;

public abstract class ColumnFilterBase<TGridItem, TProp> : IColumnFilter
{
    protected ColumnFilterBase(Column<TGridItem, TProp> column)
    {
        Column = column;
    }

    public Column<TGridItem, TProp> Column { get; set; }

    public string PropertyName => Column.PropertyName;

    public LogicalFilterOperator LogicalOperator { get; set; }

    public abstract IEnumerable<PropertyFilterExpression> FilterExpressions { get; }

    public virtual int ActiveFilterCount => FilterExpressions.Count();

    /// <summary>
    /// Incremented each time the filter is externally reset (e.g. clear, reset to default).
    /// Quick filter components use this to detect when to clear their internal state.
    /// </summary>
    public int ResetRevision { get; private set; }

    /// <summary>
    /// Returns the quick filter default only if this column is a quick filter.
    /// Prevents applying defaults to filters without a clear/reset option.
    /// </summary>
    protected object? EffectiveQuickFilterDefault => Column.QuickFilter ? Column.QuickFilterDefault : null;

    /// <summary>
    /// Snapshot of the quick filter default the last time <see cref="DefaultHasChanged"/> reported
    /// a change. <see cref="_hasTrackedDefault"/> is needed separately because <c>null</c> is a
    /// legitimate default value and must be distinguishable from "never looked at it".
    /// </summary>
    private object? _trackedDefault;

    private bool _hasTrackedDefault;

    public event EventHandler? OnClear;

    /// <summary>
    /// Creates a filter expression for this column's property.
    /// </summary>
    private protected PropertyFilterExpression<T> Expression<T>(PropertyFilterOperator filterOperator, T value) =>
        new(Column.PropertyName, filterOperator, value);

    public abstract void Clear();

    public abstract void ApplyDefault();

    public void ResetToDefault()
    {
        if (EffectiveQuickFilterDefault is not null)
        {
            ApplyDefault();
        }
        else
        {
            Clear();
        }
    }

    /// <summary>
    /// Reports whether the quick filter default differs from the last one this filter acted on, and
    /// records the new value.
    /// <para>
    /// Filters bound to an asynchronously loaded data source get their defaults re-offered on every
    /// refresh, so without this guard a default would be re-applied over a selection the user had
    /// since changed. Collections are compared by set equality because the default is a selection,
    /// not a sequence.
    /// </para>
    /// </summary>
    protected bool DefaultHasChanged()
    {
        var current = EffectiveQuickFilterDefault;

        if (_hasTrackedDefault && DefaultsAreEquivalent(_trackedDefault, current))
        {
            return false;
        }

        _trackedDefault = current;
        _hasTrackedDefault = true;
        return true;
    }

    /// <summary>
    /// Forgets the tracked default so the next <see cref="DefaultHasChanged"/> call reports a
    /// change. Used by <see cref="ApplyDefault"/> implementations, where re-applying the same
    /// default is the explicitly requested behavior.
    /// </summary>
    protected void ForgetTrackedDefault()
    {
        _trackedDefault = null;
        _hasTrackedDefault = false;
    }

    private static bool DefaultsAreEquivalent(object? left, object? right)
    {
        if (left is IEnumerable<string> leftKeys && right is IEnumerable<string> rightKeys)
        {
            return leftKeys.ToHashSet().SetEquals(rightKeys);
        }

        return Equals(left, right);
    }

    public virtual Task LoadAsync()
    {
        return Task.CompletedTask;
    }

    public virtual void OnClearInvoked()
    {
        ResetRevision++;
        OnClear?.Invoke(null, new EventArgs());
    }

    /// <summary>
    /// Restores the filter from previously saved state.
    /// Implementations pattern-match on their own state type and ignore anything else, so a state
    /// blob belonging to a different filter type can never be half-applied.
    /// </summary>
    public abstract void ApplyState(ColumnFilterState? stateObject);

    /// <summary>
    /// Captures the current filter selection as its concrete state type.
    /// </summary>
    public abstract ColumnFilterState SaveState();
}

/// <summary>
/// Base class for filters that persist their selection as a specific <typeparamref name="TState"/>.
/// <para>
/// It exists so every filter does not have to repeat the same two pieces of plumbing: the type
/// guard that rejects state belonging to another filter, and stamping <c>PropertyName</c> onto the
/// saved state. Derived filters only describe their own values via
/// <see cref="ApplyTypedState"/> and <see cref="CreateState"/>.
/// </para>
/// <para>
/// It deliberately derives from the two-parameter <see cref="ColumnFilterBase{TGridItem, TProp}"/>
/// so existing component constraints such as <c>PhColumnFilter</c> keep working unchanged.
/// </para>
/// </summary>
public abstract class ColumnFilterBase<TGridItem, TProp, TState> : ColumnFilterBase<TGridItem, TProp>
    where TState : ColumnFilterState
{
    protected ColumnFilterBase(Column<TGridItem, TProp> column)
        : base(column)
    {
    }

    /// <summary>
    /// Applies state that is already known to belong to this filter.
    /// </summary>
    protected abstract void ApplyTypedState(TState state);

    /// <summary>
    /// Builds the filter's own state. <c>PropertyName</c> is filled in by the base class, so
    /// implementations only describe their values.
    /// </summary>
    protected abstract TState CreateState();

    public sealed override void ApplyState(ColumnFilterState? stateObject)
    {
        // Anything that is not this filter's state type is ignored entirely, which prevents a
        // mismatched blob from being partially applied.
        if (stateObject is TState state)
        {
            ApplyTypedState(state);
        }
    }

    public sealed override ColumnFilterState SaveState()
    {
        var state = CreateState();
        state.PropertyName = PropertyName;
        return state;
    }
}