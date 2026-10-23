using MediatR;
using Moq;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Reagents;
using Pharos.Server.Application.Sample;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Instrument.Application.Queries.GetBarcodeDetails;

namespace Pharos.Server.Instrument.Application.Queries;

public class GetBarcodeDetailsQueryHandlerTests
{
    private readonly Mock<IReagentBarcodeService> _reagentBarcodeServiceMock;
    private readonly Mock<IInstrumentManager> _instrumentServiceMock;
    private readonly Mock<ICurrentInstrumentServerHub> _hubMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly GetBarcodeDetailsQueryHandler _handler;

    private const string _testInstrumentNumber = "S001";
    private const string _testBarcode = "TEST123";
    private const int _testInstrumentId = 1;

    public GetBarcodeDetailsQueryHandlerTests()
    {
        _reagentBarcodeServiceMock = new Mock<IReagentBarcodeService>();
        _instrumentServiceMock = new Mock<IInstrumentManager>();
        _hubMock = new Mock<ICurrentInstrumentServerHub>();
        _mediatorMock = new Mock<IMediator>();

        _handler = new GetBarcodeDetailsQueryHandler(
            _reagentBarcodeServiceMock.Object,
            _instrumentServiceMock.Object,
            _hubMock.Object,
            _mediatorMock.Object);
    }

    #region Carrier Barcode Tests

    [Theory]
    [InlineData(ArticleType.ImmunoCapCarrier, BarcodeFormat.Primary, true)]
    [InlineData(ArticleType.EliaWellCarrier, BarcodeFormat.Primary, true)]
    [InlineData(ArticleType.EliaCalibratorWellCarrier, BarcodeFormat.Primary, true)]
    [InlineData(ArticleType.ImmunoCapCarrier, BarcodeFormat.Secondary, false)]
    [InlineData(ArticleType.EliaWellCarrier, BarcodeFormat.Secondary, false)]
    public async Task Handle_ShouldReturnCarrierType_ForAllCarrierArticleTypes(
        ArticleType articleType, BarcodeFormat format, bool expectedIsPrimary)
    {
        // Arrange
        var query = new GetBarcodeDetailsQuery(_testInstrumentNumber, _testBarcode);
        _reagentBarcodeServiceMock
            .Setup(x => x.DetectArticleType(_testBarcode))
            .ReturnsAsync((articleType, format));

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result!.Barcode.Should().Be(_testBarcode);
        result.Type.Should().Be(BarcodeType.Carrier);
        result.IsPrimaryBarcode.Should().Be(expectedIsPrimary);
        result.RequestId.Should().Be(0);
    }

    #endregion

    #region Cartridge Barcode Tests

    [Theory]
    [InlineData(ArticleType.CalibratorCartridge)]
    [InlineData(ArticleType.ConjugateCartridge)]
    [InlineData(ArticleType.CurveControlCartridge)]
    [InlineData(ArticleType.DevelopmentSolutionCartridge)]
    [InlineData(ArticleType.DiluentCartridge)]
    [InlineData(ArticleType.FluoroCVial)]
    public async Task Handle_ShouldReturnCartridgeType_ForAllCartridgeArticleTypes(
        ArticleType articleType)
    {
        // Arrange
        var query = new GetBarcodeDetailsQuery(_testInstrumentNumber, _testBarcode);
        _reagentBarcodeServiceMock
            .Setup(x => x.DetectArticleType(_testBarcode))
            .ReturnsAsync((articleType, BarcodeFormat.Primary));

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result!.Type.Should().Be(BarcodeType.Cartridge);
        result.IsPrimaryBarcode.Should().BeTrue();
    }

    [Theory]
    [InlineData(BarcodeFormat.Primary, true)]
    [InlineData(BarcodeFormat.Secondary, false)]
    public async Task Handle_ShouldSetCorrectIsPrimaryFlag_ForCartridgeBarcodes(
        BarcodeFormat format, bool expectedIsPrimary)
    {
        // Arrange
        var query = new GetBarcodeDetailsQuery(_testInstrumentNumber, _testBarcode);
        _reagentBarcodeServiceMock
            .Setup(x => x.DetectArticleType(_testBarcode))
            .ReturnsAsync((ArticleType.CalibratorCartridge, format));

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result!.IsPrimaryBarcode.Should().Be(expectedIsPrimary);
    }

    #endregion

    #region Quality Control Tests

