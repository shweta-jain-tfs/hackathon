using Pharos.Client.App.Search.Models;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.Models.HelpContent;
using Pharos.Common.Web.Models.DataGrid;
//using Pharos.Common.Web.Utilities;

namespace Pharos.Client.App.Search.ViewModels;

public class SearchResultListPageViewModel
{
    private readonly ISearchResultDataSource _dataSource;
    private readonly IHelpContentService _helpContentService;

    public SearchResultListPageViewModel(
        ISearchResultDataSource dataSource,
        IHelpContentService helpContentService)
    {
        _dataSource = dataSource;
        _helpContentService = helpContentService;
    }

    public string SearchString { get; set; } = string.Empty;

    //private HelpContentManual HelpContentManual { get; set; } = null!;

    private List<SearchResultItem> _cachedResults = new();
    private string? _lastSearchString;

    public async ValueTask<GridItemsProviderResult<SearchResultItem>> GetSearchResult(GridItemsProviderRequest<SearchResultItem> request)
    {
        // Only fetch if search string changed
        if (_lastSearchString != SearchString)
        {
            _cachedResults = await GetAllSearchResults();
            _lastSearchString = SearchString;
        }

        // Apply pagination
        var pagedResults = _cachedResults
            .Skip(request.StartIndex)
            .Take(request.Count ?? _cachedResults.Count)
            .ToList();

        return new GridItemsProviderResult<SearchResultItem>
        {
            Items = pagedResults,
            TotalItemCount = _cachedResults.Count
        };
    }

    private async Task<List<SearchResultItem>> GetAllSearchResults()
    {
        // Return empty results if search string is empty or whitespace-only
        if (string.IsNullOrWhiteSpace(SearchString))
        {
            return new List<SearchResultItem>();
        }

        // Start both tasks in parallel
        var helpContentTask = SearchHelpContent();
        var resultTask = _dataSource.GetSearchResultAsync(SearchString);

        await Task.WhenAll(helpContentTask, resultTask);

        // Combine results
        var combinedResults = resultTask.Result.Concat(helpContentTask.Result).ToList();
        var gridItemResult = new GridItemsProviderResult<SearchResultItem>
        {
            Items = combinedResults,
            TotalItemCount = combinedResults.Count
        };

        return gridItemResult.Items.ToList();
    }

    private async Task<List<SearchResultItem>> SearchHelpContent()
    {
        var results = new List<SearchResultItem>();

        // Return empty results if search string is empty or whitespace-only
        if (string.IsNullOrWhiteSpace(SearchString))
        {
            return results;
        }

        // Load the help content for the specified language
        var searchIndex = await _helpContentService.GetManualSearchIndex();

        // Search through the indexes of the help content (Regular search)
        results.AddRange(SearchIndexes(searchIndex));

        // Search through the chapters of the help content (Advanced Search)
        //SearchChaptersAdvanced(HelpContentManual.Chapters, results);
        return results;
    }

    // This method searches through the indexes of the help content (Regular Search)
    private List<SearchResultItem> SearchIndexes(Dictionary<string, List<ChapterSearchIndex>> searchIndex)
    {
        var searchTerms = SearchString
            .Split([' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(term => term.Length >= 2) // ignore very short terms
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (searchTerms.Length == 0)
        {
            return [];
        }

        // Search through the indexes of the help content
        var termChapterMatches = searchTerms
            .SelectMany(term => searchIndex
                .Where(kvp => kvp.Value != null && kvp.Key.StartsWith(term, StringComparison.OrdinalIgnoreCase))
                .SelectMany(kvp => kvp.Value!.Select(chapter => new { Term = term, Chapter = chapter }))
                .DistinctBy(x => x.Chapter.ChapterId)) // no duplicate chapters per search term
            .ToList();

        var chaptersMatchingAllTerms = termChapterMatches
            .GroupBy(x => x.Chapter.ChapterId, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() == searchTerms.Length)
            .Select(g => g.First().Chapter)
            .ToList();

        // Add the matching indexes to the results
        return chaptersMatchingAllTerms.Select(idx => new SearchResultItem
        {
            StringId = idx.ChapterId,
            Match = idx.Title,
            ResultType = ClientSearchResultType.HelpChapter,
            Module = ClientSearchResultModule.Help
        }).ToList();
    }

    //This is commented out since advanced search is not added yet. Also the ManualChapter Ids are string and needs a new solution.
    // This method searches through the chapters of the help content (Advanced Search)
    //private void SearchChaptersAdvanced(IEnumerable<Chapter> chapters, List<SearchResult> result)
    //{
    //    foreach (var chapter in chapters)
    //    {
    //        // We want the match to be only part of the title or content, not the whole chapter
    //        string? match = null;
    //        // Check if the chapter title or content contains the search string
    //        if (!string.IsNullOrEmpty(chapter.Title) && chapter.Title.Contains(SearchString, StringComparison.OrdinalIgnoreCase))
    //        {
    //            match = chapter.Title;
    //        }
    //        // Check if the chapter content contains the search string
    //        else if (!string.IsNullOrEmpty(chapter.Content) && chapter.Content.Contains(SearchString, StringComparison.OrdinalIgnoreCase))
    //        {
    //            // Find the first occurrence and extract a snippet around the match
    //            var cleanContent = HtmlCleaner.StripHtml(chapter.Content);
    //            var idx = cleanContent.IndexOf(SearchString, StringComparison.OrdinalIgnoreCase);
    //            if (idx >= 0)
    //            {
    //                var snippetLength = 40;
    //                var start = Math.Max(0, idx - (snippetLength / 2));
    //                var length = Math.Min(snippetLength, cleanContent.Length - start);
    //                match = $"... {cleanContent.Substring(start, length)} ...";
    //            }
    //        }
    //        // Add the search result if a match was found
    //        if (match != null)
    //        {
    //            result.Add(new SearchResult
    //            {
    //                Id = chapter.Id,
    //                Match = match,
    //                Type = chapter.Title ?? string.Empty,
    //                Module = SearchResultModules.Help
    //            });
    //        }
    //        if (chapter.Chapters.Count > 0)
    //        {
    //            SearchChaptersAdvanced(chapter.Chapters, result);
    //        }
    //    }
    //}
}