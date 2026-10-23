using Pharos.Server.Application;
using Pharos.Server.Application.Queries;
using Pharos.Server.Settings.Application.Interfaces;
using Pharos.Server.Settings.Application.Metadata;
using Pharos.Server.Settings.Infrastructure.ConfigurationProvider;

namespace Pharos.Server.Settings.Application.Services;

internal class SettingsSearch : ISearchProvider
{
    private readonly ISettingsStringLocalizer _localizer;
    private readonly ISettingsPermissionHelper _permissionHelper;
    private readonly ITranslationSearchProvider _translationSearchProvider;
    private readonly ISettingsPagesLoader _settingsPagesLoader;

    public SettingsSearch(ISettingsStringLocalizer localizer, ISettingsPermissionHelper permissionHelper, ITranslationSearchProvider translationSearchProvider, ISettingsPagesLoader settingsPagesLoader)
    {
        _localizer = localizer;
        _permissionHelper = permissionHelper;
        _translationSearchProvider = translationSearchProvider;
        _settingsPagesLoader = settingsPagesLoader;
    }

    public async Task<List<SearchResultDto>> Search(string searchString, CancellationToken cancellationToken)
    {
        // Return empty results if search string is empty or whitespace-only
        if (string.IsNullOrWhiteSpace(searchString))
        {
            return [];
        }

        // Get matching translations from the provider
        var matchingTranslations = _translationSearchProvider.GetMatchingTranslations(searchString);

        var displayNameResourceKeys = SettingsDisplayNameResourceKeyProvider.GetDisplayNameResourceKeys();
        var matchingSettingsTranslations = matchingTranslations
            .Where(match => displayNameResourceKeys.Contains(match.Key))
            .Select(match => match.Value)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (matchingSettingsTranslations.Count == 0)
        {
            return [];
        }

        var pages = SettingsSearchPagesProvider.GetTopLevelPages(_localizer);

        // First, build all matches search trees without permission checks
        var searchTrees = new List<SearchNode>();
        foreach (var page in pages.Values)
        {
            var populatedPages = await _settingsPagesLoader.LoadAsync(page.Key, cancellationToken);
            if (!populatedPages.TryGetValue(page.Key, out var populatedPage))
            {
                continue;
            }

            var searchTree = BuildSearchTree(populatedPage, populatedPage, matchingSettingsTranslations, populatedPage.Url);
            if (searchTree != null)
            {
                searchTrees.Add(searchTree);
            }
        }

        var filteredMatches = new List<SearchResultDto>();

        // Then, traverse the search trees with permission checks, and collect results
        foreach (var searchTree in searchTrees)
        {
            var searchResultType = MapSettingsSectionToSearchResultType(searchTree.Item.Key);
            await TraverseSearchTree(searchTree, filteredMatches, searchResultType);
        }

        return filteredMatches;
    }

    /// <summary>
    /// Set the search result type by top level section as a service to the user to make filtering
    /// easier in the search results.
    /// </summary>
    private static SearchResultType MapSettingsSectionToSearchResultType(string key)
    {
        return key switch
        {
            "Database" => SearchResultType.SettingDatabase,
            "Instruments" => SearchResultType.SettingInstrument,
            "LabCommunity" => SearchResultType.SettingLabCommunity,
            "Lis" => SearchResultType.SettingLis,
            "Reagents" => SearchResultType.SettingReagents,
            "Requests" => SearchResultType.SettingRequests,
            "Result" => SearchResultType.SettingResult,
            "Samples" => SearchResultType.SettingSamples,
            "Security" => SearchResultType.SettingSecurity,
            "TaskCard" => SearchResultType.SettingTaskCard,
            _ => SearchResultType.Setting
        };
    }

    private async Task TraverseSearchTree(
        SearchNode node,
        List<SearchResultDto> results,
        SearchResultType searchResultType,
        bool parentIsPageList = false,
        string? destinationHint = null)
    {
        if (await _permissionHelper.HasPermissionAsync(node.Item))
        {
            if (node.IsMatch)
            {
                results.Add(CreateSearchResult(node.Item.Label, node.Url, searchResultType, destinationHint));
            }

            if (parentIsPageList && node.Item is Page)
            {
                destinationHint = node.Item.Label;
            }

            parentIsPageList = node.Item is PageList;

            foreach (var child in node.Children)
            {
                await TraverseSearchTree(child, results, searchResultType, parentIsPageList, destinationHint);
            }
        }
    }

    private static SearchNode? BuildSearchTree(
    SettingItemBase item,
    Page page,
    List<string> matchingKeys,
    string effectiveUrl)
    {
        // Skip hidden
        if (PageHelper.IsHidden(page, item, checkDependencyType: item is not Page))
        {
            return null;
        }

        if (item is Option { IsHidden: true })
        {
            return null;
        }

        var isMatch = matchingKeys.Contains(item.Label);

        var itemEffectiveUrl = string.IsNullOrEmpty(item.Url) ? effectiveUrl : item.Url;

        var node = new SearchNode(item, isMatch, itemEffectiveUrl);

        if (item is ISettingsParentItem parent)
        {
            foreach (var child in parent.Children)
            {
                var childNode = BuildSearchTree(child, page, matchingKeys, itemEffectiveUrl);
                if (childNode != null)
                {
                    node.Children.Add(childNode);
                }
            }
        }
        else if (item is PageList pageList)
        {
            foreach (var entry in pageList.PageEntries.Cast<Page>())
            {
                var childNode = BuildSearchTree(entry, entry, matchingKeys, entry.Url);
                if (childNode != null)
                {
                    node.Children.Add(childNode);
                }
            }
        }

        // only keep node if it or its children match
        if (!node.IsMatch && node.Children.Count == 0)
        {
            return null;
        }

        return node;
    }

    private static SearchResultDto CreateSearchResult(string match, string url, SearchResultType searchResultType, string? destinationHint)
    {
        return new SearchResultDto
        {
            Match = match,
            Module = SearchResultModules.Settings,
            ResultType = searchResultType,
            DestinationHint = destinationHint,
            StringId = url
        };
    }

    public class SearchNode
    {
        public SettingItemBase Item { get; }
        public List<SearchNode> Children { get; } = new();

        public bool IsMatch { get; }
        public string Url { get; }

        public SearchNode(SettingItemBase item, bool isMatch, string url)
        {
            Item = item;
            IsMatch = isMatch;
            Url = url;
        }
    }
}
