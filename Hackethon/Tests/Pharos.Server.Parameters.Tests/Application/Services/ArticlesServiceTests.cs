using MockQueryable.Moq;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Parameters;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Services;

public class ArticlesServiceTests
{
    private readonly Mock<IParametersDbContext> _mockContext;
    private readonly ArticlesService _service;
    private readonly Fixture _fixture;
    private const string _defaultMethodName = "sIgE";
    private const string _alternativeMethodName = "tIgE";

    public ArticlesServiceTests()
    {
        _mockContext = new Mock<IParametersDbContext>();
        _service = new ArticlesService(_mockContext.Object);
        _fixture = new Fixture();
    }

    #region GetArticleFromIdentity Tests

    [Fact]
    public async Task GetArticleFromIdentity_ReturnsNull_WhenNoArticlesExist()
    {
        // Arrange
        SetupEmptyDatabase();

        // Act
        var result = await _service.GetArticleFromIdentity("g1", _defaultMethodName, ArticleType.CalibratorCartridge);

        // Assert
        result.Should().BeNull();
    }

    [Trait("ProductRequirement", "PR_1801")]
    [Trait("ModuleRequirement", "MR_5970")]
    [Fact]
    public async Task GetArticleFromIdentity_ReturnsArticle_WhenSingleMatchInPackage()
    {
        // Arrange
        var article = CreateArticle("030", "g1", _defaultMethodName, ArticleType.CalibratorCartridge);
        var package = CreatePackage(1, IntendedMarket.Worldwide);
        var packageItem = CreatePackageItem(article.Id, package.Id, article, package);

        SetupDatabase([article], [package], [packageItem]);

        // Act
        var result = await _service.GetArticleFromIdentity("g1", _defaultMethodName, ArticleType.CalibratorCartridge);

        // Assert
        result.Should().NotBeNull();
        result!.ArticleNumber.Should().Be("030");
        result.Identity.Should().Be("g1");
        result.MethodName.Should().Be(_defaultMethodName);
        result.ArticleType.Should().Be(ArticleType.CalibratorCartridge);
    }

    [Trait("ProductRequirement", "PR_1801")]
    [Trait("ModuleRequirement", "MR_5970")]
    [Fact]
    public async Task GetArticleFromIdentity_ReturnsHighestArticleNumber_WhenMultipleMatchesInPackage()
    {
        // Arrange
        var article1 = CreateArticle("030", "g1", _defaultMethodName, ArticleType.CalibratorCartridge);
        var article2 = CreateArticle("040", "g1", _defaultMethodName, ArticleType.CalibratorCartridge);
        var article3 = CreateArticle("035", "g1", _defaultMethodName, ArticleType.CalibratorCartridge);
        var package = CreatePackage(1, IntendedMarket.Worldwide);
        var packageItem1 = CreatePackageItem(article1.Id, package.Id, article1, package);
        var packageItem2 = CreatePackageItem(article2.Id, package.Id, article2, package);
        var packageItem3 = CreatePackageItem(article3.Id, package.Id, article3, package);

        SetupDatabase([article1, article2, article3], [package], [packageItem1, packageItem2, packageItem3]);

        // Act
        var result = await _service.GetArticleFromIdentity("g1", _defaultMethodName, ArticleType.CalibratorCartridge);

        // Assert
        result.Should().NotBeNull();
        result!.ArticleNumber.Should().Be("040", "it should return the article with the highest article number");
    }

    [Trait("ProductRequirement", "PR_1801")]
    [Trait("ModuleRequirement", "MR_5970")]
    [Fact]
    public async Task GetArticleFromIdentity_FiltersByIdentity_WhenProvided()
    {
        // Arrange
        var article1 = CreateArticle("030", "g1", _defaultMethodName, ArticleType.CalibratorCartridge);
        var article2 = CreateArticle("040", "g2", _defaultMethodName, ArticleType.CalibratorCartridge);
        var package = CreatePackage(1, IntendedMarket.Worldwide);
        var packageItem1 = CreatePackageItem(article1.Id, package.Id, article1, package);
        var packageItem2 = CreatePackageItem(article2.Id, package.Id, article2, package);

        SetupDatabase([article1, article2], [package], [packageItem1, packageItem2]);

        // Act
        var result = await _service.GetArticleFromIdentity("g2", _defaultMethodName, ArticleType.CalibratorCartridge);

        // Assert
        result.Should().NotBeNull();
        result!.Identity.Should().Be("g2");
        result.ArticleNumber.Should().Be("040");
    }

