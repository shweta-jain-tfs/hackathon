using Google.Protobuf.WellKnownTypes;
using MediatR;
using Pharos.Server.Application.Reagents;
using Pharos.Server.Domain.Enums;
using Tetris.Protocol.Common;
using Tetris.Protocol.Pharos;

namespace Pharos.Server.Reagents.Application.Commands;

public class GetReagentDrawerContentsCommandHandlerTests
{
    private const string _testInstrumentNumber = "INST001";
    private const string _testCartridgeBarcode1 = "CART001";
    private const string _testCartridgeBarcode2 = "CART002";
    private const string _testCarrierOcr1 = "f1";
    private const string _testCarrierOcr2 = "e2";
    private static readonly DateTimeOffset _lotExpiryDate = new(2024, 12, 31, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset _openTimestamp = new(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);

    private readonly Mock<IMediator> _mediatorMock;
    private readonly GetReagentDrawerContentsCommandHandler _sut;

    public GetReagentDrawerContentsCommandHandlerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _sut = new GetReagentDrawerContentsCommandHandler(_mediatorMock.Object);
    }

    [Fact]
    public async Task Handle_ReturnsEmptyResponse_WhenNoCartridgesOrCarriers()
    {
        // Arrange
        var request = new ReagentDrawerContentsRequest();
        var command = new GetReagentDrawerContentsCommand(_testInstrumentNumber, request);

        SetupEmptyCartridges();
        SetupEmptyCarriers();

        // Act
        var result = await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Cartridges.Should().BeEmpty();
        result.Carriers.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ProcessesCartridgeWithBarcode_CreatesIdentifiedCartridgeInfo()
    {
        // Arrange
        var position = CreatePosition(1, 1, 1);
        var request = new ReagentDrawerContentsRequest();
        request.Cartridges.Add(new DetectedCartridge
        {
            Position = position,
            ReagentBarcode = _testCartridgeBarcode1,
            CannotReadBarcode = false
        });

        var cartridgeDto = CreateCartridgeDto(_testCartridgeBarcode1, ArticleType.ConjugateCartridge);
        SetupCartridges(new Dictionary<string, ReagentContentDto> { { _testCartridgeBarcode1, cartridgeDto } });
        SetupEmptyCarriers();

        var command = new GetReagentDrawerContentsCommand(_testInstrumentNumber, request);

        // Act
        var result = await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Cartridges.Should().HaveCount(1);
        var detectedCartridge = result.Cartridges[0];
        detectedCartridge.Position.Should().BeSameAs(position);
        detectedCartridge.UnknownCartridge.Should().BeFalse();
        detectedCartridge.Content.Should().NotBeNull();
        detectedCartridge.Content.ReagentBarcode.Should().Be(_testCartridgeBarcode1);
        detectedCartridge.Content.ArticleNumber.Should().Be("ART001");
        detectedCartridge.Content.LotNumber.Should().Be("LOT001");
    }

    [Fact]
    public async Task Handle_ProcessesCartridgeWithEmptyBarcode_CreatesUnknownCartridgeInfo()
    {
        // Arrange
        var position = CreatePosition(1, 2, 1);
        var request = new ReagentDrawerContentsRequest();
        request.Cartridges.Add(new DetectedCartridge
        {
            Position = position,
            ReagentBarcode = string.Empty,
            CannotReadBarcode = false
        });

        SetupEmptyCartridges();
        SetupEmptyCarriers();

        var command = new GetReagentDrawerContentsCommand(_testInstrumentNumber, request);

        // Act
        var result = await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Cartridges.Should().HaveCount(1);
        var detectedCartridge = result.Cartridges[0];
        detectedCartridge.Position.Should().BeSameAs(position);
        detectedCartridge.UnknownCartridge.Should().BeTrue();
        detectedCartridge.Content.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ProcessesCartridgeNotFoundInDatabase_CreatesUnknownCartridgeInfo()
    {
        // Arrange
        var position = CreatePosition(2, 1, 1);
        var request = new ReagentDrawerContentsRequest();
        request.Cartridges.Add(new DetectedCartridge
        {
            Position = position,
            ReagentBarcode = _testCartridgeBarcode1,
            CannotReadBarcode = false
        });

        SetupCartridges(new Dictionary<string, ReagentContentDto>());
        SetupEmptyCarriers();

        var command = new GetReagentDrawerContentsCommand(_testInstrumentNumber, request);

        // Act
        var result = await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Cartridges.Should().HaveCount(1);
        var detectedCartridge = result.Cartridges[0];
        detectedCartridge.UnknownCartridge.Should().BeTrue();
        detectedCartridge.Content.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ProcessesMultipleCartridges_CreatesMultipleCartridgeInfos()
    {
        // Arrange
        var position1 = CreatePosition(1, 1, 1);
        var position2 = CreatePosition(1, 1, 2);
        var request = new ReagentDrawerContentsRequest();
        request.Cartridges.Add(new DetectedCartridge
        {
            Position = position1,
            ReagentBarcode = _testCartridgeBarcode1,
            CannotReadBarcode = false
        });
        request.Cartridges.Add(new DetectedCartridge
        {
            Position = position2,
            ReagentBarcode = _testCartridgeBarcode2,
            CannotReadBarcode = false
        });

        var cartridges = new Dictionary<string, ReagentContentDto>
        {
            { _testCartridgeBarcode1, CreateCartridgeDto(_testCartridgeBarcode1, ArticleType.ConjugateCartridge) },
            { _testCartridgeBarcode2, CreateCartridgeDto(_testCartridgeBarcode2, ArticleType.DevelopmentSolutionCartridge) }
        };
        SetupCartridges(cartridges);
        SetupEmptyCarriers();

        var command = new GetReagentDrawerContentsCommand(_testInstrumentNumber, request);

        // Act
        var result = await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Cartridges.Should().HaveCount(2);
        result.Cartridges[0].Content.ReagentBarcode.Should().Be(_testCartridgeBarcode1);
        result.Cartridges[1].Content.ReagentBarcode.Should().Be(_testCartridgeBarcode2);
    }

    [Fact]
    public async Task Handle_ProcessesCarrierWithOcr_CreatesIdentifiedCarrierInfo()
    {
        // Arrange
        var position = CreatePosition(1, 2, 1);
        var request = new ReagentDrawerContentsRequest();
        request.Carriers.Add(new DetectedCarrier
        {
            Position = position,
            CarrierOcr = _testCarrierOcr1,
            CannotReadOcr = false
        });

        var carrierDto = CreateCarrierDto(_testCarrierOcr1, ArticleType.ImmunoCapCarrier, 75);
        SetupEmptyCartridges();
        SetupCarriers(new Dictionary<string, ReagentContentDto> { { _testCarrierOcr1, carrierDto } });

        var command = new GetReagentDrawerContentsCommand(_testInstrumentNumber, request);

        // Act
        var result = await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Carriers.Should().HaveCount(1);
        var detectedCarrier = result.Carriers[0];
        detectedCarrier.Position.Should().BeSameAs(position);
        detectedCarrier.UnknownCarrier.Should().BeFalse();
        detectedCarrier.CarrierType.Should().Be(EnumReagentType.ImmunoCapCarrier);
        detectedCarrier.TestName.Should().Be(_testCarrierOcr1);
        detectedCarrier.QuantityFull.Should().Be(75);
    }

    [Fact]
    public async Task Handle_ProcessesCarrierWithEmptyOcr_CreatesUnknownCarrierInfo()
    {
        // Arrange
        var position = CreatePosition(2, 1, 1);
        var request = new ReagentDrawerContentsRequest();
        request.Carriers.Add(new DetectedCarrier
        {
            Position = position,
            CarrierOcr = string.Empty,
            CannotReadOcr = false
        });

        SetupEmptyCartridges();
        SetupEmptyCarriers();

        var command = new GetReagentDrawerContentsCommand(_testInstrumentNumber, request);

        // Act
        var result = await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Carriers.Should().HaveCount(1);
        var detectedCarrier = result.Carriers[0];
        detectedCarrier.UnknownCarrier.Should().BeTrue();
        detectedCarrier.CarrierType.Should().Be(EnumReagentType.UnspecifiedReagentType);
        detectedCarrier.TestName.Should().BeEmpty();
        detectedCarrier.QuantityFull.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ProcessesCarrierWithCannotReadOcr_CreatesUnknownCarrierInfo()
    {
        // Arrange
        var position = CreatePosition(2, 2, 1);
        var request = new ReagentDrawerContentsRequest();
        request.Carriers.Add(new DetectedCarrier
        {
            Position = position,
            CarrierOcr = "SomeOcr",
            CannotReadOcr = true
        });

        SetupEmptyCartridges();
        SetupEmptyCarriers();

        var command = new GetReagentDrawerContentsCommand(_testInstrumentNumber, request);

        // Act
        var result = await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Carriers.Should().HaveCount(1);
        var detectedCarrier = result.Carriers[0];
        detectedCarrier.UnknownCarrier.Should().BeTrue();
        detectedCarrier.CarrierType.Should().Be(EnumReagentType.UnspecifiedReagentType);
    }

    [Fact]
    public async Task Handle_ProcessesCarrierNotFoundInDatabase_CreatesUnknownCarrierInfo()
    {
        // Arrange
        var position = CreatePosition(1, 3, 1);
        var request = new ReagentDrawerContentsRequest();
        request.Carriers.Add(new DetectedCarrier
        {
            Position = position,
            CarrierOcr = _testCarrierOcr1,
            CannotReadOcr = false
        });

        SetupEmptyCartridges();
        SetupCarriers(new Dictionary<string, ReagentContentDto>());

        var command = new GetReagentDrawerContentsCommand(_testInstrumentNumber, request);

        // Act
        var result = await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Carriers.Should().HaveCount(1);
        var detectedCarrier = result.Carriers[0];
        detectedCarrier.UnknownCarrier.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_MapsImmunoCapCarrier_ToCorrectEnumReagentType()
    {
        // Arrange
        var request = new ReagentDrawerContentsRequest();
        request.Carriers.Add(new DetectedCarrier
        {
            Position = CreatePosition(1, 1, 1),
            CarrierOcr = _testCarrierOcr1,
            CannotReadOcr = false
        });

        var carrierDto = CreateCarrierDto(_testCarrierOcr1, ArticleType.ImmunoCapCarrier, 100);
        SetupEmptyCartridges();
        SetupCarriers(new Dictionary<string, ReagentContentDto> { { _testCarrierOcr1, carrierDto } });

        var command = new GetReagentDrawerContentsCommand(_testInstrumentNumber, request);

        // Act
        var result = await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Carriers[0].CarrierType.Should().Be(EnumReagentType.ImmunoCapCarrier);
    }

    [Fact]
    public async Task Handle_MapsEliaWellCarrier_ToCorrectEnumReagentType()
    {
        // Arrange
        var request = new ReagentDrawerContentsRequest();
        request.Carriers.Add(new DetectedCarrier
        {
            Position = CreatePosition(1, 1, 1),
            CarrierOcr = _testCarrierOcr1,
            CannotReadOcr = false
        });

        var carrierDto = CreateCarrierDto(_testCarrierOcr1, ArticleType.EliaWellCarrier, 80);
        SetupEmptyCartridges();
        SetupCarriers(new Dictionary<string, ReagentContentDto> { { _testCarrierOcr1, carrierDto } });

        var command = new GetReagentDrawerContentsCommand(_testInstrumentNumber, request);

        // Act
        var result = await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Carriers[0].CarrierType.Should().Be(EnumReagentType.EliaWellCarrier);
    }

    [Fact]
    public async Task Handle_MapsEliaCalibratorWellCarrier_ToCorrectEnumReagentType()
    {
        // Arrange
        var request = new ReagentDrawerContentsRequest();
        request.Carriers.Add(new DetectedCarrier
        {
            Position = CreatePosition(1, 1, 1),
            CarrierOcr = _testCarrierOcr1,
            CannotReadOcr = false
        });

        var carrierDto = CreateCarrierDto(_testCarrierOcr1, ArticleType.EliaCalibratorWellCarrier, 60);
        SetupEmptyCartridges();
        SetupCarriers(new Dictionary<string, ReagentContentDto> { { _testCarrierOcr1, carrierDto } });

        var command = new GetReagentDrawerContentsCommand(_testInstrumentNumber, request);

        // Act
        var result = await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Carriers[0].CarrierType.Should().Be(EnumReagentType.EliaCalibrationWellCarrier);
    }

    [Fact]
    public async Task Handle_MapsUnknownArticleType_ToUnspecifiedReagentType()
    {
        // Arrange
        var request = new ReagentDrawerContentsRequest();
        request.Carriers.Add(new DetectedCarrier
        {
            Position = CreatePosition(1, 1, 1),
            CarrierOcr = _testCarrierOcr1,
            CannotReadOcr = false
        });

        var carrierDto = CreateCarrierDto(_testCarrierOcr1, ArticleType.None, 50);
        SetupEmptyCartridges();
        SetupCarriers(new Dictionary<string, ReagentContentDto> { { _testCarrierOcr1, carrierDto } });

        var command = new GetReagentDrawerContentsCommand(_testInstrumentNumber, request);

        // Act
        var result = await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Carriers[0].UnknownCarrier.Should().BeTrue();
        result.Carriers[0].CarrierType.Should().Be(EnumReagentType.UnspecifiedReagentType);
    }

    [Fact]
    public async Task Handle_SetsQuantityToMinimumRemainingQuantity_ForCarrier()
    {
        // Arrange
        var request = new ReagentDrawerContentsRequest();
        request.Carriers.Add(new DetectedCarrier
        {
            Position = CreatePosition(1, 1, 1),
            CarrierOcr = _testCarrierOcr1,
            CannotReadOcr = false
        });

        var carrierDto = CreateCarrierDto(_testCarrierOcr1, ArticleType.ImmunoCapCarrier, 45);
        SetupEmptyCartridges();
        SetupCarriers(new Dictionary<string, ReagentContentDto> { { _testCarrierOcr1, carrierDto } });

        var command = new GetReagentDrawerContentsCommand(_testInstrumentNumber, request);

        // Act
        var result = await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Carriers[0].QuantityFull.Should().Be(45);
    }

    [Fact]
    public async Task Handle_ProcessesMultipleCarriers_CreatesMultipleCarrierInfos()
    {
        // Arrange
        var position1 = CreatePosition(1, 2, 1);
        var position2 = CreatePosition(1, 2, 2);
        var request = new ReagentDrawerContentsRequest();
        request.Carriers.Add(new DetectedCarrier
        {
            Position = position1,
            CarrierOcr = _testCarrierOcr1,
            CannotReadOcr = false
        });
        request.Carriers.Add(new DetectedCarrier
        {
            Position = position2,
            CarrierOcr = _testCarrierOcr2,
            CannotReadOcr = false
        });

        var carriers = new Dictionary<string, ReagentContentDto>
        {
            { _testCarrierOcr1, CreateCarrierDto(_testCarrierOcr1, ArticleType.ImmunoCapCarrier, 90) },
            { _testCarrierOcr2, CreateCarrierDto(_testCarrierOcr2, ArticleType.EliaWellCarrier, 85) }
        };
        SetupEmptyCartridges();
        SetupCarriers(carriers);

        var command = new GetReagentDrawerContentsCommand(_testInstrumentNumber, request);

        // Act
        var result = await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Carriers.Should().HaveCount(2);
        result.Carriers[0].TestName.Should().Be(_testCarrierOcr1);
        result.Carriers[0].CarrierType.Should().Be(EnumReagentType.ImmunoCapCarrier);
        result.Carriers[1].TestName.Should().Be(_testCarrierOcr2);
        result.Carriers[1].CarrierType.Should().Be(EnumReagentType.EliaWellCarrier);
    }

    [Fact]
    public async Task Handle_ProcessesMixedCartridgesAndCarriers_CreatesCorrectResponse()
    {
        // Arrange
        var cartridgePosition = CreatePosition(1, 1, 1);
        var carrierPosition = CreatePosition(1, 2, 1);
        var request = new ReagentDrawerContentsRequest();
        request.Cartridges.Add(new DetectedCartridge
        {
            Position = cartridgePosition,
            ReagentBarcode = _testCartridgeBarcode1,
            CannotReadBarcode = false
        });
        request.Carriers.Add(new DetectedCarrier
        {
            Position = carrierPosition,
            CarrierOcr = _testCarrierOcr1,
            CannotReadOcr = false
        });

        var cartridgeDto = CreateCartridgeDto(_testCartridgeBarcode1, ArticleType.ConjugateCartridge);
        var carrierDto = CreateCarrierDto(_testCarrierOcr1, ArticleType.ImmunoCapCarrier, 70);
        SetupCartridges(new Dictionary<string, ReagentContentDto> { { _testCartridgeBarcode1, cartridgeDto } });
        SetupCarriers(new Dictionary<string, ReagentContentDto> { { _testCarrierOcr1, carrierDto } });

        var command = new GetReagentDrawerContentsCommand(_testInstrumentNumber, request);

        // Act
        var result = await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Cartridges.Should().HaveCount(1);
        result.Carriers.Should().HaveCount(1);
        result.Cartridges[0].UnknownCartridge.Should().BeFalse();
        result.Carriers[0].UnknownCarrier.Should().BeFalse();
    }

    [Trait("ProductRequirement", "PR_1366")]
    [Trait("ModuleRequirement", "MR_4580")]
    [Fact]
    public async Task Handle_MapsCartridgeContentCells_Correctly()
    {
        // Arrange
        var position = CreatePosition(1, 1, 1);
        var request = new ReagentDrawerContentsRequest();
        request.Cartridges.Add(new DetectedCartridge
        {
            Position = position,
            ReagentBarcode = _testCartridgeBarcode1,
            CannotReadBarcode = false
        });

        var cartridgeDto = CreateCartridgeDtoWithMultipleCells(_testCartridgeBarcode1);
        SetupCartridges(new Dictionary<string, ReagentContentDto> { { _testCartridgeBarcode1, cartridgeDto } });
        SetupEmptyCarriers();

        var command = new GetReagentDrawerContentsCommand(_testInstrumentNumber, request);

        // Act
        var result = await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        var detectedCartridge = result.Cartridges[0];
        detectedCartridge.Content.ContentCells.Should().HaveCount(3);
        detectedCartridge.Content.ContentCells[0].Position.Should().Be(1);
        detectedCartridge.Content.ContentCells[0].RemainingQuantity.Should().Be(100);
        detectedCartridge.Content.ContentCells[1].Position.Should().Be(2);
        detectedCartridge.Content.ContentCells[1].RemainingQuantity.Should().Be(80);
        detectedCartridge.Content.ContentCells[2].Position.Should().Be(3);
        detectedCartridge.Content.ContentCells[2].RemainingQuantity.Should().Be(60);
    }

    [Trait("ProductRequirement", "PR_1366")]
    [Trait("ModuleRequirement", "MR_4573")]
    [Fact]
    public async Task Handle_PreservesPositionInformation_ForCartridges()
    {
        // Arrange
        var position = CreatePosition(3, 2, 4);
        var request = new ReagentDrawerContentsRequest();
        request.Cartridges.Add(new DetectedCartridge
        {
            Position = position,
            ReagentBarcode = _testCartridgeBarcode1,
            CannotReadBarcode = false
        });

        var cartridgeDto = CreateCartridgeDto(_testCartridgeBarcode1, ArticleType.ConjugateCartridge);
        SetupCartridges(new Dictionary<string, ReagentContentDto> { { _testCartridgeBarcode1, cartridgeDto } });
        SetupEmptyCarriers();

        var command = new GetReagentDrawerContentsCommand(_testInstrumentNumber, request);

        // Act
        var result = await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        var detectedCartridge = result.Cartridges[0];
        detectedCartridge.Position.Should().BeSameAs(position);
        detectedCartridge.Position.DrawerPosition.Should().Be(3);
        detectedCartridge.Position.CompartmentPosition.Should().Be(2);
        detectedCartridge.Position.SubCompartmentPosition.Should().Be(4);
    }

    [Trait("ProductRequirement", "PR_1366")]
    [Trait("ModuleRequirement", "MR_4573")]
    [Fact]
    public async Task Handle_PreservesPositionInformation_ForCarriers()
    {
        // Arrange
        var position = CreatePosition(2, 3, 1);
        var request = new ReagentDrawerContentsRequest();
        request.Carriers.Add(new DetectedCarrier
        {
            Position = position,
            CarrierOcr = _testCarrierOcr1,
            CannotReadOcr = false
        });

        var carrierDto = CreateCarrierDto(_testCarrierOcr1, ArticleType.ImmunoCapCarrier, 95);
        SetupEmptyCartridges();
        SetupCarriers(new Dictionary<string, ReagentContentDto> { { _testCarrierOcr1, carrierDto } });

        var command = new GetReagentDrawerContentsCommand(_testInstrumentNumber, request);

        // Act
        var result = await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        var detectedCarrier = result.Carriers[0];
        detectedCarrier.Position.Should().BeSameAs(position);
        detectedCarrier.Position.DrawerPosition.Should().Be(2);
        detectedCarrier.Position.CompartmentPosition.Should().Be(3);
        detectedCarrier.Position.SubCompartmentPosition.Should().Be(1);
    }

    [Fact]
    public async Task Handle_SetsCarrierToUnknown_WhenCarrierDtoIsNull()
    {
        // Arrange
        var position = CreatePosition(1, 2, 1);
        var request = new ReagentDrawerContentsRequest();
        request.Carriers.Add(new DetectedCarrier
        {
            Position = position,
            CarrierOcr = _testCarrierOcr1,
            CannotReadOcr = false
        });

        SetupEmptyCartridges();
        SetupCarriers(new Dictionary<string, ReagentContentDto> { { _testCarrierOcr1, null! } });

        var command = new GetReagentDrawerContentsCommand(_testInstrumentNumber, request);

        // Act
        var result = await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Carriers[0].UnknownCarrier.Should().BeTrue();
        result.Carriers[0].CarrierType.Should().Be(EnumReagentType.UnspecifiedReagentType);
    }

    [Theory]
    [InlineData(ArticleType.ConjugateCartridge)]
    [InlineData(ArticleType.DevelopmentSolutionCartridge)]
    [InlineData(ArticleType.DiluentCartridge)]
    public async Task Handle_HandlesVariousCartridgeTypes(ArticleType articleType)
    {
        // Arrange
        var request = new ReagentDrawerContentsRequest();
        request.Cartridges.Add(new DetectedCartridge
        {
            Position = CreatePosition(1, 1, 1),
            ReagentBarcode = _testCartridgeBarcode1,
            CannotReadBarcode = false
        });

        var cartridgeDto = CreateCartridgeDto(_testCartridgeBarcode1, articleType);
        SetupCartridges(new Dictionary<string, ReagentContentDto> { { _testCartridgeBarcode1, cartridgeDto } });
        SetupEmptyCarriers();

        var command = new GetReagentDrawerContentsCommand(_testInstrumentNumber, request);

        // Act
        var result = await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Cartridges[0].UnknownCartridge.Should().BeFalse();
        result.Cartridges[0].Content.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_HandlesNullContentCells_InCarrier()
    {
        // Arrange
        var position = CreatePosition(1, 2, 1);
        var request = new ReagentDrawerContentsRequest();
        request.Carriers.Add(new DetectedCarrier
        {
            Position = position,
            CarrierOcr = _testCarrierOcr1,
            CannotReadOcr = false
        });

        var carrierDto = new ReagentContentDto
        {
            ReagentBarcode = _testCarrierOcr1,
            ArticleType = ArticleType.ImmunoCapCarrier,
            ArticleNumber = "ART001",
            ContentCells = null!
        };

        SetupEmptyCartridges();
        SetupCarriers(new Dictionary<string, ReagentContentDto> { { _testCarrierOcr1, carrierDto } });

        var command = new GetReagentDrawerContentsCommand(_testInstrumentNumber, request);

        // Act & Assert
        var act = async () => await _sut.Handle(command, TestContext.Current.CancellationToken);
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task Handle_VerifiesGetCartridgeContentsQueryInvocation()
    {
        // Arrange
        var request = new ReagentDrawerContentsRequest();
        request.Cartridges.Add(new DetectedCartridge
        {
            Position = CreatePosition(1, 1, 1),
            ReagentBarcode = _testCartridgeBarcode1,
            CannotReadBarcode = false
        });

        SetupEmptyCartridges();
        SetupEmptyCarriers();

        var command = new GetReagentDrawerContentsCommand(_testInstrumentNumber, request);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _mediatorMock.Verify(m => m.Send(It.IsAny<GetCartridgeContentsQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_VerifiesGetCarrierContentsByOcrQueryInvocation()
    {
        // Arrange
        var request = new ReagentDrawerContentsRequest();
        request.Carriers.Add(new DetectedCarrier
        {
            Position = CreatePosition(1, 2, 1),
            CarrierOcr = _testCarrierOcr1,
            CannotReadOcr = false
        });

        SetupEmptyCartridges();
        SetupEmptyCarriers();

        var command = new GetReagentDrawerContentsCommand(_testInstrumentNumber, request);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _mediatorMock.Verify(m => m.Send(It.IsAny<GetCarrierContentsByOcrQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_MapsLotExpiryDateCorrectly()
    {
        // Arrange
        var position = CreatePosition(1, 1, 1);
        var request = new ReagentDrawerContentsRequest();
        request.Cartridges.Add(new DetectedCartridge
        {
            Position = position,
            ReagentBarcode = _testCartridgeBarcode1,
            CannotReadBarcode = false
        });

        var cartridgeDto = CreateCartridgeDto(_testCartridgeBarcode1, ArticleType.ConjugateCartridge);
        SetupCartridges(new Dictionary<string, ReagentContentDto> { { _testCartridgeBarcode1, cartridgeDto } });
        SetupEmptyCarriers();

        var command = new GetReagentDrawerContentsCommand(_testInstrumentNumber, request);

        // Act
        var result = await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        var expectedTimestamp = Timestamp.FromDateTimeOffset(_lotExpiryDate.ToUniversalTime());
        result.Cartridges[0].Content.LotExpiryDate.Should().Be(expectedTimestamp);
    }

    [Fact]
    public async Task Handle_MapsOpenTimestampCorrectly()
    {
        // Arrange
        var position = CreatePosition(1, 1, 1);
        var request = new ReagentDrawerContentsRequest();
        request.Cartridges.Add(new DetectedCartridge
        {
            Position = position,
            ReagentBarcode = _testCartridgeBarcode1,
            CannotReadBarcode = false
        });

        var cartridgeDto = CreateCartridgeDto(_testCartridgeBarcode1, ArticleType.ConjugateCartridge);
        SetupCartridges(new Dictionary<string, ReagentContentDto> { { _testCartridgeBarcode1, cartridgeDto } });
        SetupEmptyCarriers();

        var command = new GetReagentDrawerContentsCommand(_testInstrumentNumber, request);

        // Act
        var result = await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        var expectedTimestamp = Timestamp.FromDateTimeOffset(_openTimestamp.ToUniversalTime());
        result.Cartridges[0].Content.ContentCells[0].OpenTimestamp.Should().Be(expectedTimestamp);
    }

    private static ReagentDrawerPosition CreatePosition(int drawer, int compartment, int subCompartment)
    {
        return new ReagentDrawerPosition
        {
            DrawerPosition = drawer,
            CompartmentPosition = compartment,
            SubCompartmentPosition = subCompartment
        };
    }

    private static ReagentContentDto CreateCartridgeDto(string barcode, ArticleType articleType)
    {
        return new ReagentContentDto
        {
            ReagentBarcode = barcode,
            CarrierUdi = string.Empty,
            ArticleType = articleType,
            ArticleNumber = "ART001",
            LotNumber = "LOT001",
            LotExpiryDate = _lotExpiryDate,
            SerialNumber = string.Empty,
            ContentCells = new List<ReagentContentCellDto>
            {
                new()
                {
                    Position = 1,
                    RemainingQuantity = 96,
                    UseRemainingQuantity = true,
                    OpenTimestamp = _openTimestamp
                }
            }
        };
    }

    private static ReagentContentDto CreateCartridgeDtoWithMultipleCells(string barcode)
    {
        return new ReagentContentDto
        {
            ReagentBarcode = barcode,
            CarrierUdi = string.Empty,
            ArticleType = ArticleType.ConjugateCartridge,
            ArticleNumber = "ART001",
            LotNumber = "LOT001",
            LotExpiryDate = _lotExpiryDate,
            SerialNumber = string.Empty,
            ContentCells = new List<ReagentContentCellDto>
            {
                new() { Position = 1, RemainingQuantity = 100, UseRemainingQuantity = true, OpenTimestamp = _openTimestamp },
                new() { Position = 2, RemainingQuantity = 80, UseRemainingQuantity = true, OpenTimestamp = _openTimestamp },
                new() { Position = 3, RemainingQuantity = 60, UseRemainingQuantity = true, OpenTimestamp = _openTimestamp }
            }
        };
    }

    private static ReagentContentDto CreateCarrierDto(string ocr, ArticleType articleType, int minQuantity)
    {
        return new ReagentContentDto
        {
            ReagentBarcode = ocr,
            ArticleType = articleType,
            ArticleNumber = "ART001",
            LotNumber = "LOT001",
            LotExpiryDate = _lotExpiryDate,
            ContentCells = new List<ReagentContentCellDto>
            {
                new() { Position = 1, RemainingQuantity = minQuantity, UseRemainingQuantity = true }
            }
        };
    }

    private void SetupCartridges(Dictionary<string, ReagentContentDto> cartridges)
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetCartridgeContentsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cartridges);
    }

    private void SetupEmptyCartridges()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetCartridgeContentsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, ReagentContentDto>());
    }

    private void SetupCarriers(Dictionary<string, ReagentContentDto> carriers)
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetCarrierContentsByOcrQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(carriers);
    }

    private void SetupEmptyCarriers()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetCarrierContentsByOcrQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, ReagentContentDto>());
    }
}
