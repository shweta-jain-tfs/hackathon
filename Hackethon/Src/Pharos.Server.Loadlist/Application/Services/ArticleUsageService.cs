using System.Drawing;
using Pharos.Server.Application.Parameters;
using Pharos.Server.LoadList.Domain;

namespace Pharos.Server.LoadList.Application.Services;

internal class ArticleUsageService : IArticleUsageService
{
    private readonly IArticlesService _articlesService;
    private readonly ISender _sender;
    private readonly ILoadListKeyService _keyService;

    public ArticleUsageService(IArticlesService articlesService, ISender sender, ILoadListKeyService keyService)
    {
        _articlesService = articlesService;
        _sender = sender;
        _keyService = keyService;
    }

    public async Task<ArticleUsage> GetInformation(string instrumentNumber, string? articleName, ArticleType articleType, string? methodName, CancellationToken cancellationToken)
    {
        var key = _keyService.GetKey(articleType, articleName, methodName);

        //Get from Last Loaded Reagent
        var reagentInfo = await _sender.Send(new GetLoadedArticleInformationQuery(instrumentNumber, key), cancellationToken);

        if (reagentInfo != null)
        {
            return MapArticle(instrumentNumber, reagentInfo.Article, reagentInfo.LotNumber);
        }

        //Nothing found in Last Loaded Reagent, get from ArticlesService
        var articleInfo = await GetArticle(articleName, methodName, articleType);

        if (articleInfo != null)
        {
            return MapArticle(instrumentNumber, articleInfo);
        }

        //Nothing found in ArticlesService, return default
        return new ArticleUsage
        {
            InstrumentNumber = instrumentNumber,
            Quantity = 1,
            FriendlyName = articleName ?? "Unknown article",
            FullName = "Unknown article"
        };
    }

    private async Task<Article?> GetArticle(string? identity, string? methodName, ArticleType articleType)
    {
        if (articleType == ArticleType.DilutionPlate)
        {
            var articles = await _articlesService.GetArticles(x => x.ArticleType == ArticleType.DilutionPlate, x => x);
            return articles.FirstOrDefault();
        }

        return await _articlesService.GetArticleFromIdentity(identity, methodName, articleType);
    }

    private static ArticleUsage MapArticle(string instrumentNumber, Article article, string? lotNumber = null)
    {
        return new ArticleUsage
        {
            InstrumentNumber = instrumentNumber,
            ArticleIdentity = article.Identity ?? string.Empty,
            Quantity = article.Quantity,
            FullName = article.FullName,
            Shape = article.Shape,
            Colors = article.Colors.Select(c => c.HasValue ? ColorTranslator.ToHtml(c.Value) : null).ToList(),
            FriendlyName = article.FriendlyName,
            Texts = article.Texts,
            LotNumber = lotNumber ?? string.Empty
        };
    }

    public async Task<QualityControlTypeDto> GetQualityControlType(string qualityControlName, CancellationToken cancellationToken)
    {
        return await _sender.Send(new GetQualityControlTypeQuery(qualityControlName), cancellationToken);
    }
}