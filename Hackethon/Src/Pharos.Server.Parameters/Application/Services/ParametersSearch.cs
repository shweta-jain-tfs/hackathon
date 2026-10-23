using Pharos.Server.Application;
using Pharos.Server.Application.Queries;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Services;

internal class ParametersSearch : ISearchProvider
{
    private readonly IParametersDbContext _dbContext;
    public ParametersSearch(IParametersDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<List<SearchResultDto>> Search(string searchString, CancellationToken cancellationToken)
    {
        // Return empty results if search string is empty or whitespace-only
        if (string.IsNullOrWhiteSpace(searchString))
        {
            return new List<SearchResultDto>();
        }

        // Get all articles that match the search criteria
        var matchingArticles = await _dbContext.PackageItems
            .Include(p => p.Article)
            .Include(p => p.Package)
            .Where(p =>
                (p.Article.Identity != null && p.Article.Identity.Contains(searchString)) ||
                (p.Article.FullName != null && p.Article.FullName.Contains(searchString)))
            .Take(50)
            .ToListAsync(cancellationToken);

        var results = new List<SearchResultDto>();

        // For each matching article, create separate results for each field that matches
        foreach (var matchedArticle in matchingArticles)
        {
            // Check Identity
            if (matchedArticle.Article.Identity != null && matchedArticle.Article.Identity.Contains(searchString, StringComparison.OrdinalIgnoreCase))
            {
                results.Add(new SearchResultDto
                {
                    Id = matchedArticle.PackageId,
                    Match = matchedArticle.Article.Identity,
                    ResultType = SearchResultType.ArticleIdentity,
                    DestinationHint = matchedArticle.Package.OrderingNumber,
                    Module = SearchResultModules.Parameters
                });
            }

            // Check FullName
            if (matchedArticle.Article.FullName != null && matchedArticle.Article.FullName.Contains(searchString, StringComparison.OrdinalIgnoreCase))
            {
                results.Add(new SearchResultDto
                {
                    Id = matchedArticle.PackageId,
                    Match = matchedArticle.Article.FullName,
                    ResultType = SearchResultType.ArticleFullName,
                    DestinationHint = matchedArticle.Package.OrderingNumber,
                    Module = SearchResultModules.Parameters
                });
            }
        }

        return results;
    }
}