    [Theory]
    [InlineData(BarcodeFormat.Primary, true)]
    [InlineData(BarcodeFormat.Secondary, false)]
    public async Task Handle_ShouldReturnQualityControlType_WithCorrectPrimaryFlag(
        BarcodeFormat format, bool expectedIsPrimary)
    {
        // Arrange
        // Valid QC data matrix format with empty payload: |ArticleID(3)+Lot(2)+Version(1)+CRC(8)
        // CRC for empty string is 00000000
        var qcBarcode = "|QC101100000000";
        var query = new GetBarcodeDetailsQuery(_testInstrumentNumber, qcBarcode);
        _reagentBarcodeServiceMock
            .Setup(x => x.DetectArticleType(qcBarcode))
            .ReturnsAsync((ArticleType.QualityControlCartridge, format));

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result!.Type.Should().Be(BarcodeType.QualityControl);
        result.IsPrimaryBarcode.Should().Be(expectedIsPrimary);
        result.RequestId.Should().Be(0);
    }

    #endregion

    #region Bottle Barcode Tests

    [Theory]
    [InlineData(ArticleType.StopSolutionBottle)]
    [InlineData(ArticleType.WashAdditiveBottle)]
    [InlineData(ArticleType.WashConcentrateBottle)]
    public async Task HandleBottleBarcode_ShouldReturnBottleDto_ForAllBottleTypes(
        ArticleType articleType)
    {
        // Arrange
        var query = new GetBarcodeDetailsQuery(_testInstrumentNumber, _testBarcode);
        _reagentBarcodeServiceMock
            .Setup(x => x.DetectArticleType(_testBarcode))
            .ReturnsAsync((articleType, BarcodeFormat.Primary));
        _instrumentServiceMock
            .Setup(x => x.GetInstrument(_testInstrumentNumber))
            .Returns(CreateInstrument(LockState.ClosedLocked));

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result!.Type.Should().Be(BarcodeType.Bottle);
        result.IsPrimaryBarcode.Should().BeTrue();
    }

    [Theory]
    [InlineData(LockState.ClosedLocked, BarcodeFormat.Primary, true)]
    [InlineData(LockState.ClosedUnlocked, BarcodeFormat.Secondary, false)]
    public async Task HandleBottleBarcode_ShouldReturnBottleDto_WhenDrawerIsNotOpen(
        LockState drawerState, BarcodeFormat format, bool expectedIsPrimary)
    {
        // Arrange
        var query = new GetBarcodeDetailsQuery(_testInstrumentNumber, _testBarcode);
        _reagentBarcodeServiceMock
            .Setup(x => x.DetectArticleType(_testBarcode))
            .ReturnsAsync((ArticleType.StopSolutionBottle, format));
        _instrumentServiceMock
            .Setup(x => x.GetInstrument(_testInstrumentNumber))
            .Returns(CreateInstrument(drawerState));

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result!.Type.Should().Be(BarcodeType.Bottle);
        result.IsPrimaryBarcode.Should().Be(expectedIsPrimary);
    }

    [Fact]
    public async Task HandleBottleLoadingSequence_ShouldSendIncorrectBarcodeNotification_WhenPrimaryBarcodeScanned()
    {
        // Arrange
        var query = new GetBarcodeDetailsQuery(_testInstrumentNumber, _testBarcode);
        _reagentBarcodeServiceMock
            .Setup(x => x.DetectArticleType(_testBarcode))
            .ReturnsAsync((ArticleType.StopSolutionBottle, BarcodeFormat.Primary));
        _instrumentServiceMock
            .Setup(x => x.GetInstrument(_testInstrumentNumber))
            .Returns(CreateInstrument(LockState.Open));
        _instrumentServiceMock
            .Setup(x => x.FindInstrument(_testInstrumentNumber))
            .Returns(CreateInstrument(LockState.Open));

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result!.Type.Should().Be(BarcodeType.Bottle);
        result.IsPrimaryBarcode.Should().BeTrue();
        result.IsBottleDrawerOpen.Should().BeTrue();
        _hubMock.Verify(
            x => x.IncorrectBarcodeScannedForBottles(_testInstrumentId),
            Times.Once);
    }

