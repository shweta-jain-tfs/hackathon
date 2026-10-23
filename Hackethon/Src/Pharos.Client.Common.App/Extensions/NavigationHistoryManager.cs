namespace Pharos.Client.Common.App.Extensions;

internal class NavigationHistoryManager
{
    private readonly NavigationManager _navigation;

    private readonly List<string> _navigationHistory = new();
    private readonly List<string> _blacklist;
    private int _currentIndex;

    public event EventHandler<NavigationHistoryEventArgs>? NavigationHistoryEvent;

    public NavigationHistoryManager(NavigationManager navigation)
    {
        // Set index to last inserted route. First click in back, should give index 1.
        _currentIndex = 0;

        _navigation = navigation;
        _navigation.LocationChanged += Navigation_LocationChanged;

        _blacklist = CreateBlackList();
    }

    /// <summary>
    /// Create black list to exclude from navigation history.
    /// </summary>
    /// <returns></returns>
    private static List<string> CreateBlackList()
    {
        var blackList = new List<string>
        {
            // The login/logout locations should not be added to history.
            // Example of the login uri: http://localhost:5128/authentication/login-callback
            "authentication"
        };

        return blackList;
    }

    /// <summary>
    /// Insert location/uri in navigation history and exclude values from black list.
    /// </summary>
    /// <param name="index"></param>
    /// <param name="location"></param>
    private void InsertInNavigationHistory(int index, string location)
    {
        var foundInBlackList = _blacklist.Any(excludeThis => location
            .Contains(excludeThis, StringComparison.OrdinalIgnoreCase));

        // Dont save history if its in the black list.
        if (!foundInBlackList)
        {
            _navigationHistory.Insert(index, location);
        }
    }

    /// <summary>
    /// Handle navigation history when location has changed.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Navigation_LocationChanged(object? sender, LocationChangedEventArgs e)
    {
        // Handle direct navigation or redirects (ignore back/forward navigation).
        if (_navigation.HistoryEntryState is null or "redirect")
        {
            // Clear forward history if navigating after using back button.
            if (_currentIndex > 0)
            {
                _navigationHistory.RemoveRange(0, _currentIndex);
            }

            // Replace last entry if a redirect was made with replace=true (no history entry).
            if (_navigation.HistoryEntryState == "redirect" && _navigationHistory.Count > 0)
            {
                _navigationHistory.RemoveAt(0);
            }

            if (e.Location != _navigationHistory.FirstOrDefault())
            {
                // Insert the latest route in index 0.
                InsertInNavigationHistory(0, e.Location);
            }

            _currentIndex = 0;
        }

        NavigationHistoryEvent?.Invoke(this, new NavigationHistoryEventArgs());
    }

    /// <summary>
    /// Clear navigation history.
    /// </summary>
    internal void Clear()
    {
        _navigationHistory.Clear();
    }

    /// <summary>
    /// A way to insert current route after refresh.
    /// </summary>
    internal void InsertCurrentRoute()
    {
        InsertInNavigationHistory(0, _navigation.Uri);
    }

    /// <summary>
    /// Check if its possible to step back in list.
    /// </summary>
    /// <returns></returns>
    internal bool IsEnabledBack()
    {
        var isEnabled = _currentIndex < _navigationHistory.Count - 1;
        return isEnabled;
    }

    /// <summary>
    /// Check if its possible to step forward in list (towards index 0).
    /// </summary>
    /// <returns></returns>
    internal bool IsEnabledForward()
    {
        var isEnabled = _currentIndex > 0;
        return isEnabled;
    }

    /// <summary>
    /// Navigate one step towards first added route.
    /// </summary>
    internal void NavigateBack()
    {
        if (!IsEnabledBack())
        {
            return;
        }

        // Position cant be greater than items in navigation history list.
        if (_currentIndex < _navigationHistory.Count - 1)
        {
            _currentIndex++;
        }

        // Navigate in direction of first added route.
        var navigateTo = _navigationHistory[_currentIndex];
        _navigation.NavigateTo(
            navigateTo,
            new NavigationOptions()
            {
                HistoryEntryState = "back",
                ForceLoad = false,
                ReplaceHistoryEntry = false,
            }
        );
    }

    /// <summary>
    /// Navigate one step towards last added route. (Position 0).
    /// </summary>
    internal void NavigateForward()
    {
        if (!IsEnabledForward())
        {
            return;
        }

        // Position cant be less than zero.
        if (_currentIndex > 0)
        {
            _currentIndex--;
        }

        // Navigate in direction of last added route, towards zero.
        var navigateTo = _navigationHistory[_currentIndex];
        _navigation.NavigateTo(
            navigateTo,
            new NavigationOptions()
            {
                HistoryEntryState = "forward",
                ForceLoad = false,
                ReplaceHistoryEntry = false,
            });
    }

    internal void ForceReload()
    {
        //Navigate to start since history is reset
        _navigation.NavigateTo("/", true);
    }
}