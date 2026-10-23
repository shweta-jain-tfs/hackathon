using Microsoft.JSInterop;
using Pharos.Common.Web.Models.DataGrid;
using Pharos.Common.Web.Services;

namespace Pharos.Common.Web.Components.DataGrid.State;

internal class StateHelper : IDisposable
{
    private readonly IComponentStateService _componentStateService;
    private const double _restoreScrollThreshold = 1;
    private DataGridState _currentState = new();
    private bool _stateLoaded;
    private Timer? _scrollCheckTimer;
    private bool _hasAppliedScrollState;
    private double _restoreToScrollY;
    private string? _stateId;
    private bool _disposed;

    public bool DisableState => string.IsNullOrEmpty(_stateId);
    private string StateKey => $"datagrid_{_stateId}";
    public bool HasPendingScrollRestore =>
        _stateLoaded &&
        !_hasAppliedScrollState &&
        _restoreToScrollY > _restoreScrollThreshold;

    public StateHelper(IComponentStateService componentStateService)
    {
        _componentStateService = componentStateService;
    }

    public async Task<DataGridState?> LoadState(string? stateID)
    {
        if (_stateLoaded)
        {
            return DisableState ? null : _currentState;
        }

        _stateId = stateID;
        var loadedState = null as DataGridState;
        if (!DisableState)
        {
            loadedState = await _componentStateService.GetStateAsync<DataGridState>(StateKey);
        }

        if (loadedState is not null)
        {
            _currentState = loadedState;
            _restoreToScrollY = loadedState.ScrollY;
        }

        _stateLoaded = true;
        return loadedState;
    }

    public async Task SaveFilterState(List<IColumnFilter> filters)
    {
        // Update filters
        var filterStates = filters
            .Select(f => f.SaveState())
            .Where(state => !string.IsNullOrEmpty(state?.PropertyName))
            .ToList();

        _currentState.Filters = filterStates;

        await SaveState(_currentState);
    }

    public async Task SaveSortState(string? sortColumn, bool sortAscending)
    {
        _currentState.SortColumn = sortColumn;
        _currentState.SortAscending = sortAscending;

        await SaveState(_currentState);
    }

    public void StartScrollMonitoring(ElementReference? gridContainerRef, IJSObjectReference script)
    {
        // Poll for scroll position instead of listening to events to avoid re-renders
        // Check scroll position every second
        _scrollCheckTimer?.Dispose();
        _scrollCheckTimer = new Timer(_ =>
        {
            _ = CheckScrollPosition(gridContainerRef, script);
        }, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// Restores the saved scroll position after data has been loaded.
    /// This only runs once per component lifecycle when conditions are met.
    /// </summary>
    public async Task<bool> TryRestoreScrollPosition(ElementReference? gridContainerRef, IJSObjectReference? script)
    {
        if (!_stateLoaded || _hasAppliedScrollState || gridContainerRef is null || script is null)
        {
            return false;
        }

        // Apply scroll position from state after the data is loaded
        var hasTargetScrollPosition = _restoreToScrollY > _restoreScrollThreshold;
        if (hasTargetScrollPosition)
        {
            try
            {
                await script.InvokeVoidAsync("scrollToWhenReady", gridContainerRef, _restoreToScrollY);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error restoring scroll position: {ex.Message}");
                return false;
            }
        }

        _hasAppliedScrollState = true;

        return hasTargetScrollPosition;
    }

    /// <summary>
    /// Applies the filters saved in the state to the current data grid column filters.
    /// </summary>
    /// <param name="filters">Filters to update</param>
    /// <param name="state">Component state that may contain filter information.</param>
    public static void ApplyColumnFilters(IReadOnlyList<IColumnFilter>? filters, DataGridState state)
    {
        if (filters is null)
        {
            return;
        }

        var filterStates = state.Filters.ToDictionary(filter => filter.PropertyName);

        foreach (var filter in filters)
        {
            if (filterStates.TryGetValue(filter.PropertyName, out var filterState))
            {
                filter.ApplyState(filterState);
            }
        }
    }

    private async Task CheckScrollPosition(ElementReference? gridContainerRef, IJSObjectReference script)
    {
        if (_disposed || script is null || HasPendingScrollRestore)
        {
            return;
        }

        try
        {
            var scrollInfo = await script.InvokeAsync<ScrollInfo>("getScrollPositionWithVisibility", gridContainerRef);

            // Only save if position changed AND grid is visible
            if (scrollInfo is { IsVisible: true } && Math.Abs(scrollInfo.Position - _currentState.ScrollY) > 1)
            {
                await SaveScrollPosition(scrollInfo.Position);
            }
        }
        catch (ObjectDisposedException) { /* ignore during teardown */ }
        catch (JSDisconnectedException) { /* ignore during teardown */ }
        catch (TaskCanceledException) { /* ignore during teardown */ }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking scroll position: {ex.Message}");
        }
    }

    private async Task SaveScrollPosition(double scrollY)
    {
        _currentState.ScrollY = scrollY;

        await SaveState(_currentState);
    }

    private async Task SaveState(DataGridState state)
    {
        if (!_stateLoaded || DisableState)
        {
            return; // Don't save state until we've loaded the initial state
        }

        await _componentStateService.SetStateAsync(StateKey, state);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _scrollCheckTimer?.Dispose();
        _scrollCheckTimer = null;
    }
}
