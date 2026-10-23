using System.Collections.Concurrent;
using MediatR;
using Pharos.Server.Application;
using Pharos.Server.Application.Queries;

namespace Pharos.Server.Search.Application.Queries;

public record GetSearchResultQuery(string SearchString) : IRequest<SearchResultDto[]>;

internal class GetInstrumentsQueryHandler(IEnumerable<ISearchProvider> searchProviders) : IRequestHandler<GetSearchResultQuery, SearchResultDto[]>
{
    public async Task<SearchResultDto[]> Handle(GetSearchResultQuery request, CancellationToken cancellationToken)
    {
        var searchString = request.SearchString?.Trim();

        // Return empty results if search string is empty or whitespace-only
        if (string.IsNullOrWhiteSpace(searchString))
        {
            return Array.Empty<SearchResultDto>();
        }

        // Combine results
        var searchResult = new ConcurrentBag<SearchResultDto>();

        var tasks = searchProviders.Select(async searchableModule =>
        {
            var items = await searchableModule.Search(searchString, cancellationToken);

            foreach (var item in items)
            {
                searchResult.Add(item);
            }
        });

        await Task.WhenAll(tasks);

        return searchResult.ToArray();
    }
}