using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.JSInterop;
using Pharos.Common.Web.Components.DataGrid.Columns;
using Pharos.Common.Web.Components.DataGrid.Grouping;
using Pharos.Common.Web.Components.DataGrid.Infrastructure;
using Pharos.Common.Web.Components.DataGrid.Selection;
using Pharos.Common.Web.Components.DataGrid.State;
using Pharos.Common.Web.Components.DockedButtonRow;
using Pharos.Common.Web.Components.FormElements.Enums;
using Pharos.Common.Web.Components.Icons;
using Pharos.Common.Web.Components.Loader;
using Pharos.Common.Web.Components.Modal;
using Pharos.Common.Web.Components.Status;
using Pharos.Common.Web.Components.TrailingEdge;
using Pharos.Common.Web.Contexts;
using Pharos.Common.Web.Extensions;
using Pharos.Common.Web.Models.DataGrid;
using Pharos.Common.Web.Services;
using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.DataGrid;

/// <summary>
/// A virtualized, stateful data grid with sorting, selection, and (ItemsProvider-only)
/// filtering, plus optional persisted view state.
/// </summary>
/// <remarks>
/// The grid initializes in two render passes to ensure filters and
/// persisted state are fully resolved before data is loaded.
///
/// 1) First render:
///    Columns and filter definitions are collected via the <see cref="Columns"/> RenderFragment.
///    The Virtualize component is not rendered, so no data is loaded.
///
/// 2) First OnAfterRenderAsync:
///    Persisted state is loaded, filters are initialized and applied, and the
///    grid is marked as initialized, triggering a second render.
///
/// 3) Second render:
///    The Virtualize component is rendered and begins fetching data using the
///    current sort and filters.
///
/// 4) After first data load:
///    One-shot post-render work may run (e.g. restoring scroll position).
///
/// Data reloads are render-driven: the parent can trigger StateHasChanged to
/// force a re-render, and once initialized the grid refreshes Virtualize from
/// OnParametersSetAsync (even when parameters are unchanged).
/// </remarks>
/// <typeparam name="TGridItem">Type of the items listed in the grid.</typeparam>
public partial class PhDataGrid<TGridItem> : IDisposable
{
    /// <summary>
    /// Injected service for managing loading states and progress indicators in the application.
    /// Used to show progress bars during long-running operations like selecting all items.
    /// </summary>
    [Inject] private LoaderService LoaderService { get; set; } = default!;
    /// <summary>
    /// Injected service for persisting and restoring component state across page navigations.
    /// Manages saving and loading grid filters, sorting, and scroll position.
    /// </summary>
    [Inject] private IComponentStateService ComponentStateService { get; set; } = default!;


    [CascadingParameter(Name = "UiContext")] public UiContext? UiContext { get; set; }

    /// <summary>
    /// Optional cascading parameter that provides the parent PhDockedButtonRow component.
    /// Used to register a callback for updating the selected count without triggering re-renders.
    /// </summary>
    [CascadingParameter(Name = "DockedButtonRow")]
    public PhDockedButtonRow? DockedButtonRow { get; set; }

    /// <summary>
    /// Optional cascading parameter that provides the parent PhModalContent component.
    /// Used to register a callback for updating the selected count without triggering re-renders.
    /// </summary>
    [CascadingParameter(Name = "ModalContent")]
    public PhModalContent? ModalContent { get; set; }


    /// <summary>
    /// Unique identifier for persisting this data grid's state across page navigation.
    /// When set, the grid automatically saves and restores filters, sorting, and scroll position.
    /// Set to null or empty to disable state persistence.
    /// </summary>
    /// <remarks>
    /// Must be unique among all grids on the same page.
    /// Must be a constant string (not generated or dynamic)
    /// Grids in modal dialogs usually don't need state persistence.
    /// Grids containing links to other pages should use state persistence.
    /// This property is required since most grids need state persistence.
    /// </remarks>
    [Parameter, EditorRequired]
    public string? StateID { get; set; }

    /// <summary>
    /// ItemsProvider is for when you want to get part of a list from an api and then fetch more when you scroll through the list.
    /// </summary>
    [Parameter]
    public GridItemsProvider<TGridItem>? ItemsProvider { get; set; }

    /// <summary>
    /// Items is for when you have a small list of items you want to display without filtering.
    /// </summary>
    [Parameter]
    public IQueryable<TGridItem>? Items { get; set; }

