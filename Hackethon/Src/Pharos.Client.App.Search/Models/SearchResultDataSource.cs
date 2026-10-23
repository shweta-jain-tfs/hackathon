namespace Pharos.Client.App.Search.Models;

public interface ISearchResultDataSource
{
    Task<List<SearchResultItem>> GetSearchResultAsync(string searchString);
    Task QueryAskMissingRequestAsync(string sampleId);
}

internal sealed class SearchResultDataSource : ISearchResultDataSource
{
    private readonly HttpClient _httpClient;

    public SearchResultDataSource(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<SearchResultItem>> GetSearchResultAsync(string searchString)
    {
        var client = new SearchClient(_httpClient);
        var results = await client.GetSearchResultAsync(searchString);
        return results.Select(x => x.ToSearchResultItem()).ToList();
    }

    public async Task QueryAskMissingRequestAsync(string sampleId)
    {
        var client = new LisClient(_httpClient);
        await client.QueryAskMissingRequestAsync(sampleId);
    }
}