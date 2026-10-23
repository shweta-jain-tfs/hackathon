using Microsoft.EntityFrameworkCore;
using Pharos.Server.Application.Parameters;

namespace Pharos.Server.LoadList.Application.Services;

internal record GetLoadedArticleInformationQuery(string InstrumentNumber, string ReagentKey) : IRequest<ArticleWithLotNumber?>;

internal record ArticleWithLotNumber(Article Article, string LotNumber);

internal class GetLoadedArticleInformationQueryHandler : IRequestHandler<GetLoadedArticleInformationQuery, ArticleWithLotNumber?>
{
    private readonly ILoadListDbContext _dbContext;
    private readonly IArticlesService _articlesService;

    public GetLoadedArticleInformationQueryHandler(ILoadListDbContext dbContext, IArticlesService articlesService)
    {
        _dbContext = dbContext;
        _articlesService = articlesService;
    }

    public async Task<ArticleWithLotNumber?> Handle(GetLoadedArticleInformationQuery request, CancellationToken cancellationToken)
    {
        // look for the last loaded reagent for the given instrument and reagent key
        var loadedReagent = await _dbContext.LastLoadedReagents.AsNoTracking().FirstOrDefaultAsync(x => x.InstrumentNumber == request.InstrumentNumber && x.ReagentKey == request.ReagentKey, cancellationToken);
        if (loadedReagent == null)
        {
            // nothing loaded for this instrument and reagent key, try to find the last loaded reagent for the given reagent key only
            loadedReagent = await _dbContext.LastLoadedReagents.AsNoTracking().FirstOrDefaultAsync(x => x.ReagentKey == request.ReagentKey, cancellationToken);
            if(loadedReagent == null)
            {
                return null;
            }
        }
        
        var article = await _articlesService.GetArticle(loadedReagent.ArticleNumber);
        if (article == null)
        {
            return null;
        }

        return new ArticleWithLotNumber(article, loadedReagent.LotNumber);
    }
}