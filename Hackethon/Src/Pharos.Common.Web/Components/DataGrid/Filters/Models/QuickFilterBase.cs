using Pharos.Common.Web.Models.DataGrid;

namespace Pharos.Common.Web.Components.DataGrid.Filters.Models;

/// <summary>
/// Shared plumbing for reusable quick-filter models.
/// </summary>
public abstract class QuickFilterBase : IColumnFilter
{
    protected QuickFilterBase(string propertyName, string title, LogicalFilterOperator logicalOperator)
    {
        PropertyName = propertyName;
        Title = title;
        LogicalOperator = logicalOperator;
    }

    public event EventHandler? OnClear;

    public string PropertyName { get; }

    public LogicalFilterOperator LogicalOperator { get; }

    public string Title { get; }

    public abstract IEnumerable<PropertyFilterExpression> FilterExpressions { get; }

    public abstract int ActiveFilterCount { get; }

    public int ResetRevision { get; private set; }

    public virtual Task LoadAsync() => Task.CompletedTask;

    public abstract void Clear();

    public abstract void ResetToDefault();

    public void OnClearInvoked()
    {
        ResetRevision++;
        OnClear?.Invoke(this, EventArgs.Empty);
    }

    public abstract void ApplyState(ColumnFilterState? stateObject);

    public abstract ColumnFilterState SaveState();
}
