namespace Pharos.Common.Web.Extensions;

/// <summary>
/// Extension methods for NavigationManager to support navigation history tracking.
/// </summary>
public static class NavigationManagerExtensions
{
    /// <summary>
    /// Navigates to the specified URI, optionally replacing the current history entry.
    /// When replace is true, sets HistoryEntryState to "redirect" to distinguish from normal navigation.
    /// </summary>
    /// <param name="navigationManager">The NavigationManager instance.</param>
    /// <param name="uri">The destination URI.</param>
    /// <param name="forceLoad">If true, bypasses client-side routing and forces a server-side page load.</param>
    /// <param name="replace">If true, replaces the current history entry; if false, appends a new entry.</param>
    public static void NavigateToWithRedirect(this NavigationManager navigationManager, string uri, bool forceLoad = false, bool replace = false)
    {
        if (replace)
        {
            navigationManager.NavigateTo(
                uri,
                new NavigationOptions()
                {
                    HistoryEntryState = "redirect",
                    ForceLoad = forceLoad,
                    ReplaceHistoryEntry = true,
                }
            );
        }
        else
        {
            navigationManager.NavigateTo(uri, forceLoad);
        }
    }
}