    /// <summary>
    /// The RenderFragment for the Columns defined in the html-file.
    /// </summary>
    [Parameter]
    public RenderFragment? Columns { get; set; }

    /// <summary>
    /// Possibility to add a custom header for the DataGrid. Like a custom filter.
    /// </summary>
    [Parameter]
    public RenderFragment? HeaderContent { get; set; }

    /// <summary>
    /// Space for icon buttons for performing actions related to the data grid, e.g. enlarge/shrink
    /// </summary>
    [Parameter]
    public RenderFragment? Actions { get; set; }

    /// <summary>
    /// DefaultSortColumn has to be a column the exits in the added type (TGridItem)
    /// By default the first column is the column the data is sorted on.
    /// </summary>
    [Parameter]
    public string DefaultSortColumn { get; set; } = default!;

    /// <summary>
    /// If you want to sort on another direction that the default sort direction for the type.
    /// </summary>
    [Parameter]
    public SortDirection DefaultSortDirection { get; set; } = SortDirection.Ascending;

    /// <summary>
    /// Shows the checkboxes and allow you to select multiple rows.
    /// </summary>
    [Parameter]
    public SelectionTypeEnum SelectionType { get; set; } = SelectionTypeEnum.Off;

    /// <summary>
    /// Returns an event with the selected items when multiselect is active.
    /// </summary>
    [Parameter]
    public EventCallback<HashSet<TGridItem>> OnSelectedItemsChanged { get; set; }

    /// <summary>
    /// Returns the event when a row is selected in the DataGrid.
    /// </summary>
    [Parameter]
    public EventCallback<TGridItem?> OnSelectedItemChanged { get; set; }

    /// <summary>
    /// Suppresses the automatic data refresh performed on parameter updates after initialization.
    /// Allows parent components to rerender row content without requerying the grid data.
    /// </summary>
    [Parameter]
    public bool SuppressRefresh { get; set; }

    /// <summary>
    /// RowType sets the styling for the datagrid rows.
    /// </summary>
    [Parameter]
    public RowTypeEnum RowType { get; set; } = RowTypeEnum.ZebraStripes;

    /// <summary>
    /// Row height. Also affects padding and font size.
    /// </summary>
    [Parameter]
    public RowHeightEnum RowHeight { get; set; } = RowHeightEnum.Relaxed;

    /// <summary>
    /// TableTitle shows a title for the DataGrid and the number of rows in the grid.
    /// The total row count is automatically added to the title
    /// unless <see cref="HideTotalCount"/> is set to true.
    /// </summary>
    [Parameter]
    public string? TableTitle { get; set; }

    /// <summary>
    /// Set to true to not append the total row count to the title.
    /// </summary>
    [Parameter]
    public bool HideTotalCount { get; set; }

    /// <summary>
    /// Optional custom quick filters rendered together with the column quick filters.
    /// </summary>
    /// <remarks>
    /// Initialization-only: the filters are read once when the grid initializes.
    /// Replacing the collection or its filter instances afterwards has no effect.
    /// Change filter values via the filter instances instead.
    /// </remarks>
    [Parameter]
    public IEnumerable<IQuickFilterSource>? CustomQuickFilters { get; set; }

    /// <summary>
    /// (Optional) Size of the title shown in the DataGrid header.
    /// The default is a header sized (H3) for grids placed as part of the page content
    /// (inside tab controls, under breadcrumbs).
    /// Set to <see cref="HeaderSizeEnum.WholePage" /> for a larger (H2) header for use
    /// on pages where the grid title is the title of the page (full page grids).
    /// </summary>
    [Parameter] public HeaderSizeEnum HeaderSize { get; set; } = HeaderSizeEnum.PartOfPage;

    /// <summary>
    /// Show a status symbol in the header.
    /// </summary>
    [Parameter]
    public StatusType ShowStatusSymbolInHeader { get; set; } = StatusType.None;

    /// <summary>
    /// (Optional) Custom style options for some of the items in the grid.
    /// </summary>
    [Parameter]
    public ItemStyleOptions<TGridItem>? ItemStyleOptions { get; set; }

    /// <summary>
    /// (Optional) Highlighting options for rows in the grid.
    /// </summary>
    [Parameter] public HighlightOptions<TGridItem>? HighlightOptions { get; set; }

    /// <summary>
    /// Disable trailing edge when scrolling.
    /// </summary>
    [Parameter]
    public bool HideTrailingEdge { get; set; }

