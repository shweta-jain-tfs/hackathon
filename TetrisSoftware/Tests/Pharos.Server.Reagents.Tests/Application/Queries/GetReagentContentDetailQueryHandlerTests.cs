using System.Drawing;
using Moq.EntityFrameworkCore;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Reagents;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Parameters;
using Pharos.Server.Reagents.Application.Interfaces;
using Pharos.Server.Reagents.Domain;
using Tetris.Protocol.Pharos.Storage;
using LotInformation = Pharos.Server.Application.Reagents.LotInformation;
using ReagentContent = Pharos.Server.Reagents.Domain.ReagentContent;

namespace Pharos.Server.Reagents.Application.Queries;

public class GetReagentContentDetailQueryHandlerTests
{
    private const string _stopSolutionBarcode = "|01414141414141411723051510STP01|991234";
    private const string _conjugateBarcode = "CONJUGATE_BARCODE";
    private const string _curveControlBarcode = "CURVE_CONTROL_BARCODE";
    private const string _calibratorBarcode = "CALIBRATOR_BARCODE";
    private const string _testInstrumentNumber = "INST001";

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("   ", "   ")]
    public async Task Handle_ReturnsEmptyDto_WhenBarcodesAreInvalid(string? barcode, string? reagentUdi)
    {
        // Arrange
        var handler = CreateHandler();
        var query = new GetReagentContentDetailQuery(barcode, reagentUdi, _testInstrumentNumber);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Type.Should().BeNull();
        result.Name.Should().BeNull();
        result.Quantity.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ReturnsEmptyDto_WhenReagentInfoIsNull()
    {
        // Arrange
        var barcodeService = new Mock<IReagentBarcodeService>();
        barcodeService
            .Setup(x => x.GetReagentInfo(ArticleType.StopSolutionBottle, _stopSolutionBarcode, null))
            .ReturnsAsync((ReagentInfo?)null);

        var handler = CreateHandler(barcodeService: barcodeService.Object);
        var query = new GetReagentContentDetailQuery(_stopSolutionBarcode, null, _testInstrumentNumber);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Type.Should().BeNull();
        result.Name.Should().BeNull();
        result.Quantity.Should().Be(0);
    }

    [Fact]
    public async Task Handle_PopulatesBasicFields_WhenReagentInfoExists()
    {
        // Arrange
        var articleType = ArticleType.StopSolutionBottle;
        var reagentInfo = CreateReagentInfo(articleType, "Stop Solution", "STP_Identity", "LOT456", 3000);

        var barcodeService = new Mock<IReagentBarcodeService>();
        barcodeService
            .Setup(x => x.DetectArticleType(_stopSolutionBarcode))
            .ReturnsAsync((articleType, null));
        barcodeService
            .Setup(x => x.GetReagentInfo(articleType, _stopSolutionBarcode, null))
            .ReturnsAsync(reagentInfo);

        var handler = CreateHandler(barcodeService: barcodeService.Object);
        var query = new GetReagentContentDetailQuery(_stopSolutionBarcode, null, _testInstrumentNumber);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Type.Should().Be(articleType);
        result.Name.Should().Be("Stop Solution");
        result.LotNumber.Should().Be("LOT456");
        result.Quantity.Should().Be(3000);
    }

    [Theory]
    [InlineData(_stopSolutionBarcode, null, true)]  // Uses barcode
    [InlineData(null, _stopSolutionBarcode, false)] // Uses reagentUdi
    public async Task Handle_UsesCorrectIdentifier_BasedOnInput(
        string? barcode,
        string? reagentUdi,
        bool shouldUseBarcode)
    {
        // Arrange
        var articleType = ArticleType.StopSolutionBottle;
        var reagentInfo = CreateReagentInfo(articleType, "Stop Solution", "STP_Identity");
        var identifier = shouldUseBarcode ? barcode! : reagentUdi!;

        var barcodeService = new Mock<IReagentBarcodeService>();
        barcodeService
            .Setup(x => x.DetectArticleType(identifier))
            .ReturnsAsync((articleType, null));
        barcodeService
            .Setup(x => x.GetReagentInfo(articleType, barcode, reagentUdi))
            .ReturnsAsync(reagentInfo);

        var handler = CreateHandler(barcodeService: barcodeService.Object);
        var query = new GetReagentContentDetailQuery(barcode, reagentUdi, _testInstrumentNumber);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Type.Should().Be(articleType);
        barcodeService.Verify(x => x.DetectArticleType(identifier), Times.Once);
        barcodeService.Verify(x => x.GetReagentInfo(articleType, barcode, reagentUdi), Times.Once);
    }

    [Fact]
    public async Task Handle_UsesQuantityFromDatabase_WhenReagentExistsWithContents()
    {
        // Arrange
        var articleType = ArticleType.StopSolutionBottle;
        var reagentInfo = CreateReagentInfo(articleType, "Stop Solution", "STP_Identity", quantity: 3000);
        var savedQuantity = 750;

        var reagent = new Reagent
        {
            Barcode = _stopSolutionBarcode,
            ReagentContents = new List<ReagentContent> { new() { Quantity = savedQuantity } }
        };

        var barcodeService = new Mock<IReagentBarcodeService>();
        barcodeService
            .Setup(x => x.DetectArticleType(_stopSolutionBarcode))
            .ReturnsAsync((articleType, null));
        barcodeService
            .Setup(x => x.GetReagentInfo(articleType, _stopSolutionBarcode, null))
            .ReturnsAsync(reagentInfo);

        var handler = CreateHandler(new List<Reagent> { reagent }, barcodeService.Object);
        var query = new GetReagentContentDetailQuery(_stopSolutionBarcode, null, _testInstrumentNumber);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Quantity.Should().Be(savedQuantity);
    }

    [Fact]
    public async Task Handle_SumsAllQuantities_ForNonCartridgeTypes()
    {
        // Arrange
        var articleType = ArticleType.StopSolutionBottle;
        var reagentInfo = CreateReagentInfo(articleType, "Stop Solution", "STP_Identity");

        var reagent = new Reagent
        {
            Barcode = _stopSolutionBarcode,
            ReagentContents = new List<ReagentContent>
            {
                new() { Quantity = 500 },
                new() { Quantity = 300 },
                new() { Quantity = 200 }
            }
        };

        var barcodeService = new Mock<IReagentBarcodeService>();
        barcodeService
            .Setup(x => x.DetectArticleType(_stopSolutionBarcode))
            .ReturnsAsync((articleType, null));
        barcodeService
            .Setup(x => x.GetReagentInfo(articleType, _stopSolutionBarcode, null))
            .ReturnsAsync(reagentInfo);

        var handler = CreateHandler(new List<Reagent> { reagent }, barcodeService.Object);
        var query = new GetReagentContentDetailQuery(_stopSolutionBarcode, null, _testInstrumentNumber);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Quantity.Should().Be(1000);
    }

    [Fact]
    public async Task Handle_ExcludesPiercedQuantities_ForCartridgeTypes()
    {
        // Arrange
        var articleType = ArticleType.ConjugateCartridge;
        var reagentInfo = CreateReagentInfo(articleType, "Conjugate Cartridge", "CONJ_Identity");

        var reagent = new Reagent
        {
            Barcode = _conjugateBarcode,
            ReagentContents = new List<ReagentContent>
            {
                new() { Quantity = 500, OpenTimestamp = null },
                new() { Quantity = 300, OpenTimestamp = DateTimeOffset.UtcNow },
                new() { Quantity = 200, OpenTimestamp = DateTimeOffset.MinValue }
            }
        };

        var barcodeService = new Mock<IReagentBarcodeService>();
        barcodeService
            .Setup(x => x.DetectArticleType(_conjugateBarcode))
            .ReturnsAsync((articleType, null));
        barcodeService
            .Setup(x => x.GetReagentInfo(articleType, _conjugateBarcode, null))
            .ReturnsAsync(reagentInfo);

        var handler = CreateHandler(new List<Reagent> { reagent }, barcodeService.Object);
        var query = new GetReagentContentDetailQuery(_conjugateBarcode, null, _testInstrumentNumber);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Quantity.Should().Be(700);
    }

    [Theory]
    [InlineData(ArticleType.CurveControlCartridge, _curveControlBarcode, "Curve Control Cartridge", "CC_Identity")]
    [InlineData(ArticleType.CalibratorCartridge, _calibratorBarcode, "Calibrator Cartridge", "CAL_Identity")]
    public async Task Handle_ReturnsZeroQuantity_ForSpecialCartridges_WhenAnyContentIsPierced(
        ArticleType articleType,
        string barcode,
        string friendlyName,
        string identity)
    {
        // Arrange
        var reagentInfo = CreateReagentInfo(articleType, friendlyName, identity);

        var reagent = new Reagent
        {
            Barcode = barcode,
            ReagentContents = new List<ReagentContent>
            {
                new() { Quantity = 500, OpenTimestamp = null },
                new() { Quantity = 300, OpenTimestamp = DateTimeOffset.UtcNow }
            }
        };

        var barcodeService = new Mock<IReagentBarcodeService>();
        barcodeService
            .Setup(x => x.DetectArticleType(barcode))
            .ReturnsAsync((articleType, null));
        barcodeService
            .Setup(x => x.GetReagentInfo(articleType, barcode, null))
            .ReturnsAsync(reagentInfo);

        var handler = CreateHandler(new List<Reagent> { reagent }, barcodeService.Object);
        var query = new GetReagentContentDetailQuery(barcode, null, _testInstrumentNumber);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Quantity.Should().Be(0);
    }

    [Theory]
    [InlineData(ArticleType.CalibratorCartridge)]
    [InlineData(ArticleType.ConjugateCartridge)]
    [InlineData(ArticleType.CurveControlCartridge)]
    [InlineData(ArticleType.DevelopmentSolutionCartridge)]
    [InlineData(ArticleType.DiluentCartridge)]
    [InlineData(ArticleType.QualityControlCartridge)]
    public async Task Handle_IdentifiesCartridgeTypes_Correctly(ArticleType articleType)
    {
        // Arrange
        var barcode = $"CARTRIDGE_BARCODE_{articleType}";
        var reagentInfo = CreateReagentInfo(articleType, $"{articleType} Cartridge", "Identity");

        var reagent = new Reagent
        {
            Barcode = barcode,
            ReagentContents = new List<ReagentContent>
            {
                new() { Quantity = 500, OpenTimestamp = null },
                new() { Quantity = 300, OpenTimestamp = DateTimeOffset.UtcNow }
            }
        };

        var barcodeService = new Mock<IReagentBarcodeService>();
        barcodeService
            .Setup(x => x.DetectArticleType(barcode))
            .ReturnsAsync((articleType, null));
        barcodeService
            .Setup(x => x.GetReagentInfo(articleType, barcode, null))
            .ReturnsAsync(reagentInfo);

        var handler = CreateHandler(new List<Reagent> { reagent }, barcodeService.Object);
        var query = new GetReagentContentDetailQuery(barcode, null, _testInstrumentNumber);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        if (articleType is ArticleType.CalibratorCartridge or ArticleType.CurveControlCartridge)
        {
            result.Quantity.Should().Be(0);
        }
        else
        {
            result.Quantity.Should().Be(500);
        }
    }

    [Theory]
    [InlineData(true, true)]   // ReagentContents is null
    [InlineData(true, false)]  // ReagentContents is empty
    [InlineData(false, false)] // Reagent not found in database
    public async Task Handle_UsesDefaultQuantity_WhenNoValidContents(
        bool includeReagent,
        bool useNullContents)
    {
        // Arrange
        var articleType = ArticleType.StopSolutionBottle;
        var defaultQuantity = 3000;
        var reagentInfo = CreateReagentInfo(articleType, "Stop Solution", "STP_Identity", quantity: defaultQuantity);

        var reagents = includeReagent
            ? new List<Reagent>
            {
                new()
                {
                    Barcode = _stopSolutionBarcode,
                    ReagentContents = useNullContents ? null : new List<ReagentContent>()
                }
            }
            : new List<Reagent>();

        var barcodeService = new Mock<IReagentBarcodeService>();
        barcodeService
            .Setup(x => x.DetectArticleType(_stopSolutionBarcode))
            .ReturnsAsync((articleType, null));
        barcodeService
            .Setup(x => x.GetReagentInfo(articleType, _stopSolutionBarcode, null))
            .ReturnsAsync(reagentInfo);

        var handler = CreateHandler(reagents, barcodeService.Object);
        var query = new GetReagentContentDetailQuery(_stopSolutionBarcode, null, _testInstrumentNumber);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Quantity.Should().Be(defaultQuantity);
    }

    [Fact]
    public async Task Handle_TreatsMinValueTimestamp_AsNotPierced()
    {
        // Arrange
        var articleType = ArticleType.ConjugateCartridge;
        var reagentInfo = CreateReagentInfo(articleType, "Conjugate Cartridge", "CONJ_Identity");

        var reagent = new Reagent
        {
            Barcode = _conjugateBarcode,
            ReagentContents = new List<ReagentContent>
            {
                new() { Quantity = 500, OpenTimestamp = DateTimeOffset.MinValue }
            }
        };

        var barcodeService = new Mock<IReagentBarcodeService>();
        barcodeService
            .Setup(x => x.DetectArticleType(_conjugateBarcode))
            .ReturnsAsync((articleType, null));
        barcodeService
            .Setup(x => x.GetReagentInfo(articleType, _conjugateBarcode, null))
            .ReturnsAsync(reagentInfo);

        var handler = CreateHandler(new List<Reagent> { reagent }, barcodeService.Object);
        var query = new GetReagentContentDetailQuery(_conjugateBarcode, null, _testInstrumentNumber);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Quantity.Should().Be(500);
    }

    [Fact]
    public async Task Handle_PrefersBarcode_OverReagentUdi_WhenBothProvided()
    {
        // Arrange
        var reagentUdi = "|DIFFERENT_UDI|";
        var articleType = ArticleType.StopSolutionBottle;
        var reagentInfo = CreateReagentInfo(articleType, "Stop Solution", "STP_Identity");

        var barcodeService = new Mock<IReagentBarcodeService>();
        barcodeService
            .Setup(x => x.DetectArticleType(_stopSolutionBarcode))
            .ReturnsAsync((articleType, null));
        barcodeService
            .Setup(x => x.GetReagentInfo(articleType, _stopSolutionBarcode, reagentUdi))
            .ReturnsAsync(reagentInfo);

        var handler = CreateHandler(barcodeService: barcodeService.Object);
        var query = new GetReagentContentDetailQuery(_stopSolutionBarcode, reagentUdi, _testInstrumentNumber);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        barcodeService.Verify(x => x.DetectArticleType(_stopSolutionBarcode), Times.Once);
        barcodeService.Verify(x => x.DetectArticleType(reagentUdi), Times.Never);
    }

    [Fact]
    public async Task Handle_CalculatesCalibratorSets_WhenCartridgeFoundInInstrument()
    {
        // Arrange
        var articleType = ArticleType.CalibratorCartridge;
        var reagentInfo = CreateReagentInfo(articleType, "Calibrator Cartridge", "CAL_Identity", methodName: "TestMethod");

        var cells = new List<CartridgeCell>
        {
            CreateCartridgeCell(1, isPierced: false),
            CreateCartridgeCell(2, isPierced: false),
            CreateCartridgeCell(3, isPierced: false),
            CreateCartridgeCell(4, isPierced: false),
            CreateCartridgeCell(5, isPierced: false),
            CreateCartridgeCell(6, isPierced: false),
            CreateCartridgeCell(7, isPierced: true),
            CreateCartridgeCell(8, isPierced: true),
            CreateCartridgeCell(9, isPierced: true),
            CreateCartridgeCell(10, isPierced: true),
            CreateCartridgeCell(11, isPierced: true),
            CreateCartridgeCell(12, isPierced: true)
        };

        var cartridge = new Cartridge
        {
            ReagentBarcode = _calibratorBarcode,
            Cells = { cells }
        };

        var barcodeService = new Mock<IReagentBarcodeService>();
        barcodeService
            .Setup(x => x.DetectArticleType(_calibratorBarcode))
            .ReturnsAsync((articleType, null));
        barcodeService
            .Setup(x => x.GetReagentInfo(articleType, _calibratorBarcode, null))
            .ReturnsAsync(reagentInfo);

        var instrumentReagentStorage = new Mock<IInstrumentReagentStorage>();
        instrumentReagentStorage
            .Setup(x => x.GetCartridges(_testInstrumentNumber))
            .Returns(new List<Cartridge> { cartridge });

        var handler = CreateHandler(
            barcodeService: barcodeService.Object,
            instrumentReagentStorage: instrumentReagentStorage.Object);

        var query = new GetReagentContentDetailQuery(_calibratorBarcode, null, _testInstrumentNumber);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Quantity.Should().Be(1); // 6 unpierced cells / 6 cells per set = 1 set
        result.UnitOfMeasure.Should().Be(ArticleQuantityUnit.Sets);
    }

    [Fact]
    public async Task Handle_CalculatesCurveControlSets_WhenCartridgeFoundInInstrument()
    {
        // Arrange
        var methodName = "TestMethod";
        var articleType = ArticleType.CurveControlCartridge;
        var reagentInfo = CreateReagentInfo(articleType, "Curve Control Cartridge", "CC_Identity", methodName: methodName);

        var cells = new List<CartridgeCell>
        {
            CreateCartridgeCell(1, isPierced: false),
            CreateCartridgeCell(2, isPierced: false),
            CreateCartridgeCell(3, isPierced: false),
            CreateCartridgeCell(4, isPierced: false),
            CreateCartridgeCell(5, isPierced: false),
            CreateCartridgeCell(6, isPierced: false),
            CreateCartridgeCell(7, isPierced: false),
            CreateCartridgeCell(8, isPierced: false),
            CreateCartridgeCell(9, isPierced: true),
            CreateCartridgeCell(10, isPierced: true)
        };

        var cartridge = new Cartridge
        {
            ReagentBarcode = _curveControlBarcode,
            Cells = { cells }
        };

        var barcodeService = new Mock<IReagentBarcodeService>();
        barcodeService
            .Setup(x => x.DetectArticleType(_curveControlBarcode))
            .ReturnsAsync((articleType, null));
        barcodeService
            .Setup(x => x.GetReagentInfo(articleType, _curveControlBarcode, null))
            .ReturnsAsync(reagentInfo);

        var methodsService = new Mock<IMethodsService>();
        methodsService
            .Setup(x => x.GetMethodCurveControlCount(methodName))
            .ReturnsAsync(4); // 4 curve controls per set

        var instrumentReagentStorage = new Mock<IInstrumentReagentStorage>();
        instrumentReagentStorage
            .Setup(x => x.GetCartridges(_testInstrumentNumber))
            .Returns(new List<Cartridge> { cartridge });

        var handler = CreateHandler(
            barcodeService: barcodeService.Object,
            methodsService: methodsService.Object,
            instrumentReagentStorage: instrumentReagentStorage.Object);

        var query = new GetReagentContentDetailQuery(_curveControlBarcode, null, _testInstrumentNumber);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Quantity.Should().Be(2); // 8 unpierced cells / 4 cells per set = 2 sets
        result.UnitOfMeasure.Should().Be(ArticleQuantityUnit.Sets);
    }

    [Fact]
    public async Task Handle_ReturnsZeroSets_WhenCartridgeNotFoundInInstrument()
    {
        // Arrange
        var articleType = ArticleType.CalibratorCartridge;
        var reagentInfo = CreateReagentInfo(articleType, "Calibrator Cartridge", "CAL_Identity", methodName: "TestMethod");

        var barcodeService = new Mock<IReagentBarcodeService>();
        barcodeService
            .Setup(x => x.DetectArticleType(_calibratorBarcode))
            .ReturnsAsync((articleType, null));
        barcodeService
            .Setup(x => x.GetReagentInfo(articleType, _calibratorBarcode, null))
            .ReturnsAsync(reagentInfo);

        var instrumentReagentStorage = new Mock<IInstrumentReagentStorage>();
        instrumentReagentStorage
            .Setup(x => x.GetCartridges(_testInstrumentNumber))
            .Returns(new List<Cartridge>()); // No cartridges

        var handler = CreateHandler(
            barcodeService: barcodeService.Object,
            instrumentReagentStorage: instrumentReagentStorage.Object);

        var query = new GetReagentContentDetailQuery(_calibratorBarcode, null, _testInstrumentNumber);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Quantity.Should().Be(0);
        result.UnitOfMeasure.Should().Be(ArticleQuantityUnit.Sets);
    }

    [Fact]
    public async Task Handle_ReturnsZeroSets_WhenBarcodeIsNullForSpecialCartridges()
    {
        // Arrange
        var articleType = ArticleType.CalibratorCartridge;
        var reagentUdi = "|CAL_UDI|";
        var reagentInfo = CreateReagentInfo(articleType, "Calibrator Cartridge", "CAL_Identity", methodName: "TestMethod");

        var barcodeService = new Mock<IReagentBarcodeService>();
        barcodeService
            .Setup(x => x.DetectArticleType(reagentUdi))
            .ReturnsAsync((articleType, null));
        barcodeService
            .Setup(x => x.GetReagentInfo(articleType, null, reagentUdi))
            .ReturnsAsync(reagentInfo);

        var handler = CreateHandler(barcodeService: barcodeService.Object);
        var query = new GetReagentContentDetailQuery(null, reagentUdi, _testInstrumentNumber);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Quantity.Should().Be(0);
        result.UnitOfMeasure.Should().Be(ArticleQuantityUnit.Sets);
    }

    [Fact]
    public async Task Handle_ReturnsZeroSets_WhenMethodNameIsEmpty()
    {
        // Arrange
        var articleType = ArticleType.CurveControlCartridge;
        var reagentInfo = CreateReagentInfo(articleType, "Curve Control Cartridge", "CC_Identity", methodName: "");

        var cells = new List<CartridgeCell>
        {
            CreateCartridgeCell(1, isPierced: false),
            CreateCartridgeCell(2, isPierced: false)
        };

        var cartridge = new Cartridge
        {
            ReagentBarcode = _curveControlBarcode,
            Cells = { cells }
        };

        var barcodeService = new Mock<IReagentBarcodeService>();
        barcodeService
            .Setup(x => x.DetectArticleType(_curveControlBarcode))
            .ReturnsAsync((articleType, null));
        barcodeService
            .Setup(x => x.GetReagentInfo(articleType, _curveControlBarcode, null))
            .ReturnsAsync(reagentInfo);

        var instrumentReagentStorage = new Mock<IInstrumentReagentStorage>();
        instrumentReagentStorage
            .Setup(x => x.GetCartridges(_testInstrumentNumber))
            .Returns(new List<Cartridge> { cartridge });

        var handler = CreateHandler(
            barcodeService: barcodeService.Object,
            instrumentReagentStorage: instrumentReagentStorage.Object);

        var query = new GetReagentContentDetailQuery(_curveControlBarcode, null, _testInstrumentNumber);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Quantity.Should().Be(0);
        result.UnitOfMeasure.Should().Be(ArticleQuantityUnit.Sets);
    }

    [Fact]
    public async Task Handle_ReturnsZeroSets_WhenCurveControlCountIsZero()
    {
        // Arrange
        var methodName = "TestMethod";
        var articleType = ArticleType.CurveControlCartridge;
        var reagentInfo = CreateReagentInfo(articleType, "Curve Control Cartridge", "CC_Identity", methodName: methodName);

        var cells = new List<CartridgeCell>
        {
            CreateCartridgeCell(1, isPierced: false),
            CreateCartridgeCell(2, isPierced: false)
        };

        var cartridge = new Cartridge
        {
            ReagentBarcode = _curveControlBarcode,
            Cells = { cells }
        };

        var barcodeService = new Mock<IReagentBarcodeService>();
        barcodeService
            .Setup(x => x.DetectArticleType(_curveControlBarcode))
            .ReturnsAsync((articleType, null));
        barcodeService
            .Setup(x => x.GetReagentInfo(articleType, _curveControlBarcode, null))
            .ReturnsAsync(reagentInfo);

        var methodsService = new Mock<IMethodsService>();
        methodsService
            .Setup(x => x.GetMethodCurveControlCount(methodName))
            .ReturnsAsync(0);

        var instrumentReagentStorage = new Mock<IInstrumentReagentStorage>();
        instrumentReagentStorage
            .Setup(x => x.GetCartridges(_testInstrumentNumber))
            .Returns(new List<Cartridge> { cartridge });

        var handler = CreateHandler(
            barcodeService: barcodeService.Object,
            methodsService: methodsService.Object,
            instrumentReagentStorage: instrumentReagentStorage.Object);

        var query = new GetReagentContentDetailQuery(_curveControlBarcode, null, _testInstrumentNumber);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Quantity.Should().Be(0);
        result.UnitOfMeasure.Should().Be(ArticleQuantityUnit.Sets);
    }

    [Fact]
    public async Task Handle_CalculatesPartialSets_ForCalibrators()
    {
        // Arrange
        var articleType = ArticleType.CalibratorCartridge;
        var reagentInfo = CreateReagentInfo(articleType, "Calibrator Cartridge", "CAL_Identity", methodName: "TestMethod");

        var cells = new List<CartridgeCell>
        {
            CreateCartridgeCell(1, isPierced: false),
            CreateCartridgeCell(2, isPierced: false),
            CreateCartridgeCell(3, isPierced: false),
            CreateCartridgeCell(4, isPierced: false),
            CreateCartridgeCell(5, isPierced: false),
            CreateCartridgeCell(6, isPierced: false),
            CreateCartridgeCell(7, isPierced: false),
            CreateCartridgeCell(8, isPierced: false) // 8 unpierced cells = 1 set (6 cells) + 2 partial
        };

        var cartridge = new Cartridge
        {
            ReagentBarcode = _calibratorBarcode,
            Cells = { cells }
        };

        var barcodeService = new Mock<IReagentBarcodeService>();
        barcodeService
            .Setup(x => x.DetectArticleType(_calibratorBarcode))
            .ReturnsAsync((articleType, null));
        barcodeService
            .Setup(x => x.GetReagentInfo(articleType, _calibratorBarcode, null))
            .ReturnsAsync(reagentInfo);

        var instrumentReagentStorage = new Mock<IInstrumentReagentStorage>();
        instrumentReagentStorage
            .Setup(x => x.GetCartridges(_testInstrumentNumber))
            .Returns(new List<Cartridge> { cartridge });

        var handler = CreateHandler(
            barcodeService: barcodeService.Object,
            instrumentReagentStorage: instrumentReagentStorage.Object);

        var query = new GetReagentContentDetailQuery(_calibratorBarcode, null, _testInstrumentNumber);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Quantity.Should().Be(1); // Floor(8/6) = 1
        result.UnitOfMeasure.Should().Be(ArticleQuantityUnit.Sets);
    }

    private static GetReagentContentDetailQueryHandler CreateHandler(
        List<Reagent>? reagents = null,
        IReagentBarcodeService? barcodeService = null,
        IMethodsService? methodsService = null,
        IInstrumentReagentStorage? instrumentReagentStorage = null)
    {
        var dbContext = new Mock<IReagentsDbContext>();
        dbContext.Setup(x => x.Reagents).ReturnsDbSet(reagents ?? new List<Reagent>());

        var mockBarcodeService = barcodeService ?? new Mock<IReagentBarcodeService>().Object;
        var mockMethodsService = methodsService ?? new Mock<IMethodsService>().Object;
        
        var mockInstrumentReagentStorage = instrumentReagentStorage;
        if (mockInstrumentReagentStorage == null)
        {
            var mock = new Mock<IInstrumentReagentStorage>();
            mock.Setup(x => x.GetCartridges(It.IsAny<string>())).Returns(() => Array.Empty<Cartridge>());
            mockInstrumentReagentStorage = mock.Object;
        }

        return new GetReagentContentDetailQueryHandler(
            dbContext.Object,
            mockBarcodeService,
            mockMethodsService,
            mockInstrumentReagentStorage);
    }

    private static ReagentInfo CreateReagentInfo(
        ArticleType articleType,
        string friendlyName,
        string identity,
        string lotNumber = "LOT123",
        int quantity = 1000,
        DateTimeOffset? expiry = null,
        Color? color1 = null,
        Color? color2 = null,
        string? methodName = null)
    {
        var article = new Article
        {
            ArticleType = articleType,
            FriendlyName = friendlyName,
            Identity = identity,
            Quantity = quantity,
            Color1 = color1,
            Color2 = color2,
            MethodName = methodName
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

    private static CartridgeCell CreateCartridgeCell(int position, bool isPierced)
    {
        return new CartridgeCell
        {
            Position = position,
            Content = new Tetris.Protocol.Common.ReagentContent
            {
                OpenTimestamp = isPierced
                    ? Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow)
                    : Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.MinValue.ToUniversalTime())
            }
        };
    }
}
