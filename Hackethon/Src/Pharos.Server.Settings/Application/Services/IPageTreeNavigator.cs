using Pharos.Server.Settings.Application.Metadata;

namespace Pharos.Server.Settings.Application.Services;

/// <summary>
/// Provides navigation and search operations on the settings page tree structure.
/// </summary>
internal interface IPageTreeNavigator
{
    /// <summary>
    /// Finds a PageList by its key within the root pages dictionary.
    /// </summary>
    PageList? FindPageListByKey(Dictionary<string, Page> rootPages, string pageListKey);

    /// <summary>
    /// Finds the PageList that contains a page entry with the specified key.
    /// </summary>
    PageList? FindPageListContainingEntry(Dictionary<string, Page> rootPages, string entryKey);

    /// <summary>
    /// Finds all PageLists within the root pages dictionary.
    /// </summary>
    List<PageList> FindAllPageLists(Dictionary<string, Page> rootPages);

    /// <summary>
    /// Finds all PageLists within a single page and its descendants.
    /// </summary>
    List<PageList> FindAllPageListsInPage(Page page);
}
