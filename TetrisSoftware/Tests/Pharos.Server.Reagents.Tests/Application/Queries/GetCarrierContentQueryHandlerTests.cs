using MockQueryable.Moq;
using Pharos.Server.Application.Reagents;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Parameters;
using Pharos.Server.Reagents.Application.Interfaces;
using Pharos.Server.Reagents.Application.Queries.Inventory;
using Pharos.Server.Reagents.Domain;

namespace Pharos.Server.Reagents.Application.Queries;

public class GetCarrierContentQueryHandlerTests
{
    private const string _testReagentBarcode = "REG123";
    private const string _testCarrierUdi = "UDI456";
    private const string _testArticleNumber = "ART001";
    private const string _testLotNumber = "LOT001";
    private const string _testSerialNumber = "SER001";

    [Fact]
    public async Task Handle_ReturnsNull_WhenReagentInfoIsNull()
    {
        // Arrange
        var dbContext = new Mock<IReagentsDbContext>();
        var barcodeService = new Mock<IReagentBarcodeService>();
        
        barcodeService
            .Setup(x => x.DetectArticleType(It.IsAny<string>()))
            .ReturnsAsync((ArticleType.ImmunoCapCarrier, null));
        barcodeService
            .Setup(x => x.GetReagentInfo(It.IsAny<ArticleType>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync((ReagentInfo?)null);

        var handler = CreateHandler(dbContext.Object, barcodeService.Object);
        var query = new GetCarrierContentQuery(_testReagentBarcode, _testCarrierUdi);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ReturnsReagentContentDto_WhenReagentInfoExists()
    {
        // Arrange
        var reagentInfo = CreateReagentInfo(ArticleType.ConjugateCartridge, 100);
        var reagents = new List<Reagent>().BuildMockDbSet();
        
        var dbContext = new Mock<IReagentsDbContext>();
        dbContext.Setup(x => x.Reagents).Returns(reagents.Object);
        
        var barcodeService = SetupBarcodeService(reagentInfo);

        var handler = CreateHandler(dbContext.Object, barcodeService.Object);
        var query = new GetCarrierContentQuery(_testReagentBarcode, _testCarrierUdi);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result!.ReagentBarcode.Should().Be(_testReagentBarcode);
        result.Name.Should().Be(reagentInfo.Article.Identity);
        result.ArticleType.Should().Be(ArticleType.ConjugateCartridge);
        result.ArticleNumber.Should().Be(_testArticleNumber);
        result.LotNumber.Should().Be(_testLotNumber);
        result.LotExpiryDate.Should().Be(reagentInfo.Lot.ExpiryDate);
        result.SerialNumber.Should().Be(_testSerialNumber);
    }

    [Fact]
    public async Task Handle_UsesArticleQuantity_WhenReagentDoesNotExistInDb()
    {
        // Arrange
        var articleQuantity = 150;
        var reagentInfo = CreateReagentInfo(ArticleType.ConjugateCartridge, articleQuantity);
        var reagents = new List<Reagent>().BuildMockDbSet();
        
        var dbContext = new Mock<IReagentsDbContext>();
        dbContext.Setup(x => x.Reagents).Returns(reagents.Object);
        
        var barcodeService = SetupBarcodeService(reagentInfo);

        var handler = CreateHandler(dbContext.Object, barcodeService.Object);
        var query = new GetCarrierContentQuery(_testReagentBarcode, _testCarrierUdi);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result!.ContentCells.Should().HaveCount(1);
        result.ContentCells[0].RemainingQuantity.Should().Be(articleQuantity);
        result.ContentCells[0].UseRemainingQuantity.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_UsesDbQuantity_WhenReagentExistsInDb()
    {
        // Arrange
        var articleQuantity = 150;
        var dbQuantity = 75;
        var reagentInfo = CreateReagentInfo(ArticleType.ConjugateCartridge, articleQuantity);
        var dbReagent = CreateDbReagent(_testReagentBarcode, _testCarrierUdi, dbQuantity);
        var reagents = new List<Reagent> { dbReagent }.BuildMockDbSet();
        
        var dbContext = new Mock<IReagentsDbContext>();
        dbContext.Setup(x => x.Reagents).Returns(reagents.Object);
        
        var barcodeService = SetupBarcodeService(reagentInfo);

        var handler = CreateHandler(dbContext.Object, barcodeService.Object);
        var query = new GetCarrierContentQuery(_testReagentBarcode, _testCarrierUdi);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result!.ContentCells.Should().HaveCount(1);
        result.ContentCells[0].RemainingQuantity.Should().Be(dbQuantity);
        result.ContentCells[0].UseRemainingQuantity.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_UsesReagentBarcodeForDetection_WhenReagentBarcodeProvided()
    {
        // Arrange
        var reagentInfo = CreateReagentInfo(ArticleType.ConjugateCartridge, 100);
        var reagents = new List<Reagent>().BuildMockDbSet();
        
        var dbContext = new Mock<IReagentsDbContext>();
        dbContext.Setup(x => x.Reagents).Returns(reagents.Object);
        
        var barcodeService = new Mock<IReagentBarcodeService>();
        barcodeService
            .Setup(x => x.DetectArticleType(_testReagentBarcode))
            .ReturnsAsync((ArticleType.ConjugateCartridge, null));
        barcodeService
            .Setup(x => x.GetReagentInfo(ArticleType.ConjugateCartridge, _testReagentBarcode, _testCarrierUdi))
            .ReturnsAsync(reagentInfo);

        var handler = CreateHandler(dbContext.Object, barcodeService.Object);
        var query = new GetCarrierContentQuery(_testReagentBarcode, _testCarrierUdi);

        // Act
        await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        barcodeService.Verify(x => x.DetectArticleType(_testReagentBarcode), Times.Once);
        barcodeService.Verify(x => x.GetReagentInfo(ArticleType.ConjugateCartridge, _testReagentBarcode, _testCarrierUdi), Times.Once);
    }

    [Fact]
    public async Task Handle_UsesCarrierUdiForDetection_WhenReagentBarcodeIsEmpty()
    {
        // Arrange
        var reagentInfo = CreateReagentInfo(ArticleType.ConjugateCartridge, 100);
        var reagents = new List<Reagent>().BuildMockDbSet();
        
        var dbContext = new Mock<IReagentsDbContext>();
        dbContext.Setup(x => x.Reagents).Returns(reagents.Object);
        
        var barcodeService = new Mock<IReagentBarcodeService>();
        barcodeService
            .Setup(x => x.DetectArticleType(_testCarrierUdi))
            .ReturnsAsync((ArticleType.ConjugateCartridge, null));
        barcodeService
            .Setup(x => x.GetReagentInfo(ArticleType.ConjugateCartridge, string.Empty, _testCarrierUdi))
            .ReturnsAsync(reagentInfo);

        var handler = CreateHandler(dbContext.Object, barcodeService.Object);
        var query = new GetCarrierContentQuery(string.Empty, _testCarrierUdi);

        // Act
        await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        barcodeService.Verify(x => x.DetectArticleType(_testCarrierUdi), Times.Once);
    }

    [Theory]
    [InlineData(ArticleType.ConjugateCartridge)]
    [InlineData(ArticleType.DevelopmentSolutionCartridge)]
    [InlineData(ArticleType.StopSolutionBottle)]
    public async Task Handle_WorksWithDifferentArticleTypes(ArticleType articleType)
    {
        // Arrange
        var reagentInfo = CreateReagentInfo(articleType, 100);
        var reagents = new List<Reagent>().BuildMockDbSet();
        
        var dbContext = new Mock<IReagentsDbContext>();
        dbContext.Setup(x => x.Reagents).Returns(reagents.Object);
        
        var barcodeService = SetupBarcodeService(reagentInfo);

        var handler = CreateHandler(dbContext.Object, barcodeService.Object);
        var query = new GetCarrierContentQuery(_testReagentBarcode, _testCarrierUdi);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result!.ArticleType.Should().Be(articleType);
    }

    private static GetCarrierContentQueryHandler CreateHandler(
        IReagentsDbContext dbContext,
        IReagentBarcodeService barcodeService)
    {
        return new GetCarrierContentQueryHandler(dbContext, barcodeService);
    }

    private static ReagentInfo CreateReagentInfo(ArticleType articleType, int quantity)
    {
        var article = new Article
        {
            ArticleType = articleType,
            ArticleNumber = _testArticleNumber,
            Identity = "Test Reagent",
            Quantity = quantity
        };

        return new ReagentInfo
        {
            Article = article,
            Lot = new LotInformation
            {
                LotNumber = _testLotNumber,
                ExpiryDate = DateTimeOffset.UtcNow.AddMonths(6)
            },
            SerialNumber = _testSerialNumber
        };
    }

    private static Mock<IReagentBarcodeService> SetupBarcodeService(ReagentInfo reagentInfo)
    {
        var barcodeService = new Mock<IReagentBarcodeService>();
        
        barcodeService
            .Setup(x => x.DetectArticleType(It.IsAny<string>()))
            .ReturnsAsync((reagentInfo.Article.ArticleType, null));
        barcodeService
            .Setup(x => x.GetReagentInfo(It.IsAny<ArticleType>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(reagentInfo);

        return barcodeService;
    }

    private static Reagent CreateDbReagent(string barcode, string udi, int quantity)
    {
        return new Reagent
        {
            Barcode = barcode,
            Udi = udi,
            ReagentContents = new List<ReagentContent>
            {
                new() { Quantity = quantity }
            }
        };
    }
}
