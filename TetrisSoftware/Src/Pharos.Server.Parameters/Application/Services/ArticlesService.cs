using System.Linq.Expressions;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Domain.Parameters;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Services;

internal class ArticlesService(IParametersDbContext dbContext) : IArticlesService
{
    public Task<Article?> GetArticle(string articleNumber)
    {
        return dbContext.Articles
            .Where(x => articleNumber == x.ArticleNumber)
            .Include(x => x.CartridgeCells)
            .AsNoTracking()
            .FirstOrDefaultAsync();
    }

    public Task<List<Article>> GetArticles()
    {
        return dbContext.Articles
            .AsQueryable()
            .Include(x => x.CartridgeCells)
            .AsNoTracking()
            .ToListAsync();
    }

    public Task<List<Article>> GetArticles(IReadOnlyList<string> articleNumbers)
    {
        return dbContext.Articles
            .Where(x => articleNumbers.Contains(x.ArticleNumber))
            .Include(x => x.CartridgeCells)
            .AsNoTracking()
            .ToListAsync();
    }

    public Task<Article?> GetArticleFromBarcode(string barcode)
    {
        if (string.IsNullOrEmpty(barcode) || barcode.Length < 3)
        {
            return Task.FromResult<Article?>(null);
        }

        return GetArticle(barcode[..3]);
    }

    public Task<List<Article>> GetCarrierArticlesFromIdentities(IReadOnlyList<string> identities)
    {
        return dbContext.Articles
            .Where(x => identities.Contains(x.Identity))
            .AsNoTracking()
            .ToListAsync();
    }

    public Task<List<TResult>> GetArticles<TResult>(Expression<Func<Article, bool>> filter, Expression<Func<Article, TResult>> selector)
    {
        return dbContext.Articles
            .Where(filter)
            .AsNoTracking()
            .Select(selector)
            .ToListAsync();
    }

    public async Task<Article?> GetArticleFromIdentity(string? identity, string? methodName, ArticleType? articleType)
    {
        var market = GetIntendedMarket();

        // Get articles matching market packages (and optionally identity)
        var articleQuery =
            from pi in dbContext.PackageItems
            join a in dbContext.Articles on pi.ArticleId equals a.Id
            join p in dbContext.Packages on pi.PackageId equals p.Id
            where p.IntendedMarketId == market
            select a;

        // Filter by identity only if provided
        if (!string.IsNullOrEmpty(identity))
        {
            articleQuery = articleQuery.Where(a => a.Identity == identity);
        }

        // Filter by methodName if provided
        if (!string.IsNullOrEmpty(methodName))
        {
            articleQuery = articleQuery.Where(a => a.MethodName == methodName);
        }

        // Filter by articleType if provided
        if (articleType.HasValue)
        {
            articleQuery = articleQuery.Where(a => a.ArticleType == articleType.Value);
        }

        var art = articleQuery.Distinct().AsNoTracking();

        // If there is only one article, return it.
        // If there are multiple matches, return the one with the highest article number.
        var count = await art.CountAsync();
        switch (count)
        {
            case 1:
                return await art.FirstOrDefaultAsync();
            case > 1:
                return await art.OrderByDescending(x => x.ArticleNumber).FirstOrDefaultAsync();
        }

        // Fallback when none found in package context
        var fallback = dbContext.Articles.AsQueryable();

        if (!string.IsNullOrEmpty(identity))
        {
            fallback = fallback.Where(x => x.Identity == identity);
        }

        if (!string.IsNullOrEmpty(methodName))
        {
            fallback = fallback.Where(x => x.MethodName == methodName);
        }

        if (articleType.HasValue)
        {
            fallback = fallback.Where(x => x.ArticleType == articleType.Value);
        }

        return await fallback
            .AsNoTracking()
            .OrderByDescending(x => x.ArticleNumber)
            .FirstOrDefaultAsync();
    }

    private static int GetIntendedMarket()
    {
        //Todo: get correct intended market
        return IntendedMarket.Worldwide;
    }
}