    /// <summary>
    /// Custom text to display in the progress bar during long-running operations, such as SelectAll.
    /// If not set, a default message may be used.
    /// </summary>
    [Parameter] public string? ProgressBarText { get; set; }

    /// <summary>
    /// When set, the grid will scroll to and center the row matching this key after data loads.
    /// Requires <see cref="ScrollToItemIndexResolver"/> to be set.
    /// Changing this value triggers a new scroll.
    /// </summary>
    [Parameter] public string? ScrollToItemKey { get; set; }

    /// <summary>
    /// Resolves a <see cref="ScrollToItemKey"/> to a zero-based row index.
    /// Called once each time <see cref="ScrollToItemKey"/> changes.
    /// </summary>
    [Parameter] public Func<string, Task<int>>? ScrollToItemIndexResolver { get; set; }

    private InternalGridContext<TGridItem> _internalGridContext;
    private readonly List<ColumnBase<TGridItem>> _columns = [];
    private IReadOnlyList<ColumnBase<TGridItem>> _orderedColumns = [];
    private IReadOnlyList<ColumnBase<TGridItem>> _visibleOrderedColumns = [];
    private Virtualize<(int, TGridItem)>? _virtualizeComponent;
    private PhDataGridTitle? _titleRef;
    private PhDataGridLoadingOverlay? _loadingOverlayRef;
    private ElementReference? _gridContainerRef;

    private StateHelper _stateHelper = default!;
    private SelectionManager<TGridItem> _selectionManager = default!;
    private readonly GroupingHelper<TGridItem> _groupingHelper = new();
    private IJSObjectReference? _script;

    private string _sortByColumn = string.Empty;
    private bool _sortByAscending;
    private List<IColumnFilter>? _filters;

    /// <summary>
    /// Number of extra rows to render above and below the visible content.
    /// </summary>
    private const int _overScanCount = 30;
    private const float _itemSize = 56;
    private int _totalRowCount;
    private bool _hasReceivedFirstResult;
    private bool _skipNextDataLoad;
    // Deselecting rows that became disabled or hidden must not skip the data refresh.
    private bool _isPruningSelection;
    private DateTimeOffset _lastTimeLoaded;
    private readonly TimeSpan _debounce = TimeSpan.FromMilliseconds(300);
    private bool _hasScrolledToItem;

    private TGridItem? _highlightedItem;

    /// <summary>
    /// Pending exception to be rethrown from OnAfterRenderAsync so it can be caught by an error boundary.
    /// </summary>
    private Exception? _pendingError;

    private bool _disposed;

    private bool IsAllSelected => _selectionManager.IsAllSelected(_totalRowCount);
    private bool IsPartiallySelected => _selectionManager.IsPartiallySelected(_totalRowCount);

    // header checkbox should be "checked" while select-all is running OR when all are selected
    private bool IsSelectAllChecked => _selectionManager.IsSelectAllInProgress || IsAllSelected || IsSelectAllIndeterminate;

    // don't show indeterminate while select-all is running
    private bool IsSelectAllIndeterminate => !_selectionManager.IsSelectAllInProgress && IsPartiallySelected;

    /// <summary>
    /// True once columns are collected and filters are initialized.
    /// Data loading must not start before this is true.
    /// </summary>
    private bool IsInitialized { get; set; }

    private IconSize SortIconSize { get; set; } = IconSize.Sm;
    private CheckBoxSize SelectCheckboxSize { get; set; } = CheckBoxSize.Large;

    protected string CssClassGrid => new CssBuilder("ph-datagrid")
        .AddClass("ph-datagrid--zebra-stripes", RowType == RowTypeEnum.ZebraStripes)
        .AddClass("ph-datagrid--lines-between-rows", RowType == RowTypeEnum.LinesBetweenRows)
        .AddClass("ph-datagrid--compact", RowHeight == RowHeightEnum.Compact)
        .AddClass("ph-datagrid--minimal", RowHeight == RowHeightEnum.Minimal)
        .AddClass("ph-datagrid--whole-page-header", HeaderSize == HeaderSizeEnum.WholePage)
        .Build();

    private bool ShowHeader => HasQuickFilters || !string.IsNullOrEmpty(TableTitle) || Actions != null;
    private bool HasQuickFilters => FilterSources.Any(c => c.QuickFilter);

    /// <summary>
    /// Snapshot of column filters and custom quick filters, taken during initialization.
    /// Used by both UI and data loading so they always stay in sync.
    /// </summary>
    private List<IQuickFilterSource>? _filterSources;

