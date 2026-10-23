using Microsoft.Extensions.Logging;
using Pharos.Server.Domain.Parameters;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Commands.ImportArticles;

/// <summary>
/// Handler for updating articles and packages in the database from imported articles/packages
/// </summary>
internal class UpdateArticleParameterHandler(
    IParametersDbContext dbContext,
    ILogger<UpdateArticleParameterHandler> logger)
{
    public async Task Handle(ArticleParameters articleParameters)
    {
        var version = await dbContext.ArticlesVersion.FirstOrDefaultAsync();
        if (version != null)
        {
            if (version.VersionNumber > articleParameters.Version)
            {
                logger.LogInformation("Articles version {PackageArticlesVersion} in the package is not higher existing version {DatabaseArticlesVersion}", articleParameters.Version, version.VersionNumber);
                return;
            }
            dbContext.Entry(version).State = EntityState.Deleted;
        }
        dbContext.ArticlesVersion.Add(new Domain.ArticlesVersion { VersionNumber = articleParameters.Version });

        await UpdateMarketsFromImport(articleParameters.Markets);

        var articles = await UpdateArticlesFromImport(articleParameters.Articles);

        await UpdatePackagesFromImport(articles, articleParameters.Packages);

        await dbContext.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
    }

    private async Task UpdateMarketsFromImport(List<IntendedMarket> importedMarkets)
    {
        var marketsFromDatabase = await dbContext.IntendedMarkets.AsQueryable().ToListAsync();
        foreach (var market in importedMarkets)
        {
            if (marketsFromDatabase.Any(x => x.Id == market.Id))
            {
                continue;
            }
            dbContext.IntendedMarkets.Add(market);
        }
    }

    private async Task<List<Article>> UpdateArticlesFromImport(List<Article> importedArticles)
    {
        var articlesFromDatabase = await dbContext.Articles
            .AsQueryable()
            .Include(x => x.CartridgeCells)
            .ToListAsync();

        foreach (var importedArticle in importedArticles)
        {
            var article = articlesFromDatabase.Find(x => x.ArticleNumber == importedArticle.ArticleNumber);

            if ((int)importedArticle.ArticleType == -1)
            {
                if (article != null)
                {
                    dbContext.Articles.Remove(article);
                    articlesFromDatabase.Remove(article);
                }

                continue;
            }

            if (article == null)
            {
                article = new Article();
                dbContext.Articles.Add(article);
                articlesFromDatabase.Add(article);
                article.ArticleNumber = importedArticle.ArticleNumber;
                article.CartridgeCells = new List<CartridgeCellIdentity>();
            }

            article.Identity = importedArticle.Identity;
            article.FullName = importedArticle.FullName;
            article.Quantity = importedArticle.Quantity;
            article.ArticleType = importedArticle.ArticleType;
            article.NumberOfCells = importedArticle.NumberOfCells;
            article.MaxDaysInInstrument = importedArticle.MaxDaysInInstrument;
            article.MaxHoursInInstrument = importedArticle.MaxHoursInInstrument;
            article.MethodName = importedArticle.MethodName;
            article.MaxPiercedHoles = importedArticle.MaxPiercedHoles;
            article.FriendlyName = importedArticle.FriendlyName;
            article.Text1 = importedArticle.Text1;
            article.Text2 = importedArticle.Text2;
            article.Text3 = importedArticle.Text3;
            article.Text4 = importedArticle.Text4;
            article.Shape = importedArticle.Shape;
            article.Color1 = importedArticle.Color1;
            article.Color2 = importedArticle.Color2;
            article.Color3 = importedArticle.Color3;
            article.Color4 = importedArticle.Color4;

            if (article.CartridgeCells != null)
            {
                foreach (var cell in article.CartridgeCells)
                {
                    if (importedArticle.CartridgeCells?.All(x => x.CellPosition != cell.CellPosition) ?? true)
                    {
                        dbContext.Entry(cell).State = EntityState.Deleted;
                    }
                }
            }

            if (importedArticle.CartridgeCells != null)
            {
                foreach (var cartridgeCell in importedArticle.CartridgeCells)
                {
                    article.CartridgeCells ??= new List<CartridgeCellIdentity>();
                    var cell = article.CartridgeCells.FirstOrDefault(x => x.CellPosition == cartridgeCell.CellPosition);
                    if (cell != null)
                    {
                        cell.CellIdentity = cartridgeCell.CellIdentity;
                    }
                    else
                    {
                        article.CartridgeCells.Add(cartridgeCell);
                    }
                }
            }
        }

        return articlesFromDatabase;
    }

    private async Task UpdatePackagesFromImport(List<Article> articles, List<Package> importedPackages)
    {
        var packagesFromDatabase = await dbContext.Packages
            .AsQueryable()
            .Include(x => x.PackageItems)
            .ToListAsync();

        foreach (var importedPackage in importedPackages)
        {
            var package = packagesFromDatabase.Find(x => x.PackageBarcode == importedPackage.PackageBarcode);

            if (importedPackage.OrderingNumber == "-1")
            {
                if (package != null)
                {
                    dbContext.Packages.Remove(package);
                    packagesFromDatabase.Remove(package);
                }

                continue;
            }

            if (package == null)
            {
                package = new Package
                {
                    PackageBarcode = importedPackage.PackageBarcode,
                    PackageItems = new List<PackageItem>()
                };
                dbContext.Packages.Add(package);
                packagesFromDatabase.Add(package);
            }
            package.IntendedMarketId = importedPackage.IntendedMarketId;
            package.OrderingNumber = importedPackage.OrderingNumber;
            package.PackageFullName = importedPackage.PackageFullName;

            foreach (var importedPackageItem in importedPackage.PackageItems)
            {
                // Entity Framework Core will automatically fix-up navigation properties
                // to any other entities that were previously loaded into the context instance.
                // Thus, PackageItem objects fetched to the database will contain references
                // to existing articles from the database, because we've fetched them before.
                var articleNumber = importedPackageItem.Article.ArticleNumber;
                var packageContent = package.PackageItems.FirstOrDefault(x => x.Article.ArticleNumber == articleNumber);
                if (packageContent == null)
                {
                    packageContent = new PackageItem
                    {
                        Article = articles.First(x => x.ArticleNumber == articleNumber)
                    };
                    package.PackageItems.Add(packageContent);
                }
                packageContent.ArticleAmount = importedPackageItem.ArticleAmount;
            }
        }
    }
}