    [Trait("ProductRequirement", "PR_1801")]
    [Trait("ModuleRequirement", "MR_5970")]
    [Fact]
    public async Task GetArticleFromIdentity_FiltersByMethodName_WhenProvided()
    {
        // Arrange
        var article1 = CreateArticle("030", "g1", _defaultMethodName, ArticleType.CalibratorCartridge);
        var article2 = CreateArticle("040", "g1", _alternativeMethodName, ArticleType.CalibratorCartridge);
        var package = CreatePackage(1, IntendedMarket.Worldwide);
        var packageItem1 = CreatePackageItem(article1.Id, package.Id, article1, package);
        var packageItem2 = CreatePackageItem(article2.Id, package.Id, article2, package);

        SetupDatabase([article1, article2], [package], [packageItem1, packageItem2]);

        // Act
        var result = await _service.GetArticleFromIdentity("g1", _alternativeMethodName, ArticleType.CalibratorCartridge);

        // Assert
        result.Should().NotBeNull();
        result!.MethodName.Should().Be(_alternativeMethodName);
        result.ArticleNumber.Should().Be("040");
    }

    [Trait("ProductRequirement", "PR_1801")]
    [Trait("ModuleRequirement", "MR_5970")]
    [Fact]
    public async Task GetArticleFromIdentity_FiltersByArticleType_WhenProvided()
    {
        // Arrange
        var article1 = CreateArticle("030", "g1", _defaultMethodName, ArticleType.CalibratorCartridge);
        var article2 = CreateArticle("040", "g1", _defaultMethodName, ArticleType.ConjugateCartridge);
        var package = CreatePackage(1, IntendedMarket.Worldwide);
        var packageItem1 = CreatePackageItem(article1.Id, package.Id, article1, package);
        var packageItem2 = CreatePackageItem(article2.Id, package.Id, article2, package);

        SetupDatabase([article1, article2], [package], [packageItem1, packageItem2]);

        // Act
        var result = await _service.GetArticleFromIdentity("g1", _defaultMethodName, ArticleType.ConjugateCartridge);

        // Assert
        result.Should().NotBeNull();
        result!.ArticleType.Should().Be(ArticleType.ConjugateCartridge);
        result.ArticleNumber.Should().Be("040");
    }

    [Trait("ProductRequirement", "PR_1801")]
    [Trait("ModuleRequirement", "MR_5970")]
    [Fact]
    public async Task GetArticleFromIdentity_ReturnsAllMatches_WhenOnlyIdentityProvided()
    {
        // Arrange
        var article1 = CreateArticle("030", "g1", _defaultMethodName, ArticleType.CalibratorCartridge);
        var article2 = CreateArticle("040", "g1", _alternativeMethodName, ArticleType.ConjugateCartridge);
        var article3 = CreateArticle("050", "g1", _defaultMethodName, ArticleType.CurveControlCartridge);
        var package = CreatePackage(1, IntendedMarket.Worldwide);
        var packageItem1 = CreatePackageItem(article1.Id, package.Id, article1, package);
        var packageItem2 = CreatePackageItem(article2.Id, package.Id, article2, package);
        var packageItem3 = CreatePackageItem(article3.Id, package.Id, article3, package);

        SetupDatabase([article1, article2, article3], [package], [packageItem1, packageItem2, packageItem3]);

        // Act
        var result = await _service.GetArticleFromIdentity("g1", null, null);

        // Assert
        result.Should().NotBeNull();
        result!.Identity.Should().Be("g1");
        result.ArticleNumber.Should().Be("050", "it should return the article with the highest article number");
    }