    /// <summary>
    /// Column filters and custom quick filters merged into one list.
    /// Before initialization this is computed live (columns are still being collected);
    /// after initialization the cached snapshot is used.
    /// </summary>
    private List<IQuickFilterSource> FilterSources => _filterSources ?? BuildFilterSources();

    private List<IQuickFilterSource> BuildFilterSources()
    {
        var customFilters = CustomQuickFilters?.ToList() ?? [];

        return customFilters.Where(f => f.Position == CustomQuickFilterPosition.Start)
            .Concat(_columns.Cast<IQuickFilterSource>())
            .Concat(customFilters.Where(f => f.Position == CustomQuickFilterPosition.End))
            .ToList();
    }

    private bool HasFrozenColumn => SelectionType != SelectionTypeEnum.Off;
    private DataGridStrings Strings { get; set; } = DataGridStrings.Default;


    public PhDataGrid()
    {
        _internalGridContext = new InternalGridContext<TGridItem>(this);
    }


    protected override void OnInitialized()
    {
        _stateHelper = new StateHelper(ComponentStateService);
        _selectionManager = new SelectionManager<TGridItem>(LoaderService, CanSelectItem);

        _selectionManager.OnSelectAllCompleted += SelectAllCompleted;
        _selectionManager.OnSelectionChanged += NotifySelectionChanged;
    }

    protected override async Task OnParametersSetAsync()
    {
        // Only check selected rows when styling could make their checkboxes disabled or hidden.
        if (ItemStyleOptions is not null && _selectionManager.SelectedCount > 0)
        {
            // Check the latest data too, since a selected row may have been replaced with an updated object.
            RemoveUnselectableItems(
                _selectionManager.GetSelectedItems().Concat(Items?.AsEnumerable() ?? []));
        }

        Strings = UiContext?.ComponentStrings.DataGrid ?? DataGridStrings.Default;
        if (!string.IsNullOrEmpty(ProgressBarText))
        {
            Strings = Strings with
            {
                ProgressBarText = ProgressBarText
            };
        }

        if (SelectionType != SelectionTypeEnum.Off || (HighlightOptions?.Mode == HighlightMode.Internal))
        {
            if (!typeof(IEquatable<TGridItem>).IsAssignableFrom(typeof(TGridItem)))
            {
                _pendingError = new InvalidOperationException("The type used for the data grid must implement IEquatable to support selection");
            }
        }

        if (HighlightOptions is not null)
        {
            _highlightedItem = default;

            if (HighlightOptions.Mode == HighlightMode.ExternalState && HighlightOptions.IsHighlighted is null)
            {
                _pendingError = new InvalidOperationException($"{nameof(HighlightOptions.IsHighlighted)} must be set in {nameof(HighlightMode.ExternalState)}");
            }
        }

        SortIconSize = RowHeight == RowHeightEnum.Minimal ? IconSize.Xs : IconSize.Sm;
        SelectCheckboxSize = RowHeight == RowHeightEnum.Minimal ? CheckBoxSize.Small : CheckBoxSize.Large;

        // Don't refresh the grid unless it's done initializing.
        // Some parents need to rerender row content without requerying while a side panel is open.
        if (IsInitialized && !SuppressRefresh)
        {
            await RefreshDataCoreAsync();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _script = await ImportJSAsync();
            _stateHelper.StartScrollMonitoring(_gridContainerRef, _script);

            await FinalizeInitializationAsync();
        }

        if (_hasReceivedFirstResult && _stateHelper.HasPendingScrollRestore)
        {
            await _stateHelper.TryRestoreScrollPosition(_gridContainerRef, _script);
        }

        if (_hasReceivedFirstResult && ScrollToItemKey is not null && ScrollToItemIndexResolver is not null)
        {
            await TryScrollToItemAsync();
        }

        if (_pendingError is not null)
        {
            // throw the pending error to be caught by an error boundary
            var error = _pendingError;
            _pendingError = null;
            throw error;
        }
    }

    private async Task TryScrollToItemAsync()
    {
        if (_hasScrolledToItem)
        {
            return;
        }

        var key = ScrollToItemKey!;
        var targetIndex = await ScrollToItemIndexResolver!(key);
        await PhDataGridScrollHelper.ScrollToRowAsync(JS, targetIndex, gridContainerRef: _gridContainerRef);
        _hasScrolledToItem = true;
    }

