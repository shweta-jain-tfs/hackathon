using Pharos.Common.Web.Components.DataGrid.Columns;
using Pharos.Common.Web.Models.DataGrid;

namespace Pharos.Common.Web.Components.DataGrid.Filters.Models;

public class KeyValueListColumnFilter<TGridItem, TProp> : ColumnFilterBase<TGridItem, TProp, KeyValueListFilterState>
{
    private readonly Func<Dictionary<string, string>?> _dataSourceProvider;
    private bool _hasValuesFromDataSource;
    private List<(string Key, string DisplayName)>? _dataSourceSnapshot;
    private List<SelectableKeyValue> _selectableStringValues = [];

    /// <summary>
    /// Raised when a data source refresh dropped keys the user had selected, which means the grid
    /// has to re-evaluate its filtering.
    /// </summary>
    internal event Action? SelectionInvalidated;

    /// <summary>
    /// The values offered by the filter.
    /// <para>
    /// Reading this pulls the current data source. The source is typically populated by an async
    /// load that finishes after the column's first render, so pulling on read is what lets the
    /// filter pick those values up without the column having to watch its parameters.
    /// </para>
    /// </summary>
    public List<SelectableKeyValue> SelectableStringValues
    {
        get
        {
            SyncWithDataSource();
            return _selectableStringValues;
        }
    }

    /// <summary>
    /// Creates a filter over a data source that may be replaced or populated later. The provider is
    /// re-evaluated on every read, so the filter always sees the caller's current values.
    /// </summary>
    public KeyValueListColumnFilter(Column<TGridItem, TProp> column, Func<Dictionary<string, string>?> dataSourceProvider) : base(column)
    {
        LogicalOperator = LogicalFilterOperator.Or;
        _dataSourceProvider = dataSourceProvider;
        SyncWithDataSource();
    }

    /// <summary>
    /// Creates a filter over a fixed set of values.
    /// </summary>
    public KeyValueListColumnFilter(Column<TGridItem, TProp> column, Dictionary<string, string> dataSource)
        : this(column, () => dataSource)
    {
    }

    /// <summary>
    /// Re-reads the data source and rebuilds the selectable values if they actually changed.
    /// </summary>
    private void SyncWithDataSource()
    {
        var dataSource = _dataSourceProvider();

        if (dataSource is null)
        {
            return;
        }

        var snapshot = dataSource.Select(kvp => (kvp.Key, kvp.Value)).ToList();

        if (_dataSourceSnapshot is not null && _dataSourceSnapshot.SequenceEqual(snapshot))
        {
            return;
        }

        // Recorded before rebuilding so that SetSelectableValues, which reads the backing field,
        // cannot re-enter this method.
        _dataSourceSnapshot = snapshot;

        var oldSelectionCount = _selectableStringValues.Count(x => x.IsSelected);
        SetSelectableValues(dataSource);

        if (_selectableStringValues.Count(x => x.IsSelected) != oldSelectionCount)
        {
            SelectionInvalidated?.Invoke();
        }
    }

    private void ApplyDefaultValue()
    {
        // The data source can load asynchronously, so the default is re-offered on every refresh.
        // The base class owns the "have I already acted on this default?" guard.
        if (!DefaultHasChanged())
        {
            return;
        }

        // A single string default and a multi-select default are both just a set of keys here.
        var keys = EffectiveQuickFilterDefault switch
        {
            IEnumerable<string> defaultValues => defaultValues.ToHashSet(),
            string defaultValue => [defaultValue],
            _ => new HashSet<string>()
        };

        if (keys.Count == 0)
        {
            return;
        }

        if (_hasValuesFromDataSource)
        {
            foreach (var row in _selectableStringValues.Where(x => keys.Contains(x.Key)))
            {
                row.IsSelected = true;
            }
        }
        else
        {
            // Data source not loaded yet: keep the default keys temporarily so filtering still
            // works until the real values arrive.
            foreach (var key in keys)
            {
                _selectableStringValues.Add(new SelectableKeyValue(key, key) { IsSelected = true });
            }
        }
    }

    private void SetSelectableValues(Dictionary<string, string> dataSource)
    {
        _hasValuesFromDataSource = dataSource.Count > 0;

        // Preserve already-selected keys when the datasource refreshes.
        var existingSelections = _selectableStringValues
            .Where(x => x.IsSelected)
            .Select(x => x.Key)
            .ToHashSet();

        _selectableStringValues = dataSource.Select(kvp =>
            new SelectableKeyValue(kvp.Key, kvp.Value)
            {
                // Copy selections from old values if they exist in the new list (e.g. filter set from state before initialized)
                IsSelected = existingSelections.Contains(kvp.Key)
            }).ToList();

        ApplyDefaultValue();
    }

    public override IEnumerable<PropertyFilterExpression> FilterExpressions
    {
        get
        {
            foreach (var selectable in SelectableStringValues)
            {
                if (selectable.IsSelected)
                {
                    yield return new PropertyFilterExpression<string>(Column.PropertyName, PropertyFilterOperator.EqualsTo, selectable.Key);
                }
            }
        }
    }

    public override int ActiveFilterCount => SelectableStringValues.Count(x => x.IsSelected);

    public override void Clear()
    {
        SelectableStringValues.ForEach(x => x.IsSelected = false);
        OnClearInvoked();
    }

    public override void ApplyDefault()
    {
        SelectableStringValues.ForEach(x => x.IsSelected = false);
        ForgetTrackedDefault(); // re-applying the same default is the point of this call
        ApplyDefaultValue();
    }

    protected override void ApplyTypedState(KeyValueListFilterState state)
    {
        // Read through the property first so _hasValuesFromDataSource reflects the current source.
        var selectableValues = SelectableStringValues;

        if (_hasValuesFromDataSource)
        {
            var selectedKeysSet = state.SelectedKeys.ToHashSet();

            foreach (var selectableValue in selectableValues)
            {
                selectableValue.IsSelected = selectedKeysSet.Contains(selectableValue.Key);
            }
        }
        else
        {
            // Multiselect filter that hasn't been initialized with values yet.
            // Set values from state as active filters until the real values are loaded.
            // These values are used in Column.razor.cs when the filter values are updated.
            var filterValuesFromstate = state.SelectedKeys
                .Select(x => new SelectableKeyValue(key: x, displayName: x)
                {
                    IsSelected = true
                });

            selectableValues.Clear(); // clear possible default value
            selectableValues.AddRange(filterValuesFromstate);
        }
    }

    protected override KeyValueListFilterState CreateState()
    {
        return new KeyValueListFilterState
        {
            // Only serialize selected keys as a list of strings
            SelectedKeys = SelectableStringValues.Where(x => x.IsSelected).Select(x => x.Key).ToList()
        };
    }
    }

public class SelectableKeyValue(string key, string displayName)
{
    public bool IsSelected { get; set; }
    public string Key { get; } = key;
    public string DisplayName { get; } = displayName;
}