    [Trait("ProductRequirement", "PR_1801")]
    [Trait("ModuleRequirement", "MR_5970")]
    [Fact]
    public async Task GetArticleFromIdentity_FallsBackToArticlesTable_WhenNotFoundInPackages()
    {
        // Arrange
        var articleInPackage = CreateArticle("030", "g1", _defaultMethodName, ArticleType.CalibratorCartridge);
        var articleNotInPackage = CreateArticle("040", "g2", _alternativeMethodName, ArticleType.ConjugateCartridge);
        var package = CreatePackage(1, IntendedMarket.Worldwide);
        var packageItem = CreatePackageItem(articleInPackage.Id, package.Id, articleInPackage, package);

        SetupDatabase([articleInPackage, articleNotInPackage], [package], [packageItem]);

        // Act
        var result = await _service.GetArticleFromIdentity("g2", _alternativeMethodName, ArticleType.ConjugateCartridge);

        // Assert
        result.Should().NotBeNull();
        result!.ArticleNumber.Should().Be("040");
        result.Identity.Should().Be("g2");
    }

    [Trait("ProductRequirement", "PR_1801")]
    [Trait("ModuleRequirement", "MR_5970")]
    [Fact]
    public async Task GetArticleFromIdentity_FallsBackReturnsHighestArticleNumber_WhenMultipleMatches()
    {
        // Arrange
        var article1 = CreateArticle("030", "g2", _alternativeMethodName, ArticleType.ConjugateCartridge);
        var article2 = CreateArticle("050", "g2", _alternativeMethodName, ArticleType.ConjugateCartridge);
        var article3 = CreateArticle("040", "g2", _alternativeMethodName, ArticleType.ConjugateCartridge);
        var package = CreatePackage(1, IntendedMarket.Worldwide);

        // None of these articles are in the package, so fallback will be used
        SetupDatabase([article1, article2, article3], [package], []);

        // Act
        var result = await _service.GetArticleFromIdentity("g2", _alternativeMethodName, ArticleType.ConjugateCartridge);

        // Assert
        result.Should().NotBeNull();
        result!.ArticleNumber.Should().Be("050", "fallback should also return the highest article number");
    }

    [Trait("ProductRequirement", "PR_1801")]
    [Trait("ModuleRequirement", "MR_5970")]
    [Fact]
    public async Task GetArticleFromIdentity_ReturnsNull_WhenNoMatchInPackagesOrFallback()
    {
        // Arrange
        var article = CreateArticle("030", "g1", _defaultMethodName, ArticleType.CalibratorCartridge);
        var package = CreatePackage(1, IntendedMarket.Worldwide);
        var packageItem = CreatePackageItem(article.Id, package.Id, article, package);

        SetupDatabase([article], [package], [packageItem]);

        // Act
        var result = await _service.GetArticleFromIdentity("g2", _alternativeMethodName, ArticleType.ConjugateCartridge);

        // Assert
        result.Should().BeNull();
    }

    [Trait("ProductRequirement", "PR_1801")]
    [Trait("ModuleRequirement", "MR_5970")]
    [Fact]
    public async Task GetArticleFromIdentity_WorksWithNullIdentity()
    {
        // Arrange
        var article1 = CreateArticle("030", "g1", _defaultMethodName, ArticleType.CalibratorCartridge);
        var article2 = CreateArticle("040", "g2", _alternativeMethodName, ArticleType.ConjugateCartridge);
        var package = CreatePackage(1, IntendedMarket.Worldwide);
        var packageItem1 = CreatePackageItem(article1.Id, package.Id, article1, package);
        var packageItem2 = CreatePackageItem(article2.Id, package.Id, article2, package);

        SetupDatabase([article1, article2], [package], [packageItem1, packageItem2]);

        // Act
        var result = await _service.GetArticleFromIdentity(null, _alternativeMethodName, ArticleType.ConjugateCartridge);

        // Assert
        result.Should().NotBeNull();
        result!.MethodName.Should().Be(_alternativeMethodName);
        result.ArticleType.Should().Be(ArticleType.ConjugateCartridge);
    }