    /// <summary>
    /// Use this with the @ref to clear selected items in the DataGrid and update the SelectedItemsCount
    /// Optional exceptItems to keep certain items selected.
    /// </summary>
    public Task ClearSelectedItem(List<TGridItem>? exceptItems = null)
    {
        if (exceptItems is null)
        {
            _selectionManager.ClearSelection();
        }
        else
        {
            _selectionManager.SetSelectedItems(exceptItems);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Use this to programmatically selects the specified items in the grid, replacing the current selection.
    /// Selection must be enabled; either <see cref="SelectionTypeEnum.MultiSelect"/> or <see cref="SelectionTypeEnum.MultiSelectWithSelectAll"/>
    /// in <see cref="SelectionTypeEnum.SingleSelect"/> mode only a single item can be selected.
    /// Items with disabled or hidden selection checkboxes are excluded.
    /// </summary>
    /// <param name="itemsToSelect">The items that should be selected.</param>
    /// <exception cref="InvalidOperationException">Thrown when selection is disabled for this grid.</exception>
    /// <exception cref="InvalidOperationException">Thrown when multiple items are provided while in single-select mode.</exception>
    public Task SetSelectItems(List<TGridItem> itemsToSelect)
    {
        if (SelectionType == SelectionTypeEnum.Off)
        {
            throw new InvalidOperationException("Selection is disabled for this grid.");
        }

        if (SelectionType == SelectionTypeEnum.SingleSelect && itemsToSelect is { Count: > 1 })
        {
            throw new InvalidOperationException("Cannot select multiple items when SelectionType is SingleSelect.");
        }

        _selectionManager.SetSelectedItems(itemsToSelect);

        return Task.CompletedTask;
    }

    private void RebuildColumnCaches()
    {
        var ordered = _columns
            .OrderBy(x => x.ColumnOrder)
            .ToList();

        _orderedColumns = ordered;
        _visibleOrderedColumns = ordered.Where(x => x.Visible).ToList();
    }

    /// <summary>
    /// Called from <see cref="Column{TGridItem,TProp}"/> components to register themselves with the grid.
    /// </summary>
    /// <param name="column"></param>
    internal void AddColumn(ColumnBase<TGridItem> column)
    {
        if (IsInitialized)
        {
            return;
        }

        if (!string.IsNullOrEmpty(DefaultSortColumn) && column.PropertyName == DefaultSortColumn)
        {
            _sortByColumn = column.PropertyName;
            _sortByAscending = DefaultSortDirection == SortDirection.Ascending;
        }

        _columns.Add(column);
    }

    private async Task FinalizeInitializationAsync()
    {
        var state = await _stateHelper.LoadState(StateID);
        InitFilters(); // init filters before applying state
        ApplyState(state);

        // Columns have now been collected and filters are initialized.
        // The grid is ready to load data.

        IsInitialized = true;
        StateHasChanged(); // Single re-render to show Virtualize component
    }

    /// <summary>
    /// Get all filterable filters from columns after the columns have been initialized.
    /// At this point the default values in the filters are set.
    /// </summary>
    private void InitFilters()
    {
        _filterSources = BuildFilterSources();

        _filters = _filterSources
            .Where(source => source is {
                SupportsFiltering: true,
                QuickFilter: true,
                Filter: not null,
                QuickFilterComponentType: not null
            })
            .Select(source => source.Filter!)
            .ToList();

        var duplicatePropertyNames = _filters
            .GroupBy(filter => filter.PropertyName)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (duplicatePropertyNames.Count > 0)
        {
            throw new InvalidOperationException($"Filter PropertyName values must be unique. Duplicate values: {string.Join(", ", duplicatePropertyNames)}.");
        }
    }

    private void ApplyState(DataGridState? state)
    {
        if (state == null)
        {
            // No state could be loaded for this component
            return;
        }

        if (!string.IsNullOrEmpty(state.SortColumn))
        {
            _sortByColumn = state.SortColumn;
            _sortByAscending = state.SortAscending;
        }

        StateHelper.ApplyColumnFilters(_filters, state);
    }


    private string AriaSortValue(ColumnBase<TGridItem> column)
    {
        if (string.IsNullOrEmpty(_sortByColumn))
        {
            _sortByColumn = column.PropertyName;
        }

        return _sortByColumn.Equals(column.PropertyName, StringComparison.Ordinal)
            ? (_sortByAscending ? "ascending" : "descending")
            : "none";
    }

    private TrailingEdgeDisplayMode GetTrailingEdgeDisplayMode()
    {
        if (HideTrailingEdge)
        {
            return TrailingEdgeDisplayMode.Disabled;
        }

        if (HasFrozenColumn)
        {
            return TrailingEdgeDisplayMode.HideTopAndLeftEdge;
        }

        return TrailingEdgeDisplayMode.HideTopEdge;
    }

    private bool IsSelected(TGridItem item)
    {
        return _selectionManager.IsItemSelected(item);
    }

    private void CheckedChanged(TGridItem item, bool isChecked)
    {
        if (isChecked)
        {
            _selectionManager.SelectItem(item, SelectionType == SelectionTypeEnum.SingleSelect);
        }
        else
        {
            _selectionManager.DeselectItem(item);
        }
    }

    /// <summary>
    /// Gets the row style used for both display and selection checks.
    /// </summary>
    private ItemStyleFlags GetItemStyle(TGridItem item)
    {
        // A selector decides each row's style directly, without checking Match.
        if (ItemStyleOptions?.StyleSelector is { } selector)
        {
            return selector(item);
        }

        return ItemStyleOptions?.IsMatch(item) == true
            ? ItemStyleOptions.Style
            : ItemStyleFlags.None;
    }

    private bool CanSelectItem(TGridItem item)
    {
        // Dimming alone is allowed; disabled or hidden checkboxes must never be selected.
        const ItemStyleFlags blocked = ItemStyleFlags.Disabled | ItemStyleFlags.CheckboxDisabled | ItemStyleFlags.CheckboxHidden;
        return (GetItemStyle(item) & blocked) == 0;
    }

    /// <summary>
    /// Deselects disabled or hidden checkboxes while allowing updated row data to load.
    /// </summary>
    private void RemoveUnselectableItems(IEnumerable<TGridItem> items)
    {
        if (ItemStyleOptions is null || _selectionManager.SelectedCount == 0)
        {
            return;
        }

        // The selection callback needs to know this change comes from cleanup, not a checkbox click.
        _isPruningSelection = true;
        try
        {
            _selectionManager.RemoveUnselectableItems(items);
        }
        finally
        {
            // Treat later checkbox clicks normally, even if cleanup fails.
            _isPruningSelection = false;
        }
    }

    private Task ToggleSelectAll(bool isChecked)
    {
        // If unchecked OR partially selected (indeterminate),
        // clear the current selection to avoid triggering an expensive select-all.
        if (!isChecked || _selectionManager.SelectedCount > 0)
        {
            _selectionManager.ClearSelection();
            return Task.CompletedTask;
        }

        // Fire-and-forget; server path raises OnSelectAllCompleted when finished.
        _ = ExecuteSelectAll();
        return Task.CompletedTask;
    }

    private Task ExecuteSelectAll()
    {
        if (ItemsProvider is not null)
        {
            // Results are handled via the OnSelectAllCompleted event
            return _selectionManager.SelectAllFromServerAsync(
                ItemsProvider,
                _totalRowCount,
                _sortByColumn,
                _sortByAscending,
                _filters,
                Strings.ProgressBarText);
        }

        if (Items is not null)
        {
            _selectionManager.SelectAllFromItems(Items);
        }

        return Task.CompletedTask;
    }

    private bool IsHighlightedRow(TGridItem item)
    {
        if (HighlightOptions is null)
        {
            return false;
        }

        if (HighlightOptions.Mode == HighlightMode.ExternalState)
        {
            return HighlightOptions.IsHighlighted is not null && HighlightOptions.IsHighlighted(item);
        }

        return EqualityComparer<TGridItem>.Default.Equals(_highlightedItem, item);
    }

    private void ToggleRowHighlighted(TGridItem item)
    {
        if (HighlightOptions is null)
        {
            return;
        }

        _highlightedItem = IsHighlightedRow(item) ? default : item;
        if (HighlightOptions.HighlightedItemChanged is not null)
        {
            HighlightOptions.HighlightedItemChanged(_highlightedItem);
        }
    }

    private string? RowStyle(TGridItem item, bool isRowDisabled)
    {
        var rowClassNames = new CssBuilder("")
            .AddClass("ph-datagrid__row--highlighted", IsHighlightedRow(item))
            .AddClass("ph-datagrid__row--disabled", isRowDisabled)
            .Build();

        return string.IsNullOrEmpty(rowClassNames) ? null : rowClassNames;
    }


    private void SelectAllCompleted(SelectAllCompletedResult result)
    {
        if (result.HasError)
        {
            _pendingError = result.Error;
        }
        StateHasChanged();
    }


    private void NotifySelectionChanged()
    {
        var selectedItems = _selectionManager.GetSelectedItems();

        // Notify consumers
        if (SelectionType is SelectionTypeEnum.MultiSelect or SelectionTypeEnum.MultiSelectWithSelectAll)
        {
            if (OnSelectedItemsChanged.HasDelegate)
            {
                // Cleanup clears any pending reload skip so updated row data can load.
                _skipNextDataLoad = !_isPruningSelection;
                _ = OnSelectedItemsChanged.InvokeAsync([.. selectedItems]);
                SelectedItemsCountUpdate();
            }
        }
        else
        {
            if (OnSelectedItemChanged.HasDelegate)
            {
                // Apply the same reload rule when the grid allows only one selected row.
                _skipNextDataLoad = !_isPruningSelection;
                var firstItem = selectedItems.Count > 0 ? selectedItems[0] : default;
                _ = OnSelectedItemChanged.InvokeAsync(firstItem);
            }
        }
    }

    private void SelectedItemsCountUpdate()
    {
        var count = _selectionManager.SelectedCount;

        // Notify docked button row to update selected count
        DockedButtonRow?.RaiseSelectedCount(count);

        // Notify modal dialog to update selected count
        ModalContent?.RaiseSelectedCount(count);
    }


    private async Task RefreshDataCoreAsync()
    {
        if (_skipNextDataLoad)
        {
            _skipNextDataLoad = false;
            return;
        }

        _groupingHelper.Reset();

        if (_virtualizeComponent is not null)
        {
            // We have to go through its RefreshDataAsync API otherwise:
            // (1) It won't know to update its own internal state if the provider output has changed
            // (2) We won't know what slice of data to query for
            await _virtualizeComponent.RefreshDataAsync();
        }
    }

    private async ValueTask<ItemsProviderResult<(int, TGridItem)>> ProvideVirtualizedItems(ItemsProviderRequest request)
    {
        if (_columns.Count == 0 || !IsInitialized)
        {
            return new ItemsProviderResult<(int, TGridItem)>();
        }

        if (DateTimeOffset.Now - _lastTimeLoaded < _debounce)
        {
            // Debounce the requests. This eliminates a lot of redundant queries at the cost of slight lag after interactions.
            await Task.Delay(_debounce);
        }

        if (request.CancellationToken.IsCancellationRequested)
        {
            return default;
        }

        _lastTimeLoaded = DateTimeOffset.Now;

        var startIndex = request.StartIndex;
        var count = request.Count;

        if (count == 0)
        {
            // Grid is not visible and can't request items.
            return default;
        }

        var providerRequest = new GridItemsProviderRequest<TGridItem>(
            startIndex,
            count,
            _sortByColumn,
            _sortByAscending,
            _filters,
            request.CancellationToken);

        var providerResult = await ResolveItemsRequestAsync(providerRequest);

        if (!request.CancellationToken.IsCancellationRequested)
        {
            RemoveUnselectableItems(providerResult.Items);
            _totalRowCount = providerResult.TotalItemCount;
            if (_titleRef is not null)
            {
                var titleCount = providerResult.GroupedCount ?? providerResult.TotalItemCount;
                await _titleRef.SetRowCountAsync(titleCount);
            }

            //Maximum number of items that can be scrolled in the list where every items height is 56px
            // is 66589. Otherwise, the browser can not handle the height.
            const int maxScrollableRows = 66589;
            var maxScrollableRowCount = Math.Min(maxScrollableRows, _totalRowCount);

            await OnItemsReceivedAsync();

            // We're supplying the row index along with each row's data because we need it for aria-rowindex, and we have to account for
            // the virtualized start index.
            return new ItemsProviderResult<(int, TGridItem)>(
                items: providerResult.Items.Select((x, i) => ValueTuple.Create(i + request.StartIndex + 1, x)),
                totalItemCount: maxScrollableRowCount);
        }

        return new ItemsProviderResult<(int, TGridItem)>();
    }

    // Normalizes all the different ways of configuring a data source so they have common GridItemsProvider-shaped API
    private async ValueTask<GridItemsProviderResult<TGridItem>> ResolveItemsRequestAsync(GridItemsProviderRequest<TGridItem> request)
    {
        if (ItemsProvider is not null)
        {
            return await ItemsProvider(request);
        }

        if (Items is not null)
        {
            var totalItemCount = Items.Count();
            var sortDirection = _sortByAscending ? SortDirection.Ascending : SortDirection.Descending;

            var result = Items.SortBy(_sortByColumn, sortDirection)
                .Skip(request.StartIndex)
                .Take(request.Count ?? totalItemCount);

            return GridItemsProviderResult.From(result.ToArray(), totalItemCount);
        }

        return GridItemsProviderResult.From(Array.Empty<TGridItem>(), 0);
    }

    private async Task OnItemsReceivedAsync()
    {
        if (_hasReceivedFirstResult)
        {
            return;
        }

        // Always hide overlay
        if (_loadingOverlayRef is not null)
        {
            await _loadingOverlayRef.Hide();
        }

        // When using Items, defer "first result received" until rows exist.
        // Items may be initialized as an empty list and populated later.
        if (ItemsProvider is null && Items?.Any() == false)
        {
            return;
        }

        _hasReceivedFirstResult = true;

        var hasPendingAfterRenderWork = _stateHelper.HasPendingScrollRestore;

        // After the first items load, we may need one extra re-render so OnAfterRenderAsync
        // can run one-shot work (e.g. scroll restore) that requires rendered rows.
        // Total count updates are handled by the title component and do not require a re-render.
        if (hasPendingAfterRenderWork)
        {
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task SortColumn(ColumnBase<TGridItem> column)
    {
        if (_sortByColumn.Equals(column.PropertyName, StringComparison.Ordinal))
        {
            _sortByAscending = !_sortByAscending;
        }
        else if (column.PropertyType == typeof(DateTimeOffset) || column.PropertyType == typeof(DateTimeOffset?))
        {
            _sortByAscending = false;
        }
        else
        {
            _sortByAscending = true;
        }

        _sortByColumn = column.PropertyName;

        await _stateHelper.SaveSortState(_sortByColumn, _sortByAscending);

        await _virtualizeComponent!.RefreshDataAsync();
    }


    private async Task FilterChanged(IEnumerable<IColumnFilter> filters)
    {
        _filters = filters.ToList();

        _selectionManager.ClearSelection();

        if (IsInitialized)
        {
            await _stateHelper.SaveFilterState(_filters);
        }

        if (_virtualizeComponent is not null)
        {
            await _virtualizeComponent.RefreshDataAsync();
            // Only needs StateHasChanged when triggered from another component. See FilterUpdated.
        }
    }

    /// <summary>
    /// Update the filter if it's modified progamatically after
    /// the initialization phase (after the first data load).
    /// </summary>
    /// <param name="filter"></param>
    /// <returns></returns>
    internal async Task FilterUpdated(IColumnFilter filter)
    {
        if (_filters != null && IsInitialized)
        {
            var index = _filters.FindIndex(x => x.PropertyName == filter.PropertyName);
            if (index >= 0)
            {
                _filters[index] = filter;
                await InvokeAsync(async () =>
                {
                    await FilterChanged(_filters);
                    // Force re-render when the column component triggers filter change.
                    StateHasChanged();
                });
            }
        }
    }

    public override async ValueTask DisposeAsync()
    {
        // First dispose objects that could be dependent on the javascript object.
        Dispose();

        // Then dispose the javascript object.
        await _script.SafeDisposeAsync();

        GC.SuppressFinalize(this);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Dispose timer in state helper
        _stateHelper?.Dispose();

        if (_selectionManager is not null)
        {
            _selectionManager.OnSelectAllCompleted -= SelectAllCompleted;
            _selectionManager.OnSelectionChanged -= NotifySelectionChanged;
            _selectionManager.Dispose();
        }

        // Drop filter references only. Do not call Clear() on the filters:
        // - Column filters are owned by column components disposed in the same tree.
        // - Custom quick filters are owned by the parent and may be reused after the grid is disposed.
        // Calling Clear() would change the parent's state and could raise events during disposal.
        _filters = null;
        _filterSources = null;

        _columns.Clear();

        // Drop references to large/long-lived graphs
        ItemsProvider = null;
        Items = null;
        ItemStyleOptions = null;
        HighlightOptions = null;
        HeaderContent = null;
        Columns = null;
        Actions = null;
        CustomQuickFilters = null;

        // Release component references
        _virtualizeComponent = null;
        _internalGridContext = null!;
        _highlightedItem = default;

        GC.SuppressFinalize(this);
    }
}
