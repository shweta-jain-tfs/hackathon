using System.Drawing;
using Moq.EntityFrameworkCore;
using Pharos.Server.Application.Reagents;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Parameters;
using Pharos.Server.Reagents.Application.Interfaces;
using Pharos.Server.Reagents.Domain;
using Tetris.Protocol.Common;
using Tetris.Protocol.Pharos.Storage;
using LotInformation = Pharos.Server.Application.Reagents.LotInformation;
using ReagentContent = Pharos.Server.Reagents.Domain.ReagentContent;

namespace Pharos.Server.Reagents.Application.Queries;

public class GetLoadingPositionQueryHandlerTests
{
    private const string _testInstrumentNumber = "Inst1";
    private const string _stopSolutionBarcode = "|01414141414141411723051510STP01|991234";
    private const string _washAdditiveBarcode = "|01073330660041291722033110WAD02|995678";
    private const string _washConcentrateBarcode = "|01510222223333611724042810WCN02|999012";

    [Trait("ProductRequirement", "PR_2253")]
    [Trait("ModuleRequirement", "MR_5072")]
    [Trait("ProductRequirement", "PR_1558")]
    [Trait("ModuleRequirement", "MR_5086")]
    [Theory]
    [InlineData(ArticleType.None, "InvalidBarcode", LoadingPositionStatus.NotFound, 0)]
    [InlineData(ArticleType.StopSolutionBottle, _stopSolutionBarcode, LoadingPositionStatus.NotFound, 0)]
    public async Task Handle_ReturnsNotFound_WhenReagentCannotBeIdentified(
        ArticleType articleType,
        string barcode,
        LoadingPositionStatus expectedStatus,
        int expectedPosition)
    {
        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        var barcodeService = new Mock<IReagentBarcodeService>();
        barcodeService
            .Setup(x => x.DetectArticleType(barcode))
            .ReturnsAsync((articleType, null));

        if (articleType != ArticleType.None)
        {
            barcodeService
                .Setup(x => x.GetReagentInfo(articleType, null, barcode))
                .ReturnsAsync((ReagentInfo?)null);
        }

        var handler = CreateHandler(reagentStorage.Object, barcodeService.Object);
        var query = new GetLoadingPositionQuery(_testInstrumentNumber, barcode);

        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Status.Should().Be(expectedStatus);
        result.Position.Should().Be(expectedPosition);
    }

    [Trait("ProductRequirement", "PR_1370")]
    [Trait("ModuleRequirement", "MR_5064")]
    [Theory]
    [InlineData(ArticleType.StopSolutionBottle, _stopSolutionBarcode, "Stop Solution", "Identity1", 1)]
    [InlineData(ArticleType.WashAdditiveBottle, _washAdditiveBarcode, "Wash Additive", "Identity1", 1)]
    public async Task Handle_FindsFirstPosition_WhenNoBottlesAreLoaded(
        ArticleType articleType,
        string barcode,
        string friendlyName,
        string identity,
        int expectedPosition)
    {
        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetBottles(_testInstrumentNumber))
            .Returns(Array.Empty<BottledReagent>());

