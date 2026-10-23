using Pharos.Server.Application.Queries;

namespace Pharos.Server.Application;

public interface ISearchProvider
{
    /// <summary>
    /// Searches shall be case-insensitive.
    /// </summary>
    /// <param name="searchString"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<List<SearchResultDto>> Search(string searchString, CancellationToken cancellationToken);
}