    [Trait("ProductRequirement", "PR_1801")]
    [Trait("ModuleRequirement", "MR_5970")]
    [Fact]
    public async Task GetArticleFromIdentity_WorksWithNullMethodName()
    {
        // Arrange
        var article1 = CreateArticle("030", "g1", _defaultMethodName, ArticleType.CalibratorCartridge);
        var article2 = CreateArticle("040", "g1", _alternativeMethodName, ArticleType.ConjugateCartridge);
        var package = CreatePackage(1, IntendedMarket.Worldwide);
        var packageItem1 = CreatePackageItem(article1.Id, package.Id, article1, package);
        var packageItem2 = CreatePackageItem(article2.Id, package.Id, article2, package);

        SetupDatabase([article1, article2], [package], [packageItem1, packageItem2]);

        // Act
        var result = await _service.GetArticleFromIdentity("g1", null, ArticleType.ConjugateCartridge);

        // Assert
        result.Should().NotBeNull();
        result!.Identity.Should().Be("g1");
        result.ArticleType.Should().Be(ArticleType.ConjugateCartridge);
    }

    [Trait("ProductRequirement", "PR_1801")]
    [Trait("ModuleRequirement", "MR_5970")]
    [Fact]
    public async Task GetArticleFromIdentity_WorksWithNullArticleType()
    {
        // Arrange
        var article1 = CreateArticle("030", "g1", _defaultMethodName, ArticleType.CalibratorCartridge);
        var article2 = CreateArticle("040", "g1", _defaultMethodName, ArticleType.ConjugateCartridge);
        var package = CreatePackage(1, IntendedMarket.Worldwide);
        var packageItem1 = CreatePackageItem(article1.Id, package.Id, article1, package);
        var packageItem2 = CreatePackageItem(article2.Id, package.Id, article2, package);

        SetupDatabase([article1, article2], [package], [packageItem1, packageItem2]);

        // Act
        var result = await _service.GetArticleFromIdentity("g1", _defaultMethodName, null);

        // Assert
        result.Should().NotBeNull();
        result!.Identity.Should().Be("g1");
        result.MethodName.Should().Be(_defaultMethodName);
    }

    [Trait("ProductRequirement", "PR_1801")]
    [Trait("ModuleRequirement", "MR_5970")]
    [Fact]
    public async Task GetArticleFromIdentity_WorksWithAllNullParameters()
    {
        // Arrange
        var article1 = CreateArticle("030", "g1", _defaultMethodName, ArticleType.CalibratorCartridge);
        var article2 = CreateArticle("050", "g2", _alternativeMethodName, ArticleType.ConjugateCartridge);
        var article3 = CreateArticle("040", "g3", _alternativeMethodName, ArticleType.CurveControlCartridge);
        var package = CreatePackage(1, IntendedMarket.Worldwide);
        var packageItem1 = CreatePackageItem(article1.Id, package.Id, article1, package);
        var packageItem2 = CreatePackageItem(article2.Id, package.Id, article2, package);
        var packageItem3 = CreatePackageItem(article3.Id, package.Id, article3, package);

        SetupDatabase([article1, article2, article3], [package], [packageItem1, packageItem2, packageItem3]);

        // Act
        var result = await _service.GetArticleFromIdentity(null, null, null);

        // Assert
        result.Should().NotBeNull();
        result!.ArticleNumber.Should().Be("050", "should return the highest article number when no filters applied");
    }

