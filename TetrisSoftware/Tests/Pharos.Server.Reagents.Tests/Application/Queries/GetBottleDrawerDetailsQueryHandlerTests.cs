using Google.Protobuf.WellKnownTypes;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Reagents;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Parameters;
using Pharos.Server.Reagents.Application.Interfaces;
using Tetris.Protocol.Common;
using Tetris.Protocol.Pharos.Storage;
using LotInformation = Pharos.Server.Application.Reagents.LotInformation;

namespace Pharos.Server.Reagents.Application.Queries;

public class GetBottleDrawerDetailsQueryHandlerTests
{
    private const string _testInstrumentNumber = "Inst1";
    private const string _stopSolutionBarcode = "|01414141414141411723051510STP01|991234";
    private const string _washAdditiveBarcode = "|01073330660041291722033110WAD02|995678";
    private const string _washConcentrateBarcode = "|01510222223333611724042810WCN03|999012";

    [Fact]
    public async Task Handle_ReturnsBaselinePositions_WhenNoBottles()
    {
        // Arrange
        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetBottles(_testInstrumentNumber))
            .Returns(Array.Empty<BottledReagent>());

        var instrumentService = new Mock<IInstrumentManager>();
        CreateInstrumentMock(instrumentService)
            .Setup(instrument => instrument.DrawersLockState[DrawerType.BottledReagent])
            .Returns(new DrawerLockState(DrawerType.BottledReagent, LockState.ClosedUnlocked, false));

