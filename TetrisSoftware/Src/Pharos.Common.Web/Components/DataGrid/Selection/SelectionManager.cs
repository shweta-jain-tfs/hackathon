using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Pharos.Common.Web.Components.Loader;
using Pharos.Common.Web.Models.DataGrid;

namespace Pharos.Common.Web.Components.DataGrid.Selection;

internal class SelectionManager<TGridItem> : IDisposable
{
    private const int _selectAllBatchSize = 5000;
    private const int _minimumRowCountForProgressBar = 500;

    private readonly LoaderService _loaderService;
    private readonly Guid _loaderGuid = Guid.NewGuid();
    private readonly Func<TGridItem, object?>? _keySelector;
    private readonly Func<TGridItem, bool>? _canSelect;

    // Selection is split into two buckets:
    // - _selectedByKey: items with a stable identifier (Id/Guid). This handles virtualization
    //   and reloading scenarios where the same logical item may appear as a new instance.
    // - _selectedWithoutKey: fallback for items without a stable key. These are tracked using
    //   EqualityComparer<T>.Default (IEquatable/Equals if implemented, otherwise reference equality).
    private readonly Dictionary<object, TGridItem> _selectedByKey = new();
    private readonly List<TGridItem> _selectedWithoutKey = new();

    private bool _progressBarActive;
    private CancellationTokenSource? _selectAllCancellationTokenSource;

    public int SelectedCount => _selectedByKey.Count + _selectedWithoutKey.Count;

    public bool IsSelectAllInProgress => _selectAllCancellationTokenSource is not null;

    public event Action? OnSelectionChanged;
    public event Action<SelectAllCompletedResult>? OnSelectAllCompleted;

    public SelectionManager(LoaderService loaderService, Func<TGridItem, bool>? canSelect = null)
    {
        _loaderService = loaderService;
        _canSelect = canSelect;
        _loaderService.OnCancel += LoaderService_OnCancel;

        _keySelector = TryBuildIdKeySelector();
    }

    private void LoaderService_OnCancel()
    {
        HideProgressBar();

        // Cancel the select all operation if it's running
        _selectAllCancellationTokenSource?.Cancel();
    }

    public bool IsPartiallySelected(int totalItemCount)
    {
        return SelectedCount > 0 && SelectedCount < totalItemCount;
    }

    public bool IsAllSelected(int totalItemCount)
    {
        return SelectedCount == totalItemCount && totalItemCount > 0;
    }

    public void SelectItem(TGridItem item, bool isSingleSelectMode)
    {
        // Reject blocked rows before clearing the existing single selection.
        if (_canSelect?.Invoke(item) == false)
        {
            return;
        }

        if (isSingleSelectMode)
        {
            ClearSelectionInternal();
        }

        if (TrySelectItem(item))
        {
            NotifySelectionChanged();
        }
    }

    public void DeselectItem(TGridItem item)
    {
        // Only notify listeners if the row was actually selected and removed.
        if (TryDeselectItem(item))
        {
            NotifySelectionChanged();
        }
    }

    private bool TryDeselectItem(TGridItem item)
    {
        if (_keySelector is not null)
        {
            var key = _keySelector(item);
            if (!IsDefaultKey(key))
            {
                return _selectedByKey.Remove(key);
            }
        }

        var index = _selectedWithoutKey.FindIndex(
            x => EqualityComparer<TGridItem>.Default.Equals(x, item));

        if (index >= 0)
        {
            _selectedWithoutKey.RemoveAt(index);
            return true;
        }

        return false;
    }

    public IReadOnlyList<TGridItem> GetSelectedItems()
    {
        return [.. _selectedByKey.Values, .. _selectedWithoutKey];
    }

    /// <summary>
    /// Deselects rows that are no longer allowed and sends one notification if the selection changed.
    /// </summary>
    public void RemoveUnselectableItems(IEnumerable<TGridItem> items)
    {
        if (_canSelect is null || SelectedCount == 0)
        {
            return;
        }

        var selectionChanged = false;
        foreach (var item in items)
        {
            if (!_canSelect(item))
            {
                selectionChanged |= TryDeselectItem(item);
            }
        }

        if (selectionChanged)
        {
            NotifySelectionChanged();
        }
    }

    public void ClearSelection()
    {
        if (SelectedCount == 0)
        {
            return;
        }

        ClearSelectionInternal();
        NotifySelectionChanged();
    }

    public void SetSelectedItems(IList<TGridItem> items)
    {
        ClearSelectionInternal();

        if (items is { Count: > 0 })
        {
            foreach (var item in items)
            {
                TrySelectItem(item);
            }
        }

        NotifySelectionChanged();
    }

    public bool IsItemSelected(TGridItem item)
    {
        if (_keySelector is not null)
        {
            var key = _keySelector(item);
            if (!IsDefaultKey(key))
            {
                return _selectedByKey.ContainsKey(key);
            }
        }

        // Using EqualityComparer to compare stale items with newly loaded items
        return _selectedWithoutKey.Any(selectedItem => EqualityComparer<TGridItem>.Default.Equals(selectedItem, item));
    }