    [Trait("ProductRequirement", "PR_1801")]
    [Trait("ModuleRequirement", "MR_5970")]
    [Fact]
    public async Task GetArticleFromIdentity_OnlyConsidersIntendedMarket()
    {
        // Arrange
        var articleInternational = CreateArticle("030", "g1", _defaultMethodName, ArticleType.CalibratorCartridge);
        var articleJapan = CreateArticle("040", "g1", _defaultMethodName, ArticleType.CalibratorCartridge);

        var packageInternational = CreatePackage(1, IntendedMarket.Worldwide);
        var packageJapan = CreatePackage(2, IntendedMarket.Japan);

        var packageItem1 = CreatePackageItem(articleInternational.Id, packageInternational.Id, articleInternational, packageInternational);
        var packageItem2 = CreatePackageItem(articleJapan.Id, packageJapan.Id, articleJapan, packageJapan);

        SetupDatabase([articleInternational, articleJapan], [packageInternational, packageJapan], [packageItem1, packageItem2]);

        // Act
        var result = await _service.GetArticleFromIdentity("g1", _defaultMethodName, ArticleType.CalibratorCartridge);

        // Assert
        result.Should().NotBeNull();
        result!.ArticleNumber.Should().Be("030", "should only consider articles in intended market packages");
    }

    [Trait("ProductRequirement", "PR_1801")]
    [Trait("ModuleRequirement", "MR_5970")]
    [Fact]
    public async Task GetArticleFromIdentity_DistinctArticles_WhenArticleInMultiplePackages()
    {
        // Arrange
        var article = CreateArticle("030", "g1", _defaultMethodName, ArticleType.CalibratorCartridge);
        var package1 = CreatePackage(1, IntendedMarket.Worldwide);
        var package2 = CreatePackage(2, IntendedMarket.Worldwide);
        var packageItem1 = CreatePackageItem(article.Id, package1.Id, article, package1);
        var packageItem2 = CreatePackageItem(article.Id, package2.Id, article, package2);

        SetupDatabase([article], [package1, package2], [packageItem1, packageItem2]);

        // Act
        var result = await _service.GetArticleFromIdentity("g1", _defaultMethodName, ArticleType.CalibratorCartridge);

        // Assert
        result.Should().NotBeNull();
        result!.ArticleNumber.Should().Be("030");
        result.Identity.Should().Be("g1");
    }

    #endregion

    #region Helper Methods

    private void SetupEmptyDatabase()
    {
        SetupDatabase([], [], []);
    }

    private void SetupDatabase(
        List<Article> articles,
        List<Package> packages,
        List<PackageItem> packageItems)
    {
        var articlesDbSet = articles.BuildMockDbSet();
        var packagesDbSet = packages.BuildMockDbSet();
        var packageItemsDbSet = packageItems.BuildMockDbSet();

        _mockContext.Setup(c => c.Articles).Returns(articlesDbSet.Object);
        _mockContext.Setup(c => c.Packages).Returns(packagesDbSet.Object);
        _mockContext.Setup(c => c.PackageItems).Returns(packageItemsDbSet.Object);
    }

    private Article CreateArticle(
        string articleNumber,
        string identity,
        string methodName,
        ArticleType articleType)
    {
        return new Article
        {
            Id = _fixture.Create<int>(),
            ArticleNumber = articleNumber,
            Identity = identity,
            MethodName = methodName,
            ArticleType = articleType,
            FullName = $"Article {identity}",
            Quantity = 1
        };
    }

    private Package CreatePackage(int id, int intendedMarketId)
    {
        return new Package
        {
            Id = id,
            PackageBarcode = _fixture.Create<string>()[..3],
            OrderingNumber = _fixture.Create<string>()[..15],
            PackageFullName = $"Package {id}",
            IntendedMarketId = intendedMarketId,
            IntendedMarket = new IntendedMarket
            {
                Id = intendedMarketId,
                Name = intendedMarketId == IntendedMarket.Worldwide ? "Worldwide" : "Japan"
            }
        };
    }

    private PackageItem CreatePackageItem(
        int articleId,
        int packageId,
        Article article,
        Package package)
    {
        return new PackageItem
        {
            Id = _fixture.Create<int>(),
            ArticleId = articleId,
            PackageId = packageId,
            ArticleAmount = 1,
            Article = article,
            Package = package
        };
    }

    #endregion
}
