using Pharos.Client.App.Search.Models;
using Pharos.Client.App.Search.ViewModels;
using Pharos.Client.Common.App.PageRouting;
using Pharos.Common.Web.Utilities;

namespace Pharos.Client.App.Search.Views;

public partial class SearchResultListPage
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] private SearchResultListPageViewModel PageViewModel { get; set; } = default!;

    [Parameter]
    [SupplyParameterFromQuery(Name = "search")]
    public string Search { get; set; } = string.Empty;

    private ElementReference _focusElement;
    private bool _shouldFocus;

    private string? SearchPageTitle { get; set; }

    protected override void OnParametersSet()
    {
        // Trim whitespace and validate the search parameter before passing it to the view model
        var trimmedSearch = Search?.Trim();
        PageViewModel.SearchString = string.IsNullOrWhiteSpace(trimmedSearch) ? string.Empty : trimmedSearch;
        _shouldFocus = true;

        SearchPageTitle = @Loc["SearchListPage_Title"].Value.Replace("{0}", Search);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_shouldFocus)
        {
            // take focus from the search field
            _shouldFocus = false;
            await _focusElement.FocusAsync();
        }
    }

    private void OnViewRefreshNeeded(object? sender, EventArgs e)
    {
        StateHasChanged();
    }

    public static string GetSearchResultLink(SearchResultItem searchResult)
    {
        return searchResult.Module switch
        {
            ClientSearchResultModule.Request =>
                $"{PageRoute.SampleDetails.Default}/{searchResult.Id}",

            ClientSearchResultModule.Parameters when searchResult.ResultType is ClientSearchResultType.RequestorId or ClientSearchResultType.RequestorName =>
                $"{PageRoute.Parameters.RequestorDetails.Default}/{searchResult.Id}",

            ClientSearchResultModule.Parameters when searchResult.ResultType is ClientSearchResultType.ArticleFullName or ClientSearchResultType.ArticleIdentity =>
                $"{PageRoute.Parameters.PackageDetails.Default}/{searchResult.Id}",

            ClientSearchResultModule.Parameters =>
                throw new ArgumentOutOfRangeException(nameof(searchResult), searchResult.ResultType, "Unsupported parameters search result type."),

            ClientSearchResultModule.Settings =>
                $"{PageRoute.Settings}/{searchResult.StringId}",

            ClientSearchResultModule.Help =>
                $"{PageRoute.Help}/#{searchResult.StringId}",

            _ or ClientSearchResultModule.Unknown => throw new ArgumentOutOfRangeException(nameof(searchResult), searchResult.Module, "Unsupported search result module.")
        };
    }

    private MarkupString Highlight(string? text)
    {
        return TextHighlighter.Highlight(text, Search);
    }
}