    public async Task SelectAllFromServerAsync(
        GridItemsProvider<TGridItem> itemsProvider,
        int totalRowCount,
        string sortByColumn,
        bool sortByAscending,
        List<IColumnFilter>? filters,
        string? progressBarText)
    {
        var result = new SelectAllCompletedResult();

        using var cts = new CancellationTokenSource();

        try
        {
            if (_keySelector is null)
            {
                throw new InvalidOperationException(
                    "Select All from server requires TGridItem to have an Id property or a property marked with [SelectionKey].");
            }

            if (totalRowCount == 0)
            {
                return;
            }

            ClearSelectionInternal();

            _selectedByKey.EnsureCapacity(totalRowCount);

            _selectAllCancellationTokenSource?.Cancel();
            _selectAllCancellationTokenSource = cts;
            var cancellationToken = cts.Token;

            if (totalRowCount > _minimumRowCountForProgressBar)
            {
                ShowProgressBar(progressBarText);
            }

            var colFilters = filters?.ToList() ?? [];

            var startIndex = 0;
            while (startIndex < totalRowCount)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (_progressBarActive)
                {
                    _loaderService.UpdateProgress(_loaderGuid, startIndex, totalRowCount);
                }

                var output = await itemsProvider(new GridItemsProviderRequest<TGridItem>(
                    startIndex: startIndex,
                    count: _selectAllBatchSize,
                    sortByColumn: sortByColumn,
                    sortByAscending: sortByAscending,
                    columnFilters: colFilters,
                    requiresRowLimitBypass: true,
                    cancellationToken: cancellationToken));

                foreach (var item in output.Items)
                {
                    TrySelectItem(item);
                }

                startIndex += _selectAllBatchSize;
            }
        }
        catch (OperationCanceledException)
        {
            ClearSelectionInternal();
            result = new SelectAllCompletedResult(isCancelled: true);
        }
        catch (Exception ex)
        {
            result = new SelectAllCompletedResult(error: ex);
        }
        finally
        {
            if (ReferenceEquals(_selectAllCancellationTokenSource, cts))
            {
                _selectAllCancellationTokenSource = null;
            }

            HideProgressBar();
            NotifySelectionChanged();

            // Report the result through the OnSelectAllCompleted event
            OnSelectAllCompleted?.Invoke(result);
        }
    }

    public void SelectAllFromItems(IQueryable<TGridItem> items)
    {
        ClearSelectionInternal();

        foreach (var item in items)
        {
            TrySelectItem(item);
        }

        NotifySelectionChanged();
    }

    private bool TrySelectItem(TGridItem item)
    {
        // Apply the same selection rules to Select All and programmatic selection.
        if (_canSelect?.Invoke(item) == false)
        {
            return false;
        }

        if (_keySelector is not null)
        {
            var key = _keySelector(item);
            if (!IsDefaultKey(key))
            {
                _selectedByKey[key] = item;
                return true;
            }
        }

        if (!_selectedWithoutKey.Any(selectedItem => EqualityComparer<TGridItem>.Default.Equals(selectedItem, item)))
        {
            _selectedWithoutKey.Add(item);
            return true;
        }

        return false;
    }

    private void ShowProgressBar(string? progressBarText)
    {
        if (!_progressBarActive)
        {
            _loaderService.ShowProgress(_loaderGuid, progressBarText);
            _progressBarActive = true;
        }
    }

    private void HideProgressBar()
    {
        if (_progressBarActive)
        {
            _loaderService.HideProgress(_loaderGuid);
            _progressBarActive = false;
        }
    }

    private void NotifySelectionChanged()
    {
        OnSelectionChanged?.Invoke();
    }

    private static bool IsDefaultKey([NotNullWhen(false)] object? key)
    {
        return key is null
            || (key is int i && i == 0)
            || (key is Guid g && g == Guid.Empty);
    }

    private static Func<TGridItem, object?>? TryBuildIdKeySelector()
    {
        var t = typeof(TGridItem);

        // Look for [SelectionKey] attribute first
        var keyProp = t.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .FirstOrDefault(p => p.GetCustomAttribute<SelectionKeyAttribute>() != null);

        // Fallback to conventions if attribute not found
        keyProp ??= t.GetProperty("Id", BindingFlags.Instance | BindingFlags.Public)
            ?? t.GetProperty($"{t.Name}Id", BindingFlags.Instance | BindingFlags.Public);

        if (keyProp is null || !keyProp.CanRead || keyProp.GetIndexParameters().Length != 0)
        {
            return null;
        }

        return item => keyProp.GetValue(item);
    }

    private void ClearSelectionInternal()
    {
        _selectedByKey.Clear();
        _selectedWithoutKey.Clear();
    }

    public void Dispose()
    {
        ClearSelectionInternal();
        _loaderService.OnCancel -= LoaderService_OnCancel;
        _selectAllCancellationTokenSource?.Cancel();
        _selectAllCancellationTokenSource?.Dispose();
        _selectAllCancellationTokenSource = null;
    }
}