        var quantity = articleType == ArticleType.StopSolutionBottle 
            ? Constants.MaxQuantityStopSolution 
            : Constants.MaxQuantityWashAdditive;
        var reagentInfo = CreateReagentInfo(articleType, friendlyName, identity, quantity: quantity);
        var barcodeService = SetupBarcodeService(barcode, articleType, reagentInfo);
        var handler = CreateHandler(reagentStorage.Object, barcodeService.Object, new List<Reagent>());
        var query = new GetLoadingPositionQuery(_testInstrumentNumber, barcode);

        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Status.Should().Be(LoadingPositionStatus.Found);
        result.ArticleType.Should().Be(articleType);
        result.Position.Should().Be(expectedPosition);
        result.Name.Should().Be(friendlyName);
    }

    [Trait("ProductRequirement", "PR_1370")]
    [Trait("ModuleRequirement", "MR_5066")]
    [Theory]
    [InlineData(ArticleType.StopSolutionBottle, EnumReagentType.StopSolution, _stopSolutionBarcode, 2)]
    public async Task Handle_FindsNextAvailablePosition_WhenPositionOccupied(
        ArticleType articleType,
        EnumReagentType reagentType,
        string barcode,
        int expectedPosition)
    {
        var existingBottle = CreateBottle("ExistingUdi", reagentType, 1);
        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetBottles(_testInstrumentNumber))
            .Returns(new List<BottledReagent> { existingBottle });

        var reagentInfo = CreateReagentInfo(articleType, "Stop Solution", "Identity1", quantity: Constants.MaxQuantityStopSolution);
        var barcodeService = SetupBarcodeService(barcode, articleType, reagentInfo);
        var handler = CreateHandler(reagentStorage.Object, barcodeService.Object, new List<Reagent>());
        var query = new GetLoadingPositionQuery(_testInstrumentNumber, barcode);

        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Status.Should().Be(LoadingPositionStatus.Found);
        result.Position.Should().Be(expectedPosition);
    }

    [Trait("ProductRequirement", "PR_2253")]
    [Trait("ModuleRequirement", "MR_5072")]
    [Theory]
    [InlineData(ArticleType.StopSolutionBottle, EnumReagentType.StopSolution, _stopSolutionBarcode)]
    public async Task Handle_ReturnsNotFound_WhenAllPositionsOccupied(
        ArticleType articleType,
        EnumReagentType reagentType,
        string barcode)
    {
        var bottle1 = CreateBottle("Udi1", reagentType, 1);
        var bottle2 = CreateBottle("Udi2", reagentType, 2);
        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetBottles(_testInstrumentNumber))
            .Returns(new List<BottledReagent> { bottle1, bottle2 });

        var reagentInfo = CreateReagentInfo(articleType, "Stop Solution", "Identity1", quantity: Constants.MaxQuantityStopSolution);
        var barcodeService = SetupBarcodeService(barcode, articleType, reagentInfo);
        var handler = CreateHandler(reagentStorage.Object, barcodeService.Object);
        var query = new GetLoadingPositionQuery(_testInstrumentNumber, barcode);

        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Status.Should().Be(LoadingPositionStatus.NotFound);
        result.Position.Should().Be(0);
    }

    [Trait("ProductRequirement", "PR_1377")]
    [Trait("ModuleRequirement", "MR_5065")]
    [Trait("ModuleRequirement", "MR_5068")]
    [Theory]
    [InlineData(ArticleType.WashAdditiveBottle, EnumReagentType.WashAdditive, _washAdditiveBarcode, "WAD_Identity", "WAD_Identity", LoadingPositionStatus.Found, 2)]
    [InlineData(ArticleType.WashAdditiveBottle, EnumReagentType.WashAdditive, _washAdditiveBarcode, "WAD_A", "WAD_B", LoadingPositionStatus.NotValidPair, 0)]
    [InlineData(ArticleType.WashConcentrateBottle, EnumReagentType.WashConcentrate, _washConcentrateBarcode, "WCN_Identity", "WCN_Identity", LoadingPositionStatus.Found, 4)]
    [InlineData(ArticleType.WashConcentrateBottle, EnumReagentType.WashConcentrate, _washConcentrateBarcode, "WCN_A", "WCN_B", LoadingPositionStatus.NotValidPair, 0)]
    public async Task Handle_ValidatesReagentPairCompatibility(
        ArticleType articleType,
        EnumReagentType reagentType,
        string barcode,
        string scannedIdentity,
        string existingIdentity,
        LoadingPositionStatus expectedStatus,
        int expectedPosition)
    {
        var existingBarcode = barcode.Replace("02", "01");
        var existingBottlePosition = articleType == ArticleType.WashConcentrateBottle ? 3 : 1;
        var existingBottle = CreateBottle(existingBarcode, reagentType, existingBottlePosition);
        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetBottles(_testInstrumentNumber))
            .Returns(new List<BottledReagent> { existingBottle });

        var friendlyName = articleType == ArticleType.WashAdditiveBottle ? "Wash Additive" : "Wash Concentrate";
        var quantity = articleType == ArticleType.WashAdditiveBottle 
            ? Constants.MaxQuantityWashAdditive 
            : Constants.MaxQuantityWashConcentrateB;
        var scannedReagentInfo = CreateReagentInfo(articleType, friendlyName, scannedIdentity, quantity: quantity);
        var existingReagentInfo = CreateReagentInfo(articleType, friendlyName, existingIdentity, quantity: quantity);

        var barcodeService = new Mock<IReagentBarcodeService>();
        barcodeService
            .Setup(x => x.DetectArticleType(barcode))
            .ReturnsAsync((articleType, null));
        barcodeService
            .Setup(x => x.GetReagentInfo(articleType, null, barcode))
            .ReturnsAsync(scannedReagentInfo);
        barcodeService
            .Setup(x => x.GetReagentInfo(articleType, null, existingBarcode))
            .ReturnsAsync(existingReagentInfo);

        var handler = CreateHandler(reagentStorage.Object, barcodeService.Object, new List<Reagent>());
        var query = new GetLoadingPositionQuery(_testInstrumentNumber, barcode);

        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Status.Should().Be(expectedStatus);
        result.Position.Should().Be(expectedPosition);
    }

    [Trait("ProductRequirement", "PR_1558")]
    [Trait("ModuleRequirement", "MR_5086")]
    [Theory]
    [InlineData(Constants.MaxQuantityWashConcentrateB, 3)]
    [InlineData(Constants.MaxQuantityWashConcentrateA, 1)]
    public async Task Handle_SelectsCorrectPositionRange_BasedOnBottleSize(
        int quantity,
        int expectedPosition)
    {
        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetBottles(_testInstrumentNumber))
            .Returns(Array.Empty<BottledReagent>());

        var reagentInfo = CreateReagentInfo(ArticleType.WashConcentrateBottle, "Wash Concentrate", "WCN_Identity", quantity: quantity);
        var barcodeService = SetupBarcodeService(_washConcentrateBarcode, ArticleType.WashConcentrateBottle, reagentInfo);
        var handler = CreateHandler(reagentStorage.Object, barcodeService.Object, new List<Reagent>());
        var query = new GetLoadingPositionQuery(_testInstrumentNumber, _washConcentrateBarcode);

        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Status.Should().Be(LoadingPositionStatus.Found);
        result.Position.Should().Be(expectedPosition);
    }

    [Trait("ProductRequirement", "PR_2253")]
    [Trait("ModuleRequirement", "MR_5073")]
    [Trait("ProductRequirement", "PR_1370")]
    [Trait("ModuleRequirement", "MR_5064")]
    [Fact]
    public async Task Handle_PopulatesAllResponseFields_WhenPositionFound()
    {
        var expiryDate = DateTimeOffset.UtcNow.AddMonths(6);
        var color1 = Color.Red;
        var color2 = Color.Blue;

        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetBottles(_testInstrumentNumber))
            .Returns(Array.Empty<BottledReagent>());

        var reagentInfo = CreateReagentInfo(
            ArticleType.StopSolutionBottle,
            "Stop Solution",
            "STP_Identity",
            "LOT456",
            quantity: Constants.MaxQuantityStopSolution,
            expiry: expiryDate,
            color1: color1,
            color2: color2);

        var barcodeService = SetupBarcodeService(_stopSolutionBarcode, ArticleType.StopSolutionBottle, reagentInfo);
        var handler = CreateHandler(reagentStorage.Object, barcodeService.Object, new List<Reagent>());
        var query = new GetLoadingPositionQuery(_testInstrumentNumber, _stopSolutionBarcode);

        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Status.Should().Be(LoadingPositionStatus.Found);
        result.ArticleType.Should().Be(ArticleType.StopSolutionBottle);
        result.Position.Should().Be(1);
        result.Name.Should().Be("Stop Solution");
        result.LotNumber.Should().Be("LOT456");
        result.ExpiryDate.Should().Be(expiryDate.DateTime);
        result.UnitOfMeasure.Should().Be(ArticleQuantityUnit.Milliliters);
        result.Quantity.Should().Be(Constants.MaxQuantityStopSolution);
        result.Colors.Where(c => c != null).Should().HaveCount(2);
        result.Colors[0].Should().Be(ColorTranslator.ToHtml(color1));
        result.Colors[1].Should().Be(ColorTranslator.ToHtml(color2));
        result.Id.Should().Be(HashCode.Combine(ArticleType.StopSolutionBottle, 1));
    }

    [Trait("ProductRequirement", "PR_2253")]
    [Trait("ModuleRequirement", "MR_5073")]
    [Theory]
    [InlineData(750, 750)]
    [InlineData(0, 0)]
    public async Task Handle_UsesCorrectQuantity_FromDatabaseOrDefault(
        int savedQuantity,
        int expectedQuantity)
    {
        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetBottles(_testInstrumentNumber))
            .Returns(Array.Empty<BottledReagent>());

        var reagentInfo = CreateReagentInfo(ArticleType.StopSolutionBottle, "Stop Solution", "STP_Identity", quantity: Constants.MaxQuantityStopSolution);
        var barcodeService = SetupBarcodeService(_stopSolutionBarcode, ArticleType.StopSolutionBottle, reagentInfo);

        var reagent = new Reagent
        {
            Udi = _stopSolutionBarcode,
            ReagentContents = savedQuantity > 0
                ? new List<ReagentContent> { new() { Quantity = savedQuantity } }
                : null
        };

        var handler = CreateHandler(reagentStorage.Object, barcodeService.Object, new List<Reagent> { reagent });
        var query = new GetLoadingPositionQuery(_testInstrumentNumber, _stopSolutionBarcode);

        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Status.Should().Be(LoadingPositionStatus.Found);
        result.Quantity.Should().Be(expectedQuantity);
    }

    [Trait("ProductRequirement", "PR_1377")]
    [Trait("ModuleRequirement", "MR_5065")]
    [Theory]
    [InlineData("")]
    public async Task Handle_AcceptsPairPosition_WhenExistingBottleHasEmptyBarcode(string emptyBarcode)
    {
        var existingBottle = CreateBottle(emptyBarcode, EnumReagentType.WashAdditive, 1);
        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetBottles(_testInstrumentNumber))
            .Returns(new List<BottledReagent> { existingBottle });

        var reagentInfo = CreateReagentInfo(ArticleType.WashAdditiveBottle, "Wash Additive", "WAD_Identity", quantity: Constants.MaxQuantityWashAdditive);
        var barcodeService = SetupBarcodeService(_washAdditiveBarcode, ArticleType.WashAdditiveBottle, reagentInfo);
        var handler = CreateHandler(reagentStorage.Object, barcodeService.Object, new List<Reagent>());
        var query = new GetLoadingPositionQuery(_testInstrumentNumber, _washAdditiveBarcode);

        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Status.Should().Be(LoadingPositionStatus.Found);
        result.Position.Should().Be(2);
    }

    [Trait("ProductRequirement", "PR_1558")]
    [Trait("ModuleRequirement", "MR_5086")]
    [Theory]
    [InlineData(ArticleType.WashAdditiveBottle, Constants.BottleCutoffQuantityForSize + 1)]
    [InlineData(ArticleType.StopSolutionBottle, Constants.BottleCutoffQuantityForSize)]
    public async Task Handle_ReturnsNotFound_WhenInvalidBottleSizeScanned(
        ArticleType articleType,
        int quantity)
    {
        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetBottles(_testInstrumentNumber))
            .Returns(Array.Empty<BottledReagent>());

        var barcode = articleType == ArticleType.WashAdditiveBottle ? _washAdditiveBarcode : _stopSolutionBarcode;
        var friendlyName = articleType == ArticleType.WashAdditiveBottle ? "Wash Additive" : "Stop Solution";
        var reagentInfo = CreateReagentInfo(articleType, friendlyName, "Identity1", quantity: quantity);
        var barcodeService = SetupBarcodeService(barcode, articleType, reagentInfo);
        var handler = CreateHandler(reagentStorage.Object, barcodeService.Object, new List<Reagent>());
        var query = new GetLoadingPositionQuery(_testInstrumentNumber, barcode);

        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Status.Should().Be(LoadingPositionStatus.NotFound);
        result.Position.Should().Be(0);
    }

    [Trait("ProductRequirement", "PR_1558")]
    [Trait("ModuleRequirement", "MR_5086")]
    [Trait("ProductRequirement", "PR_2253")]
    [Trait("ModuleRequirement", "MR_5073")]
    [Theory]
    [InlineData(ArticleType.WashAdditiveBottle, Constants.BottleCutoffQuantityForSize)]
    [InlineData(ArticleType.StopSolutionBottle, Constants.BottleCutoffQuantityForSize + 1)]
    [InlineData(ArticleType.WashConcentrateBottle, Constants.BottleCutoffQuantityForSize)]
    [InlineData(ArticleType.WashConcentrateBottle, Constants.BottleCutoffQuantityForSize + 1)]
    public async Task Handle_ReturnsFound_WhenValidBottleSizeScanned(
        ArticleType articleType,
        int quantity)
    {
        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetBottles(_testInstrumentNumber))
            .Returns(Array.Empty<BottledReagent>());

        var barcode = articleType switch
        {
            ArticleType.WashAdditiveBottle => _washAdditiveBarcode,
            ArticleType.StopSolutionBottle => _stopSolutionBarcode,
            ArticleType.WashConcentrateBottle => _washConcentrateBarcode,
            _ => throw new ArgumentException("Unsupported article type")
        };

        var friendlyName = articleType switch
        {
            ArticleType.WashAdditiveBottle => "Wash Additive",
            ArticleType.StopSolutionBottle => "Stop Solution",
            ArticleType.WashConcentrateBottle => "Wash Concentrate",
            _ => throw new ArgumentException("Unsupported article type")
        };

        var reagentInfo = CreateReagentInfo(articleType, friendlyName, "Identity1", quantity: quantity);
        var barcodeService = SetupBarcodeService(barcode, articleType, reagentInfo);
        var handler = CreateHandler(reagentStorage.Object, barcodeService.Object, new List<Reagent>());
        var query = new GetLoadingPositionQuery(_testInstrumentNumber, barcode);

        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Status.Should().Be(LoadingPositionStatus.Found);
        result.Position.Should().BeGreaterThan(0);
    }

    private static GetLoadingPositionQueryHandler CreateHandler(
        IInstrumentReagentStorage reagentStorage,
        IReagentBarcodeService barcodeService,
        List<Reagent>? reagents = null)
    {
        var dbContext = new Mock<IReagentsDbContext>();
        dbContext.Setup(x => x.Reagents).ReturnsDbSet(reagents ?? new List<Reagent>());
        return new GetLoadingPositionQueryHandler(reagentStorage, barcodeService, dbContext.Object);
    }

    private static Mock<IReagentBarcodeService> SetupBarcodeService(
        string barcode,
        ArticleType articleType,
        ReagentInfo? reagentInfo)
    {
        var barcodeService = new Mock<IReagentBarcodeService>();
        barcodeService
            .Setup(x => x.DetectArticleType(barcode))
            .ReturnsAsync((articleType, null));
        barcodeService
            .Setup(x => x.GetReagentInfo(articleType, null, barcode))
            .ReturnsAsync(reagentInfo);
        return barcodeService;
    }

    private static BottledReagent CreateBottle(string udi, EnumReagentType type, int position)
    {
        return new BottledReagent
        {
            ReagentUdiBarcode = udi,
            ReagentType = type,
            Position = position
        };
    }

    private static ReagentInfo CreateReagentInfo(
        ArticleType articleType,
        string friendlyName,
        string identity,
        string lotNumber = "LOT123",
        int quantity = 0,
        DateTimeOffset? expiry = null,
        Color? color1 = null,
        Color? color2 = null)
    {
        var article = new Server.Domain.Parameters.Article
        {
            ArticleType = articleType,
            FriendlyName = friendlyName,
            Identity = identity,
            Quantity = quantity,
            Color1 = color1,
            Color2 = color2
        };

        return new ReagentInfo
        {
            Article = article,
            Lot = new LotInformation
            {
                LotNumber = lotNumber,
                ExpiryDate = expiry ?? DateTimeOffset.UtcNow.AddDays(30)
            }
        };
    }
}
