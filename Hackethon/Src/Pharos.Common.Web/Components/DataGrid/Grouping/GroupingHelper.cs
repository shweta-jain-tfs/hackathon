using Pharos.Common.Web.Components.DataGrid.Columns;

namespace Pharos.Common.Web.Components.DataGrid.Grouping;

/// <summary>
/// Grouping state for a single cell (column) in the last row processed by
/// <see cref="GroupingHelper{TGridItem}.ComputeGroupStatesForRow" />.
/// The state is used to decide whether the cell value should render (and optionally how grouping borders are drawn).
/// </summary>
internal enum GroupState
{
    /// <summary>
    /// The column is not grouped (or grouping was not evaluated for the current row).
    /// </summary>
    None,

    /// <summary>
    /// Render the cell value for this row because this column changed,
    /// or because a grouped column to the left changed (which restarts grouping for columns to the right).
    /// </summary>
    Start,

    /// <summary>
    /// Do not render the cell value for this row because it is grouped with the row above
    /// and no grouped column to the left forced a restart.
    /// </summary>
    Same
}

internal sealed class GroupingHelper<TGridItem>
{
    private int _lastRowIndex = -1;

    private readonly Dictionary<ColumnBase<TGridItem>, object?> _lastValueByColumn = new();

    // Stores group state for the last evaluated row. Queried once per cell during rendering.
    private readonly Dictionary<ColumnBase<TGridItem>, GroupState> _stateByColumn = new();

    public void Reset()
    {
        _stateByColumn.Clear();
        _lastValueByColumn.Clear();
        _lastRowIndex = -1;
    }

    /// <summary>
    /// Computes grouping state for the given row.
    /// Must be called once per row before calling <see cref="GetGroupState" /> for each cell.
    /// </summary>
    public void ComputeGroupStatesForRow(
        int rowIndex,
        TGridItem item,
        IEnumerable<ColumnBase<TGridItem>> orderedColumns,
        bool enabled = true)
    {
        _stateByColumn.Clear();

        if (!enabled)
        {
            _lastValueByColumn.Clear();
            _lastRowIndex = -1;
            return;
        }

        if (rowIndex == 0 || rowIndex != _lastRowIndex + 1)
        {
            // Don't compare to last value if it's the first row or if rows were skipped
            _lastValueByColumn.Clear();
        }

        _lastRowIndex = rowIndex;

        var leftGroupChanged = false;

        foreach (var column in orderedColumns)
        {
            if (!column.Grouped)
            {
                break; // Bail out early once we reach a non-grouped column.
            }

            var currentValue = column.GetValue(item);
            _lastValueByColumn.TryGetValue(column, out var lastValue);

            var isStart = leftGroupChanged || !Equals(currentValue, lastValue);
            _stateByColumn[column] = isStart ? GroupState.Start : GroupState.Same;

            if (isStart)
            {
                _lastValueByColumn[column] = currentValue;
                leftGroupChanged = true;
            }
        }
    }

    /// <summary>
    /// Gets the grouping state for <paramref name="column"/> in the last row processed by
    /// <see cref="ComputeGroupStatesForRow" />.
    /// Intended to be called for each rendered cell after evaluating the row.
    /// Returns <see cref="GroupState.None"/> when the column isn't grouped or the row hasn't been evaluated.
    /// </summary>
    public GroupState GetGroupState(ColumnBase<TGridItem> column)
    {
        if (_stateByColumn.TryGetValue(column, out var state))
        {
            return state;
        }

        return GroupState.None;
    }
}