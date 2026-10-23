using System.Linq.Expressions;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Parameters;

namespace Pharos.Server.Application.Parameters;

public interface IArticlesService
{
    Task<Article?> GetArticle(string articleNumber);
    Task<List<Article>> GetArticles();
    Task<List<Article>> GetArticles(IReadOnlyList<string> articleNumbers);
    Task<Article?> GetArticleFromBarcode(string barcode);
    Task<List<Article>> GetCarrierArticlesFromIdentities(IReadOnlyList<string> identities);
    Task<Article?> GetArticleFromIdentity(string? identity, string? methodName, ArticleType? articleType);
    Task<List<TResult>> GetArticles<TResult>(Expression<Func<Article, bool>> filter, Expression<Func<Article, TResult>> selector);
}