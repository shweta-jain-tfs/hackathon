using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;

namespace Pharos.Common.Web.Services;

/// <summary>
/// Provides component state persistence across navigation using browser sessionStorage.
/// 
/// Usage:
/// - Pass a unique component ID per page - the current page URL is automatically managed
/// - Clear operations are optional - automatic state clearing occurs based on navigation patterns
/// - State is preserved on back navigation and breadcrumb navigation
/// - State is cleared when navigating to different sections via main menu
/// </summary>
public interface IComponentStateService
{
    /// <summary>
    /// Retrieves stored state for a component on the current page.
    /// </summary>
    /// <typeparam name="T">Type of state object to retrieve</typeparam>
    /// <param name="componentId">Unique identifier for the component (must be unique per page)</param>
    /// <returns>The stored state object, or null if no state exists</returns>
    Task<T?> GetStateAsync<T>(string componentId) where T : class;

    /// <summary>
    /// Stores state for a component on the current page.
    /// </summary>
    /// <typeparam name="T">Type of state object to store</typeparam>
    /// <param name="componentId">Unique identifier for the component (must be unique per page)</param>
    /// <param name="state">State object to persist</param>
    Task SetStateAsync<T>(string componentId, T state) where T : class;

    /// <summary>
    /// Removes stored state for a component on the current page.
    /// Optional - automatic clearing occurs based on navigation patterns.
    /// </summary>
    /// <param name="componentId">Unique identifier for the component</param>
    Task RemoveStateAsync(string componentId);

    /// <summary>
    /// Manually clears all component states for the current page.
    /// Optional - automatic clearing occurs based on navigation patterns.
    /// </summary>
    /// <param name="clearSubtree">If true, also clears states for sub-pages</param>
    Task ClearStatesForCurrentPageAsync(bool clearSubtree = true);

    /// <summary>
    /// Manually clears all component states for a specific page.
    /// Optional - automatic clearing occurs based on navigation patterns.
    /// </summary>
    /// <param name="pageUrl">URL of the page to clear states for</param>
    /// <param name="clearSubtree">If true, also clears states for sub-pages</param>
    Task ClearStatesForPageAsync(string pageUrl, bool clearSubtree = true);
}

