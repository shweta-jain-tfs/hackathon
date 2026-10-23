using MockQueryable.Moq;
using Pharos.Server.Application.Reagents;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Parameters;
using Pharos.Server.Reagents.Application.Interfaces;
using Pharos.Server.Reagents.Application.Queries.Inventory;
using Pharos.Server.Reagents.Domain;

namespace Pharos.Server.Reagents.Application.Queries;

public class GetCartridgeContentQueryHandlerTests
{
    private const string _testBarcode1 = "CART001";
    private const string _testBarcode2 = "CART002";
    private const string _testBarcode3 = "CART003";
    private const string _testArticleNumber = "ART001";
    private const string _testLotNumber = "LOT001";

    [Fact]
    public async Task Handle_ReturnsEmptyDictionary_WhenNoBarcodes()
    {
        // Arrange
        var dbContext = new Mock<IReagentsDbContext>();
        var reagents = new List<Reagent>().BuildMockDbSet();
        dbContext.Setup(x => x.Reagents).Returns(reagents.Object);

        var barcodeService = new Mock<IReagentBarcodeService>();

        var handler = CreateHandler(dbContext.Object, barcodeService.Object);
        var query = new GetCartridgeContentsQuery([]);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ReturnsCartridgeFromDb_WhenCartridgeExists()
    {
        // Arrange
        var dbReagent = CreateDbReagent(_testBarcode1, _testLotNumber, withContents: true);
        var reagents = new List<Reagent> { dbReagent }.BuildMockDbSet();

        var dbContext = new Mock<IReagentsDbContext>();
        dbContext.Setup(x => x.Reagents).Returns(reagents.Object);

        var reagentInfo = CreateReagentInfo(ArticleType.ConjugateCartridge, 96, 6);
        var barcodeService = SetupBarcodeService(_testBarcode1, reagentInfo);

        var handler = CreateHandler(dbContext.Object, barcodeService.Object);
        var query = new GetCartridgeContentsQuery([_testBarcode1]);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().ContainKey(_testBarcode1);
        var cartridge = result[_testBarcode1];
        cartridge.ReagentBarcode.Should().Be(_testBarcode1);
        cartridge.ArticleNumber.Should().Be(_testArticleNumber);
        cartridge.ArticleType.Should().Be(ArticleType.ConjugateCartridge);
        cartridge.LotNumber.Should().Be(_testLotNumber);
        cartridge.ContentCells.Should().HaveCount(6);
        cartridge.ContentCells.Should().AllSatisfy(c => c.UseRemainingQuantity.Should().BeTrue());
    }

    [Fact]
    public async Task Handle_ReturnsNewCartridge_WhenCartridgeDoesNotExistInDb()
    {
        // Arrange
        var reagents = new List<Reagent>().BuildMockDbSet();
        var dbContext = new Mock<IReagentsDbContext>();
        dbContext.Setup(x => x.Reagents).Returns(reagents.Object);

        var reagentInfo = CreateReagentInfo(ArticleType.ConjugateCartridge, 96, 6);
        var barcodeService = SetupBarcodeService(_testBarcode1, reagentInfo);

        var handler = CreateHandler(dbContext.Object, barcodeService.Object);
        var query = new GetCartridgeContentsQuery([_testBarcode1]);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().ContainKey(_testBarcode1);
        var cartridge = result[_testBarcode1];
        cartridge.ReagentBarcode.Should().Be(_testBarcode1);
        cartridge.ArticleType.Should().Be(ArticleType.ConjugateCartridge);
        cartridge.ContentCells.Should().HaveCount(6);
        cartridge.ContentCells.Should().AllSatisfy(c =>
        {
            c.RemainingQuantity.Should().Be(96);
            c.UseRemainingQuantity.Should().BeTrue();
        });
    }

    [Fact]
    public async Task Handle_CreatesMultipleCells_WhenArticleHasNumberOfCells()
    {
        // Arrange
        var numberOfCells = 6;
        var reagents = new List<Reagent>().BuildMockDbSet();
        var dbContext = new Mock<IReagentsDbContext>();
        dbContext.Setup(x => x.Reagents).Returns(reagents.Object);

        var reagentInfo = CreateReagentInfo(ArticleType.ConjugateCartridge, 96, numberOfCells);
        var barcodeService = SetupBarcodeService(_testBarcode1, reagentInfo);

        var handler = CreateHandler(dbContext.Object, barcodeService.Object);
        var query = new GetCartridgeContentsQuery([_testBarcode1]);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        var cartridge = result[_testBarcode1];
        cartridge.ContentCells.Should().HaveCount(numberOfCells);
        for (var i = 0; i < numberOfCells; i++)
        {
            cartridge.ContentCells[i].Position.Should().Be(i + 1);
            cartridge.ContentCells[i].RemainingQuantity.Should().Be(96);
        }
    }

    [Fact]
    public async Task Handle_CreatesSingleCellWithDefaultPosition_WhenArticleHasNoNumberOfCells()
    {
        // Arrange
        var reagents = new List<Reagent>().BuildMockDbSet();
        var dbContext = new Mock<IReagentsDbContext>();
        dbContext.Setup(x => x.Reagents).Returns(reagents.Object);

        var reagentInfo = CreateReagentInfo(ArticleType.StopSolutionBottle, 100, numberOfCells: null);
        var barcodeService = SetupBarcodeService(_testBarcode1, reagentInfo);

        var handler = CreateHandler(dbContext.Object, barcodeService.Object);
        var query = new GetCartridgeContentsQuery([_testBarcode1]);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        var cartridge = result[_testBarcode1];
        cartridge.ContentCells.Should().HaveCount(1);
        cartridge.ContentCells[0].Position.Should().Be(0);
        cartridge.ContentCells[0].RemainingQuantity.Should().Be(100);
    }

    [Fact]
    public async Task Handle_ReturnsMultipleCartridges_WhenMixOfExistingAndNewBarcodes()
    {
        // Arrange
        var dbReagent1 = CreateDbReagent(_testBarcode1, _testLotNumber, withContents: true);
        var reagents = new List<Reagent> { dbReagent1 }.BuildMockDbSet();

        var dbContext = new Mock<IReagentsDbContext>();
        dbContext.Setup(x => x.Reagents).Returns(reagents.Object);

        var reagentInfo1 = CreateReagentInfo(ArticleType.ConjugateCartridge, 96, 6);
        var reagentInfo2 = CreateReagentInfo(ArticleType.DevelopmentSolutionCartridge, 56, 3);

        var barcodeService = new Mock<IReagentBarcodeService>();
        barcodeService
            .Setup(x => x.DetectArticleType(_testBarcode1))
            .ReturnsAsync((ArticleType.ConjugateCartridge, null));
        barcodeService
            .Setup(x => x.GetReagentInfo(ArticleType.ConjugateCartridge, _testBarcode1, null))
            .ReturnsAsync(reagentInfo1);
        barcodeService
            .Setup(x => x.DetectArticleType(_testBarcode2))
            .ReturnsAsync((ArticleType.DevelopmentSolutionCartridge, null));
        barcodeService
            .Setup(x => x.GetReagentInfo(ArticleType.DevelopmentSolutionCartridge, _testBarcode2, null))
            .ReturnsAsync(reagentInfo2);

        var handler = CreateHandler(dbContext.Object, barcodeService.Object);
        var query = new GetCartridgeContentsQuery([_testBarcode1, _testBarcode2]);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().HaveCount(2);
        result.Should().ContainKey(_testBarcode1);
        result.Should().ContainKey(_testBarcode2);

        result[_testBarcode1].ContentCells.Should().HaveCount(6);
        result[_testBarcode2].ContentCells.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_SkipsBarcode_WhenReagentInfoIsNull()
    {
        // Arrange
        var reagents = new List<Reagent>().BuildMockDbSet();
        var dbContext = new Mock<IReagentsDbContext>();
        dbContext.Setup(x => x.Reagents).Returns(reagents.Object);

        var reagentInfo = CreateReagentInfo(ArticleType.ConjugateCartridge, 96, 6);

        var barcodeService = new Mock<IReagentBarcodeService>();
        barcodeService
            .Setup(x => x.DetectArticleType(_testBarcode1))
            .ReturnsAsync((ArticleType.ConjugateCartridge, null));
        barcodeService
            .Setup(x => x.GetReagentInfo(ArticleType.ConjugateCartridge, _testBarcode1, null))
            .ReturnsAsync(reagentInfo);
        barcodeService
            .Setup(x => x.DetectArticleType(_testBarcode2))
            .ReturnsAsync((ArticleType.ImmunoCapCarrier, null));
        barcodeService
            .Setup(x => x.GetReagentInfo(It.IsAny<ArticleType>(), _testBarcode2, null))
            .ReturnsAsync((ReagentInfo?)null);

        var handler = CreateHandler(dbContext.Object, barcodeService.Object);
        var query = new GetCartridgeContentsQuery([_testBarcode1, _testBarcode2]);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().HaveCount(1);
        result.Should().ContainKey(_testBarcode1);
        result.Should().NotContainKey(_testBarcode2);
    }

    [Fact]
    public async Task Handle_EnrichesDbDataWithArticleInfo_WhenCartridgeExistsInDb()
    {
        // Arrange
        var dbReagent = CreateDbReagent(_testBarcode1, _testLotNumber, withContents: true);
        var reagents = new List<Reagent> { dbReagent }.BuildMockDbSet();

        var dbContext = new Mock<IReagentsDbContext>();
        dbContext.Setup(x => x.Reagents).Returns(reagents.Object);

        var reagentInfo = CreateReagentInfo(ArticleType.ConjugateCartridge, 96, 6);
        var barcodeService = SetupBarcodeService(_testBarcode1, reagentInfo);

        var handler = CreateHandler(dbContext.Object, barcodeService.Object);
        var query = new GetCartridgeContentsQuery([_testBarcode1]);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        var cartridge = result[_testBarcode1];
        cartridge.ArticleType.Should().Be(ArticleType.ConjugateCartridge);
        cartridge.ArticleNumber.Should().Be(_testArticleNumber);
    }

    [Fact]
    public async Task Handle_PreservesDbQuantitiesAndTimestamps_WhenCartridgeExistsInDb()
    {
        // Arrange
        var openTimestamp = DateTimeOffset.UtcNow.AddHours(-5);
        var dbReagent = CreateDbReagent(_testBarcode1, _testLotNumber, withContents: true, openTimestamp: openTimestamp);
        var reagents = new List<Reagent> { dbReagent }.BuildMockDbSet();

        var dbContext = new Mock<IReagentsDbContext>();
        dbContext.Setup(x => x.Reagents).Returns(reagents.Object);

        var reagentInfo = CreateReagentInfo(ArticleType.ConjugateCartridge, 96, 6);
        var barcodeService = SetupBarcodeService(_testBarcode1, reagentInfo);

        var handler = CreateHandler(dbContext.Object, barcodeService.Object);
        var query = new GetCartridgeContentsQuery([_testBarcode1]);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        var cartridge = result[_testBarcode1];
        cartridge.ContentCells.Should().AllSatisfy(c =>
        {
            c.RemainingQuantity.Should().BeGreaterThan(0);
            c.OpenTimestamp.Should().Be(openTimestamp);
        });
    }

    [Fact]
    public async Task Handle_SetsUseRemainingQuantityToTrue_ForAllCells()
    {
        // Arrange
        var reagents = new List<Reagent>().BuildMockDbSet();
        var dbContext = new Mock<IReagentsDbContext>();
        dbContext.Setup(x => x.Reagents).Returns(reagents.Object);

        var reagentInfo = CreateReagentInfo(ArticleType.ConjugateCartridge, 96, 6);
        var barcodeService = SetupBarcodeService(_testBarcode1, reagentInfo);

        var handler = CreateHandler(dbContext.Object, barcodeService.Object);
        var query = new GetCartridgeContentsQuery([_testBarcode1]);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        var cartridge = result[_testBarcode1];
        cartridge.ContentCells.Should().AllSatisfy(c => c.UseRemainingQuantity.Should().BeTrue());
    }

    [Theory]
    [InlineData(ArticleType.ConjugateCartridge)]
    [InlineData(ArticleType.DevelopmentSolutionCartridge)]
    [InlineData(ArticleType.StopSolutionBottle)]
    public async Task Handle_WorksWithDifferentArticleTypes(ArticleType articleType)
    {
        // Arrange
        var reagents = new List<Reagent>().BuildMockDbSet();
        var dbContext = new Mock<IReagentsDbContext>();
        dbContext.Setup(x => x.Reagents).Returns(reagents.Object);

        var reagentInfo = CreateReagentInfo(articleType, 100, 3);
        var barcodeService = SetupBarcodeService(_testBarcode1, reagentInfo);

        var handler = CreateHandler(dbContext.Object, barcodeService.Object);
        var query = new GetCartridgeContentsQuery([_testBarcode1]);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result[_testBarcode1].ArticleType.Should().Be(articleType);
    }

    [Fact]
    public async Task Handle_MaintainsBarcodeOrder_InResultDictionary()
    {
        // Arrange
        var reagents = new List<Reagent>().BuildMockDbSet();
        var dbContext = new Mock<IReagentsDbContext>();
        dbContext.Setup(x => x.Reagents).Returns(reagents.Object);

        var barcodeService = new Mock<IReagentBarcodeService>();
        foreach (var barcode in new[] { _testBarcode1, _testBarcode2, _testBarcode3 })
        {
            var reagentInfo = CreateReagentInfo(ArticleType.ConjugateCartridge, 96, 6);
            barcodeService
                .Setup(x => x.DetectArticleType(barcode))
                .ReturnsAsync((ArticleType.ConjugateCartridge, null));
            barcodeService
                .Setup(x => x.GetReagentInfo(ArticleType.ConjugateCartridge, barcode, null))
                .ReturnsAsync(reagentInfo);
        }

        var handler = CreateHandler(dbContext.Object, barcodeService.Object);
        var query = new GetCartridgeContentsQuery([_testBarcode1, _testBarcode2, _testBarcode3]);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().HaveCount(3);
        result.Keys.Should().Contain(new[] { _testBarcode1, _testBarcode2, _testBarcode3 });
    }

    private static GetCartridgeContentQueryHandler CreateHandler(
        IReagentsDbContext dbContext,
        IReagentBarcodeService barcodeService)
    {
        return new GetCartridgeContentQueryHandler(dbContext, barcodeService);
    }

    private static ReagentInfo CreateReagentInfo(ArticleType articleType, int quantity, int? numberOfCells)
    {
        var article = new Article
        {
            ArticleType = articleType,
            ArticleNumber = _testArticleNumber,
            Identity = "Test Reagent",
            Quantity = quantity,
            NumberOfCells = numberOfCells
        };

        return new ReagentInfo
        {
            Article = article,
            Lot = new LotInformation
            {
                LotNumber = _testLotNumber,
                ExpiryDate = DateTimeOffset.UtcNow.AddMonths(6)
            }
        };
    }

    private static Mock<IReagentBarcodeService> SetupBarcodeService(string barcode, ReagentInfo reagentInfo)
    {
        var barcodeService = new Mock<IReagentBarcodeService>();

        barcodeService
            .Setup(x => x.DetectArticleType(barcode))
            .ReturnsAsync((reagentInfo.Article.ArticleType, null));
        barcodeService
            .Setup(x => x.GetReagentInfo(reagentInfo.Article.ArticleType, barcode, null))
            .ReturnsAsync(reagentInfo);

        return barcodeService;
    }

    private static Reagent CreateDbReagent(
        string barcode,
        string lotNumber,
        bool withContents,
        DateTimeOffset? openTimestamp = null)
    {
        var reagent = new Reagent
        {
            Barcode = barcode,
            Lot = new Lot
            {
                LotNumber = lotNumber,
                ExpiryDate = DateTimeOffset.UtcNow.AddMonths(3),
                ArticleType = ArticleType.ConjugateCartridge
            },
            ReagentContents = []
        };

        if (withContents)
        {
            for (var i = 1; i <= 6; i++)
            {
                reagent.ReagentContents.Add(new ReagentContent
                {
                    CellPosition = i,
                    Quantity = 96 - (i * 5),
                    OpenTimestamp = openTimestamp ?? DateTimeOffset.UtcNow.AddHours(-i)
                });
            }
        }

        return reagent;
    }
}