    [Fact]
    public async Task HandleBottleLoadingSequence_ShouldNotCallReagentService_WhenPrimaryBarcode()
    {
        // Arrange
        var query = new GetBarcodeDetailsQuery(_testInstrumentNumber, _testBarcode);
        _reagentBarcodeServiceMock
            .Setup(x => x.DetectArticleType(_testBarcode))
            .ReturnsAsync((ArticleType.StopSolutionBottle, BarcodeFormat.Primary));
        _instrumentServiceMock
            .Setup(x => x.GetInstrument(_testInstrumentNumber))
            .Returns(CreateInstrument(LockState.Open));
        _instrumentServiceMock
            .Setup(x => x.FindInstrument(_testInstrumentNumber))
            .Returns(CreateInstrument(LockState.Open));

        // Act
        await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        _reagentBarcodeServiceMock.Verify(
            x => x.GetReagentInfo(It.IsAny<ArticleType>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleBottleLoadingSequence_ShouldValidateAndSendNotification_WhenReagentFound()
    {
        // Arrange
        var query = new GetBarcodeDetailsQuery(_testInstrumentNumber, _testBarcode);
        var reagentInfo = new ReagentInfo
        {
            Article = new Server.Domain.Parameters.Article { ArticleNumber = "TEST", ArticleType = ArticleType.StopSolutionBottle },
            Lot = new LotInformation { LotNumber = "LOT123" }
        };

        _reagentBarcodeServiceMock
            .Setup(x => x.DetectArticleType(_testBarcode))
            .ReturnsAsync((ArticleType.StopSolutionBottle, BarcodeFormat.Secondary));
        _instrumentServiceMock
            .Setup(x => x.GetInstrument(_testInstrumentNumber))
            .Returns(CreateInstrument(LockState.Open));
        _instrumentServiceMock
            .Setup(x => x.FindInstrument(_testInstrumentNumber))
            .Returns(CreateInstrument(LockState.Open));
        _reagentBarcodeServiceMock
            .Setup(x => x.GetReagentInfo(ArticleType.StopSolutionBottle, null, _testBarcode))
            .ReturnsAsync(reagentInfo);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result!.Type.Should().Be(BarcodeType.Bottle);
        result.IsBottleDrawerOpen.Should().BeTrue();
        _hubMock.Verify(
            x => x.ReagentBarcodeScanned(_testInstrumentId, _testBarcode),
            Times.Once);
        _reagentBarcodeServiceMock.Verify(
            x => x.GetReagentInfo(ArticleType.StopSolutionBottle, null, _testBarcode),
            Times.Once);
    }

    [Fact]
    public async Task HandleBottleLoadingSequence_ShouldSendIncorrectBarcode_WhenReagentNotFound()
    {
        // Arrange
        var query = new GetBarcodeDetailsQuery(_testInstrumentNumber, _testBarcode);

        _reagentBarcodeServiceMock
            .Setup(x => x.DetectArticleType(_testBarcode))
            .ReturnsAsync((ArticleType.StopSolutionBottle, BarcodeFormat.Secondary));
        _instrumentServiceMock
            .Setup(x => x.GetInstrument(_testInstrumentNumber))
            .Returns(CreateInstrument(LockState.Open));
        _instrumentServiceMock
            .Setup(x => x.FindInstrument(_testInstrumentNumber))
            .Returns(CreateInstrument(LockState.Open));
        _reagentBarcodeServiceMock
            .Setup(x => x.GetReagentInfo(ArticleType.StopSolutionBottle, null, _testBarcode))
            .ReturnsAsync((ReagentInfo?)null);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result!.Type.Should().Be(BarcodeType.Bottle);
        result.IsBottleDrawerOpen.Should().BeTrue();
        _hubMock.Verify(
            x => x.IncorrectBarcodeScannedForBottles(_testInstrumentId),
            Times.Once);
    }

    [Fact]
    public async Task HandleBottleLoadingSequence_ShouldNotSendNotification_WhenInstrumentIdIsNull()
    {
        // Arrange
        var query = new GetBarcodeDetailsQuery(_testInstrumentNumber, _testBarcode);

        _reagentBarcodeServiceMock
            .Setup(x => x.DetectArticleType(_testBarcode))
            .ReturnsAsync((ArticleType.StopSolutionBottle, BarcodeFormat.Primary));
        _instrumentServiceMock
            .Setup(x => x.GetInstrument(_testInstrumentNumber))
            .Returns(CreateInstrument(LockState.Open));
        _instrumentServiceMock
            .Setup(x => x.FindInstrument(_testInstrumentNumber))
            .Returns((IInstrument?)null);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result!.Type.Should().Be(BarcodeType.Bottle);
        result.IsPrimaryBarcode.Should().BeTrue();
        result.IsBottleDrawerOpen.Should().BeTrue();
        _hubMock.Verify(
            x => x.IncorrectBarcodeScannedForBottles(It.IsAny<int>()),
            Times.Never);
    }

    #endregion

    private static Domain.Instrument CreateInstrument(LockState bottledReagentDrawerState)
    {
        var drawerStates = Domain.Instrument.CreateDefaultDrawerLockStates();
        var index = drawerStates.FindIndex(x => x.Drawer == DrawerType.BottledReagent);
        drawerStates[index] = new DrawerLockState(DrawerType.BottledReagent, bottledReagentDrawerState, false);

        return new Domain.Instrument
        {
            Id = _testInstrumentId,
            SerialNumber = _testInstrumentNumber,
            DrawersLockState = drawerStates
        };
    }

    #region Unknown Article Type Tests

    [Theory]
    [InlineData(BarcodeType.Tray, 0)]
    [InlineData(BarcodeType.Sample, 123)]
    [InlineData(BarcodeType.UnknownBarcode, 0)]
    public async Task HandleUnknownArticleType_ShouldReturnCorrectType_BasedOnIdentificationResult(
        BarcodeType expectedType, int expectedRequestId)
    {
        // Arrange
        var query = new GetBarcodeDetailsQuery(_testInstrumentNumber, _testBarcode);
        SetupUnknownArticleType(new SampleBarcodeResult(expectedType, expectedRequestId));

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result!.Type.Should().Be(expectedType);
        result.RequestId.Should().Be(expectedRequestId);
        result.Barcode.Should().Be(_testBarcode);
    }

    [Fact]
    public async Task HandleUnknownArticleType_ShouldForwardMissingRequestEligibility_WhenNoPrefixesAreConfigured()
    {
        // Arrange
        var query = new GetBarcodeDetailsQuery(_testInstrumentNumber, _testBarcode);
        SetupUnknownArticleType(new SampleBarcodeResult(BarcodeType.UnknownBarcode, canQueryMissingRequest: true));

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Type.Should().Be(BarcodeType.UnknownBarcode);
        result.CanQueryMissingRequest.Should().BeTrue();
    }

    [Fact]
    public async Task HandleUnknownArticleType_ShouldForwardMissingRequestEligibility_WhenPrefixMatches()
    {
        // Arrange
        var query = new GetBarcodeDetailsQuery(_testInstrumentNumber, _testBarcode);
        SetupUnknownArticleType(new SampleBarcodeResult(BarcodeType.UnknownBarcode, canQueryMissingRequest: true));

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Type.Should().Be(BarcodeType.UnknownBarcode);
        result.CanQueryMissingRequest.Should().BeTrue();
    }

    [Fact]
    public async Task HandleUnknownArticleType_ShouldNotForwardMissingRequestEligibility_WhenPrefixDoesNotMatch()
    {
        // Arrange
        var query = new GetBarcodeDetailsQuery(_testInstrumentNumber, _testBarcode);
        SetupUnknownArticleType(new SampleBarcodeResult(BarcodeType.UnknownBarcode));

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Type.Should().Be(BarcodeType.UnknownBarcode);
        result.CanQueryMissingRequest.Should().BeFalse();
    }

    [Fact]
    public async Task HandleUnknownArticleType_ShouldReturnTray_WhenCombinedQueryIdentifiesTray()
    {
        // Arrange
        var query = new GetBarcodeDetailsQuery(_testInstrumentNumber, _testBarcode);
        SetupUnknownArticleType(new SampleBarcodeResult(BarcodeType.Tray));

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Type.Should().Be(BarcodeType.Tray);
        _mediatorMock.Verify(
            x => x.Send(It.IsAny<IdentifyTrayOrSampleBarcodeQuery>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Edge Cases

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_ShouldHandleEmptyOrWhitespaceBarcodes_Gracefully(string barcode)
    {
        // Arrange
        var query = new GetBarcodeDetailsQuery(_testInstrumentNumber, barcode);
        _reagentBarcodeServiceMock
            .Setup(x => x.DetectArticleType(barcode))
            .ReturnsAsync((ArticleType.None, BarcodeFormat.Primary));
        _mediatorMock
            .Setup(x => x.Send(It.Is<IdentifyTrayOrSampleBarcodeQuery>(q => q.Barcode == barcode), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SampleBarcodeResult(BarcodeType.UnknownBarcode, 0));

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result!.Type.Should().Be(BarcodeType.UnknownBarcode);
    }

    [Fact]
    public async Task Handle_ShouldPassCancellationToken_ToMediatorSend()
    {
        // Arrange
        var query = new GetBarcodeDetailsQuery(_testInstrumentNumber, _testBarcode);
        var cancellationToken = new CancellationToken();
        _reagentBarcodeServiceMock
            .Setup(x => x.DetectArticleType(_testBarcode))
            .ReturnsAsync((ArticleType.None, BarcodeFormat.Primary));
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<IdentifyTrayOrSampleBarcodeQuery>(), cancellationToken))
            .ReturnsAsync(new SampleBarcodeResult(BarcodeType.UnknownBarcode, 0));

        // Act
        await _handler.Handle(query, cancellationToken);

        // Assert
        _mediatorMock.Verify(
            x => x.Send(It.IsAny<IdentifyTrayOrSampleBarcodeQuery>(), cancellationToken),
            Times.Once);
    }

    #endregion

    private void SetupUnknownArticleType(SampleBarcodeResult result)
    {
        _reagentBarcodeServiceMock
            .Setup(x => x.DetectArticleType(_testBarcode))
            .ReturnsAsync((ArticleType.None, BarcodeFormat.Primary));
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<IdentifyTrayOrSampleBarcodeQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
    }
}