internal class ComponentStateService : IComponentStateService, IDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private readonly NavigationManager _navigationManager;
    private const string _storagePrefix = "component_state";
    private string _currentPageUrl = "__component_state_initial_url__";
    private int? _lastHistoryIndex; // Track history state index for navigation detection
    private readonly SemaphoreSlim _navigationLock = new(1, 1); // Synchronize navigation handling
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public ComponentStateService(IJSRuntime jsRuntime, NavigationManager navigationManager)
    {
        _jsRuntime = jsRuntime;
        _navigationManager = navigationManager;

        // Subscribe to location changes to ensure _currentPageUrl is always updated
        _navigationManager.LocationChanged += OnLocationChanged;
    }

    private async void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        await _navigationLock.WaitAsync();
        try
        {
            var toUrl = GetCleanUrl(e.Location);
            var fromUrl = _currentPageUrl;

            // Handle navigation logic first (while fromUrl is still the previous URL for back detection)
            await HandlePageNavigation(fromUrl, toUrl);

            // Update current URL after navigation handling
            _currentPageUrl = toUrl;
        }
        finally
        {
            _navigationLock.Release();
        }
    }

    /// <summary>
    /// Manages component state clearing based on navigation patterns:
    /// - Preserves state on breadcrumb navigation (sub-page → parent)
    /// - Preserves state on back navigation (cross-section browsing)
    /// - Preserves state when deep linking to sub-pages (navigation to non-level 1 pages)
    /// - Clears state on cold navigation (main menu → level 1 page)
    /// 
    /// Clears state only when: navigating TO level 1 page AND from different section AND not back navigation.
    /// </summary>
    /// <param name="fromUrl">Source URL</param>
    /// <param name="toUrl">Destination URL</param>
    private async Task HandlePageNavigation(string fromUrl, string toUrl)
    {
        var fromLevel1 = GetLevel1PageUrl(fromUrl);
        var toLevel1 = GetLevel1PageUrl(toUrl);
        var isNavigatingToLevel1Page = IsLevel1PageOrRoot(toUrl);

        // Don't clear state when navigating back to a page (back button) or via NavigationHistoryManager
        var isBackNavigation = await IsBackNavigationAsync();

        if (isNavigatingToLevel1Page &&
            fromLevel1 != toLevel1 &&
            !isBackNavigation)
        {
            var isRoot = toLevel1.Length == 0;
            var clearSubtree = !isRoot;

            // Clear all states for the destination level 1 page tree (including subpages except when navigating to the root page)
            await ClearStatesForPageAsync(toUrl, clearSubtree);
        }
    }

    private async Task<bool> IsBackNavigationAsync()
    {
        // First check if NavigationManager indicates back/forward navigation
        // This handles programmatic navigation via NavigationHistoryManager
        var hasHistoryEntryState = _navigationManager.HistoryEntryState != null;

        try
        {
            // Get current state index from history.state._index (history.length maxes out at 50, so it can't be used)
            var currentStateIndex = await _jsRuntime.InvokeAsync<int?>("eval", "window.history.state?._index");

            // If we have no state index, treat as forward navigation (safe default)
            if (currentStateIndex == null)
            {
                _lastHistoryIndex = null;
                return hasHistoryEntryState;
            }

            var isBrowserBackNavigation = _lastHistoryIndex != null && currentStateIndex < _lastHistoryIndex;

            // Update stored history index
            _lastHistoryIndex = currentStateIndex;

            return isBrowserBackNavigation || hasHistoryEntryState;
        }
        catch
        {
            return hasHistoryEntryState;
        }
    }

    private string GetCleanUrl(string url)
    {
        string relativePath;

        if (url.Contains("://") || url.StartsWith(_navigationManager.BaseUri, StringComparison.OrdinalIgnoreCase))
        {
            relativePath = _navigationManager.ToBaseRelativePath(url);
        }
        else
        {
            relativePath = url;
        }

        // Remove fragments
        relativePath = relativePath.Split('#')[0];

        // Split into base path and query string
        var parts = relativePath.Split('?', 2);
        var basePath = parts[0].Trim('/');
        var queryString = parts.Length > 1 ? $"?{parts[1]}" : "";

        // Add trailing slash to base path to prevent partial URL matches during clearing operations
        // Example: "QUALITY/" won't accidentally match "QUALITYCONTROL/"
        return $"{basePath}/{queryString}".ToUpperInvariant();
    }

    private static string GetLevel1PageUrl(string cleanUrl)
    {
        // Extract the base path before query parameters for level 1 detection
        var basePath = cleanUrl.Split('?')[0].TrimEnd('/');

        var segments = basePath.Split('/');

        // Special case: URLs starting with "more/" are treated as level 1
        // Examples: "more/parameters", "more/settings", "more/help" are all level 1
        if (segments.Length >= 2 &&
            segments[0].Equals("MORE", StringComparison.OrdinalIgnoreCase))
        {
            return $"{segments[0]}/{segments[1]}";
        }

        return segments[0];
    }

    private static bool IsLevel1PageOrRoot(string cleanUrl)
    {
        // Extract base path without query parameters
        var basePath = cleanUrl.Split('?')[0].TrimEnd('/');

        // Count slash-separated segments
        var segments = basePath.Split('/');

        // Special case: URLs starting with "more/" are treated as level 1 if they have exactly 2 segments
        // Examples: "more/parameters" is level 1, but "more/parameters/methods" is not
        if (segments.Length >= 2 &&
            segments[0].Equals("MORE", StringComparison.OrdinalIgnoreCase))
        {
            return segments.Length == 2;
        }

        return segments.Length <= 1;
    }

    /// <summary>
    /// Builds a storage key in the format: component_state|URL/TO/UPPER/|componentid
    /// </summary>
    /// <param name="cleanUrl">Cleaned up URL with forward slashes</param>
    /// <param name="componentId">Component identifier</param>
    /// <returns>Storage key with pipe separators</returns>
    private static string BuildStorageKey(string cleanUrl, string componentId)
    {
        return $"{_storagePrefix}|{cleanUrl}|{componentId}";
    }

    private string BuildStorageKey(string componentId)
    {
        return BuildStorageKey(_currentPageUrl, componentId);
    }

    /// <summary>
    /// Ensures navigation state is properly synchronized before state operations.
    /// 
    /// This method addresses a race condition where components calling GetStateAsync (typically in OnAfterRenderAsync)
    /// might execute before or concurrently with OnLocationChanged. Without synchronization, state could be loaded
    /// before it's properly cleared for navigation changes, leading to stale data being served.
    /// 
    /// The navigation lock ensures that:
    /// 1. Any ongoing navigation clearing operations complete before state access
    /// 2. Navigation detection (especially back navigation) works correctly by processing URL changes in proper sequence
    /// </summary>
    private async Task EnsureCurrentPageUrlUpdatedAndClearedAsync()
    {
        await _navigationLock.WaitAsync();
        try
        {
            // Check if current URL has changed since last update
            var currentUrl = GetCleanUrl(_navigationManager.Uri);
            if (currentUrl != _currentPageUrl)
            {
                var fromUrl = _currentPageUrl;

                // Handle navigation logic first (while fromUrl is still the previous URL for back detection)
                await HandlePageNavigation(fromUrl, currentUrl);

                // Update current URL after navigation handling
                _currentPageUrl = currentUrl;
            }
        }
        finally
        {
            _navigationLock.Release();
        }
    }

    public async Task<T?> GetStateAsync<T>(string componentId) where T : class
    {
        // Handle navigation before getting state to make sure we're not serving
        // values that were supposed to be cleared but OnLocationChanged hasn't run yet
        await EnsureCurrentPageUrlUpdatedAndClearedAsync();
        var key = BuildStorageKey(componentId);

        try
        {
            var json = await _jsRuntime.InvokeAsync<string?>("sessionStorage.getItem", key);

            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }
        catch (Exception)
        {
#if DEBUG
            Console.WriteLine($"ComponentStateService.GetStateAsync failed for key '{key}'");
#endif
            return null;
        }
    }

    public async Task SetStateAsync<T>(string componentId, T state) where T : class
    {
        await EnsureCurrentPageUrlUpdatedAndClearedAsync();
        var key = BuildStorageKey(componentId);

        try
        {
            var json = JsonSerializer.Serialize(state, _jsonOptions);
            await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", key, json);
        }
        catch (Exception)
        {
#if DEBUG
            Console.WriteLine($"ComponentStateService.SetStateAsync failed for key '{key}'");
#endif
        }
    }

    public async Task RemoveStateAsync(string componentId)
    {
        await EnsureCurrentPageUrlUpdatedAndClearedAsync();

        try
        {
            var key = BuildStorageKey(componentId);
            await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", key);
        }
        catch (Exception)
        {
#if DEBUG
            Console.WriteLine($"ComponentStateService.RemoveStateAsync failed for component '{componentId}'");
#endif
        }
    }

    public async Task ClearStatesForCurrentPageAsync(bool clearSubtree = true)
    {
        await ClearStatesForPageAsync(_currentPageUrl, clearSubtree);
    }

    public async Task ClearStatesForPageAsync(string pageUrl, bool clearSubtree = true)
    {
        try
        {
            var processedUrl = GetCleanUrl(pageUrl);

            if (clearSubtree)
            {
                // Remove query string when clearing subtree
                processedUrl = processedUrl.Split('?')[0];
            }

            var prefix = clearSubtree
                ? $"{_storagePrefix}|{processedUrl}"        // Matches page and all subpages (without query string)
                : $"{_storagePrefix}|{processedUrl}|";      // Matches only exact page (with query string)

#if DEBUG
            Console.WriteLine($"ComponentStateService.ClearStatesForPageAsync '{prefix}'");
#endif

            // Get all sessionStorage keys and remove matching ones
            var allKeys = await _jsRuntime.InvokeAsync<string[]>("eval", "Object.keys(sessionStorage)");

            var keysToRemove = allKeys
                .Where(key => key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

            foreach (var key in keysToRemove)
            {
                await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", key);
            }
        }
        catch (Exception)
        {
#if DEBUG
            Console.WriteLine($"ComponentStateService.ClearStatesForPageAsync failed for URL '{pageUrl}'");
#endif
        }
    }

    public void Dispose()
    {
        _navigationManager.LocationChanged -= OnLocationChanged;
        _navigationLock.Dispose();
        GC.SuppressFinalize(this);
    }
}