        var handler = CreateHandler(reagentStorage.Object, instrumentService.Object);
        var query = new GetBottleDrawerDetailsQuery(_testInstrumentNumber);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.LockState.Should().Be(LockState.ClosedUnlocked);
        result.Positions.Should().NotBeEmpty();
        result.Positions
            .SelectMany(p => p.Bottles)
            .All(b => b.IsPresent == false)
            .Should().BeTrue();
    }

    [Trait("ProductRequirement", "PR_1362")]
    [Trait("ModuleRequirement", "MR_5052")]
    [Trait("ModuleRequirement", "MR_5054")]
    [Trait("ModuleRequirement", "MR_5055")]
    [Trait("ModuleRequirement", "MR_5063")]
    [Trait("ProductRequirement", "PR_1379")]
    [Trait("ModuleRequirement", "MR_5056")]
    [Trait("ProductRequirement", "PR_1373")]
    [Trait("ModuleRequirement", "MR_5057")]
    [Trait("ModuleRequirement", "MR_5058")]
    [Trait("ProductRequirement", "PR_2113")]
    [Trait("ModuleRequirement", "MR_5060")]
    [Trait("ProductRequirement", "PR_1558")]
    [Trait("ModuleRequirement", "MR_5061")]
    [Theory]
    [InlineData(EnumReagentType.StopSolution, ArticleType.StopSolutionBottle, _stopSolutionBarcode, 1, "Stop Solution", "01")]
    [InlineData(EnumReagentType.WashAdditive, ArticleType.WashAdditiveBottle, _washAdditiveBarcode, 2, "Wash Additive", "02")]
    [InlineData(EnumReagentType.WashConcentrate, ArticleType.WashConcentrateBottle, _washConcentrateBarcode, 2, "Wash ConcentrateA", "03")]
    public async Task Handle_PopulatesBottleDetails_WhenBottlePresent(
        EnumReagentType reagentType,
        ArticleType articleType,
        string barcode,
        int position,
        string friendlyName,
        string lotNumber)
    {
        // Arrange
        var bottle = CreateBottle(barcode, reagentType, position, 750);
        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetBottles(_testInstrumentNumber))
            .Returns(new List<BottledReagent> { bottle });

        var barcodeService = new Mock<IReagentBarcodeService>();
        barcodeService
            .Setup(x => x.GetReagentInfo(articleType, It.IsAny<string?>(), barcode))
            .ReturnsAsync(CreateReagentInfo(articleType, friendlyName, lotNumber));

        var handler = CreateHandler(reagentStorage.Object, barcodeService: barcodeService.Object);
        var query = new GetBottleDrawerDetailsQuery(_testInstrumentNumber);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        var matchedPosition = result.Positions.FirstOrDefault(p => p.ArticleType == articleType);
        matchedPosition.Should().NotBeNull($"Expected a position for {articleType}");

        var dto = matchedPosition!.Bottles.FirstOrDefault(b => b.Position == position);
        dto.Should().NotBeNull($"Expected a bottle detail at position {position}");

        dto!.IsPresent.Should().BeTrue();
        dto.Name.Should().Be(friendlyName);
        dto.LotNumber.Should().Be(lotNumber);
        dto.ReagentUdi.Should().Be(barcode);
        dto.Quantity.Should().BeGreaterThan(0);
        dto.UnitOfMeasure.Should().Be(ArticleQuantityUnit.Milliliters);
        dto.ExpiryDate.Should().NotBeNull();
    }

    [Trait("ProductRequirement", "PR_2180")]
    [Trait("ModuleRequirement", "MR_5059")]
    [Trait("ModuleRequirement", "MR_5076")]
    [Theory]
    [InlineData(EnumBottledReagentError.VolumeReadError, ReagentPositionStatus.UndetectedVolume)]
    [InlineData(EnumBottledReagentError.CannotPumpLiquid, ReagentPositionStatus.DisconnectedBottle)]
    [InlineData(EnumBottledReagentError.WrongVolume, ReagentPositionStatus.UnexpectedVolume)]
    [InlineData(EnumBottledReagentError.BottleUnknown, ReagentPositionStatus.UnknownBottle)]
    [InlineData(EnumBottledReagentError.BottleExpired, ReagentPositionStatus.Expired)]
    public async Task Handle_MapsBottleErrorToStatus_Correctly(
        EnumBottledReagentError bottleError,
        ReagentPositionStatus expectedStatus)
    {
        // Arrange
        var position = 1;
        var bottle = CreateBottle(
            _stopSolutionBarcode,
            EnumReagentType.StopSolution,
            position,
            remainingQuantity: 500,
            error: bottleError);

        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetBottles(_testInstrumentNumber))
            .Returns(new List<BottledReagent> { bottle });

        var barcodeService = new Mock<IReagentBarcodeService>();
        barcodeService
            .Setup(x => x.GetReagentInfo(ArticleType.StopSolutionBottle, It.IsAny<string?>(), _stopSolutionBarcode))
            .ReturnsAsync(CreateReagentInfo(ArticleType.StopSolutionBottle, "Stop Solution", "01"));

        var handler = CreateHandler(reagentStorage.Object, barcodeService: barcodeService.Object);
        var query = new GetBottleDrawerDetailsQuery(_testInstrumentNumber);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        var matchedPosition = result.Positions.FirstOrDefault(p => p.ArticleType == ArticleType.StopSolutionBottle);
        matchedPosition.Should().NotBeNull();

        var dto = matchedPosition!.Bottles.FirstOrDefault(b => b.Position == position);
        dto.Should().NotBeNull();

        dto!.IsPresent.Should().BeTrue();
        dto.Status.Should().Be(expectedStatus);
        dto.Name.Should().Be("Stop Solution");
        dto.ReagentUdi.Should().Be(_stopSolutionBarcode);
    }

    [Trait("ProductRequirement", "PR_1362")]
    [Trait("ModuleRequirement", "MR_5075")]
    [Trait("ProductRequirement", "PR_1373")]
    [Trait("ModuleRequirement", "MR_5074")]
    [Trait("ProductRequirement", "PR_2180")]
    [Trait("ModuleRequirement", "MR_5059")]
    [Theory]
    [InlineData(0, false, 0, 0, ReagentPositionStatus.Empty)] // Empty bottle
    [InlineData(100, true, 0, 0, ReagentPositionStatus.InUse)] // In use
    [InlineData(100, false, 50, 0, ReagentPositionStatus.Reserved)] // Reserved (started)
    [InlineData(100, false, 0, 30, ReagentPositionStatus.Reserved)] // Reserved (scheduled)
    [InlineData(100, false, 25, 25, ReagentPositionStatus.Reserved)] // Reserved (both)
    [InlineData(500, false, 0, 0, ReagentPositionStatus.Available)] // Available
    public async Task Handle_DeterminesBottleStatus_BasedOnState(
        int remainingQuantity,
        bool inUse,
        int reservedStarted,
        int reservedScheduled,
        ReagentPositionStatus expectedStatus)
    {
        // Arrange
        var position = 1;
        var bottle = CreateBottle(
            _stopSolutionBarcode,
            EnumReagentType.StopSolution,
            position,
            remainingQuantity,
            error: EnumBottledReagentError.UnspecifiedBottledReagentError,
            inUse: inUse,
            reservedStarted: reservedStarted,
            reservedScheduled: reservedScheduled);

        var reagentStorage = new Mock<IInstrumentReagentStorage>();
        reagentStorage
            .Setup(x => x.GetBottles(_testInstrumentNumber))
            .Returns(new List<BottledReagent> { bottle });

        var barcodeService = new Mock<IReagentBarcodeService>();
        barcodeService
            .Setup(x => x.GetReagentInfo(ArticleType.StopSolutionBottle, It.IsAny<string?>(), _stopSolutionBarcode))
            .ReturnsAsync(CreateReagentInfo(ArticleType.StopSolutionBottle, "Stop Solution", "01"));

        var handler = CreateHandler(reagentStorage.Object, barcodeService: barcodeService.Object);
        var query = new GetBottleDrawerDetailsQuery(_testInstrumentNumber);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        var matchedPosition = result.Positions.FirstOrDefault(p => p.ArticleType == ArticleType.StopSolutionBottle);
        matchedPosition.Should().NotBeNull();

        var dto = matchedPosition!.Bottles.FirstOrDefault(b => b.Position == position);
        dto.Should().NotBeNull();

        dto!.Status.Should().Be(expectedStatus);
        dto.IsPresent.Should().BeTrue();
        dto.Quantity.Should().Be(remainingQuantity);
        dto.InUse.Should().Be(inUse);
    }

    private static GetBottleDrawerDetailsQueryHandler CreateHandler(
        IInstrumentReagentStorage reagentStorage,
        IInstrumentManager? instrumentService = null,
        IReagentBarcodeService? barcodeService = null)
    {
        if (instrumentService is null)
        {
            var instrumentServiceMock = new Mock<IInstrumentManager>();
            CreateInstrumentMock(instrumentServiceMock)
                .Setup(instrument => instrument.DrawersLockState[DrawerType.BottledReagent])
                .Returns(new DrawerLockState(DrawerType.BottledReagent, LockState.ClosedUnlocked, false));
            instrumentService = instrumentServiceMock.Object;
        }

        var mockBarcodeService = barcodeService ?? new Mock<IReagentBarcodeService>().Object;
        var dbContext = new Mock<IReagentsDbContext>();

        return new GetBottleDrawerDetailsQueryHandler(
            reagentStorage,
            mockBarcodeService,
            instrumentService,
            dbContext.Object);
    }

    private static Mock<IInstrument> CreateInstrumentMock(Mock<IInstrumentManager> instrumentService)
    {
        var instrumentMock = new Mock<IInstrument>();
        instrumentService
            .Setup(x => x.GetInstrument(_testInstrumentNumber))
            .Returns(instrumentMock.Object);

        return instrumentMock;
    }

    private static BottledReagent CreateBottle(
        string udi,
        EnumReagentType type,
        int position,
        int remainingQuantity,
        EnumBottledReagentError error = EnumBottledReagentError.UnspecifiedBottledReagentError,
        bool inUse = false,
        int reservedStarted = 0,
        int reservedScheduled = 0,
        DateTime? expiryDate = null)
    {
        return new BottledReagent
        {
            ReagentUdiBarcode = udi,
            ReagentType = type,
            Position = position,
            Content = new ReagentContent
            {
                RemainingQuantity = remainingQuantity,
                ReservedStartedQuantity = reservedStarted,
                ReservedScheduledQuantity = reservedScheduled,
                ExpiryDate = Timestamp.FromDateTime((expiryDate ?? DateTime.UtcNow.AddDays(10)).ToUniversalTime())
            },
            Error = error,
            InUse = inUse
        };
    }

    private static ReagentInfo CreateReagentInfo(
        ArticleType articleType,
        string friendlyName,
        string lotNumber = "LOT1",
        DateTimeOffset? expiry = null)
    {
        var article = new Article
        {
            ArticleType = articleType,
            FriendlyName = friendlyName
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

