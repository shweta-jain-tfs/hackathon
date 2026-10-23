using Pharos.Server.Settings.Application.Metadata;

namespace Pharos.Server.Settings.Application.Services;

/// <summary>
/// Provides navigation and search operations on the settings page tree structure.
/// Consolidates tree traversal logic used by validators and copy operations.
/// </summary>
internal sealed class PageTreeNavigator : IPageTreeNavigator
{
    public PageList? FindPageListByKey(Dictionary<string, Page> rootPages, string pageListKey)
    {
        foreach (var page in rootPages.Values)
        {
            var found = SearchForPageListByKey(page, pageListKey);
            if (found != null)
            {
                return found;
            }
        }
        return null;
    }

    public PageList? FindPageListContainingEntry(Dictionary<string, Page> rootPages, string entryKey)
    {
        foreach (var page in rootPages.Values)
        {
            var found = SearchForPageListWithEntry(page, entryKey);
            if (found != null)
            {
                return found;
            }
        }
        return null;
    }

    public List<PageList> FindAllPageLists(Dictionary<string, Page> rootPages)
    {
        var pageLists = new List<PageList>();
        foreach (var page in rootPages.Values)
        {
            FindPageListsRecursive(page.Children, pageLists);
        }
        return pageLists;
    }

    public List<PageList> FindAllPageListsInPage(Page page)
    {
        var pageLists = new List<PageList>();
        FindPageListsRecursive(page.Children, pageLists);
        return pageLists;
    }

    private static PageList? SearchForPageListByKey(Page page, string pageListKey)
    {
        foreach (var child in page.Children)
        {
            switch (child)
            {
                case PageList pl when pl.Key == pageListKey:
                    return pl;
                case Page nestedPage:
                    {
                        var found = SearchForPageListByKey(nestedPage, pageListKey);
                        if (found != null)
                        {
                            return found;
                        }
                        break;
                    }
                case Group group:
                    {
                        foreach (var gChild in group.Children.OfType<Page>())
                        {
                            var found = SearchForPageListByKey(gChild, pageListKey);
                            if (found != null)
                            {
                                return found;
                            }
                        }
                        break;
                    }
            }
        }
        return null;
    }

    private static PageList? SearchForPageListWithEntry(Page page, string entryKey)
    {
        foreach (var child in page.Children)
        {
            switch (child)
            {
                case PageList pl when pl.PageEntries.Any(e => e.Key == entryKey):
                    return pl;
                case Page nested:
                    {
                        var found = SearchForPageListWithEntry(nested, entryKey);
                        if (found != null)
                        {
                            return found;
                        }
                        break;
                    }
                case Group group:
                    {
                        foreach (var gChild in group.Children.OfType<Page>())
                        {
                            var found = SearchForPageListWithEntry(gChild, entryKey);
                            if (found != null)
                            {
                                return found;
                            }
                        }
                        break;
                    }
            }
        }
        return null;
    }

    private static void FindPageListsRecursive(List<SettingsChildItem> children, List<PageList> pageLists)
    {
        foreach (var child in children)
        {
            switch (child)
            {
                case PageList pageList:
                    pageLists.Add(pageList);
                    break;
                case Page nestedPage:
                    FindPageListsRecursive(nestedPage.Children, pageLists);
                    break;
                case Group group:
                    FindPageListsRecursive(group.Children, pageLists);
                    break;
            }
        }
    }
}
