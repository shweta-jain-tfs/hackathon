using Moq.EntityFrameworkCore;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Parameters;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Parameters.Application.Interfaces;
using Pharos.Server.Parameters.Application.Services;

namespace Pharos.Server.Parameters.Application.Queries.GetQualityControls;

public class IsQualityControlQueryHandlerTests
{
    private readonly Mock<IParametersDbContext> _mockDbContext;
    private readonly Mock<IIsQualityControlNeededService> _mockIsNeededService;
    private readonly IsQualityControlQueryHandler _sut;

    public IsQualityControlQueryHandlerTests()
    {
        _mockDbContext = new Mock<IParametersDbContext>();
        _mockIsNeededService = new Mock<IIsQualityControlNeededService>();
        _sut = new IsQualityControlQueryHandler(_mockDbContext.Object, _mockIsNeededService.Object);
    }

    [Fact]
    public async Task Handle_ReturnsNotQualityControl_WhenNoMatchingQualityControl()
    {
        // Arrange
        var query = new IsQualityControlQuery("1234567890", "Instrument1");

        _mockDbContext.Setup(db => db.LabQualityControls).ReturnsDbSet(new List<LabQualityControl>());
        _mockDbContext.Setup(db => db.QualityControlBarcodeFormats).ReturnsDbSet(new List<QualityControlBarcodeFormat>());

        // Act
        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsQualityControl.Should().BeFalse();
        result.QualityControlIdentity.Should().BeEmpty();
        result.IsNeeded.Should().BeFalse();
        result.LotNumber.Should().BeEmpty();
        result.TubeTypeId.Should().Be(-1);
    }

    [Fact]
    public async Task Handle_ReturnsQualityControl_WhenMatchingWithoutBarcodeFormat()
    {
        // Arrange
        var query = new IsQualityControlQuery("QC123", "Instrument1");
        var method = new Method
        {
            Id = 1,
            MethodName = "sIgE"
        };
        var methodTest = new MethodTest
        {
            Id = 1,
            Method = method,
            TestName = "t1"
        };
        var test = new LabQualityControlTest
        {
            Id = 1,
            Test = methodTest
        };
        var labQualityControls = new List<LabQualityControl>
        {
            new()
            {
                Id = 1,
                Name = "QC Test",
                IdOnBarcode = "QC123",
                BarcodeFormatId = null,
                TubeTypeId = 1,
                Tests = new List<BaseQualityControlTest>
                {
                    test
                }
            }
        };

        _mockDbContext.Setup(db => db.LabQualityControls).ReturnsDbSet(labQualityControls);
        _mockDbContext.Setup(db => db.QualityControlBarcodeFormats).ReturnsDbSet(new List<QualityControlBarcodeFormat>());
        _mockIsNeededService.Setup(s => s.IsQualityControlNeeded(It.IsAny<LabQualityControl>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QualityControlNeededDto(true, 1));

        // Act
        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsQualityControl.Should().BeTrue();
        result.QualityControlIdentity.Should().Be("QC Test");
        result.IsNeeded.Should().BeTrue();
        result.LotNumber.Should().BeEmpty();
        result.TubeTypeId.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ReturnsFirstMatch_WhenMultipleQualityControlsWithoutBarcodeFormat()
    {
        // Arrange
        var query = new IsQualityControlQuery("QC123", "Instrument1");
        var method = new Method
        {
            Id = 1,
            MethodName = "tIgE"
        };
        var methodTest = new MethodTest
        {
            Id = 1,
            Method = method,
            TestName = "IgE"
        };
        var test = new LabQualityControlTest
        {
            Id = 1,
            Test = methodTest
        };
        var labQualityControls = new List<LabQualityControl>
        {
            new()
            {
                Id = 1,
                Name = "QC Test 1",
                IdOnBarcode = "QC123",
                BarcodeFormatId = null,
                TubeTypeId = 2,
                Tests = new List<BaseQualityControlTest>
                {
                    test
                }
            },
            new()
            {
                Id = 2,
                Name = "QC Test 2",
                IdOnBarcode = "QC123",
                BarcodeFormatId = null,
                TubeTypeId = 2,
                Tests = new List<BaseQualityControlTest>
                {
                    test
                }
            }
        };

        _mockDbContext.Setup(db => db.LabQualityControls).ReturnsDbSet(labQualityControls);
        _mockDbContext.Setup(db => db.QualityControlBarcodeFormats).ReturnsDbSet(new List<QualityControlBarcodeFormat>());
        _mockIsNeededService.Setup(s => s.IsQualityControlNeeded(It.IsAny<LabQualityControl>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QualityControlNeededDto(true, 2));

        // Act
        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsQualityControl.Should().BeTrue();
        result.QualityControlIdentity.Should().Be("QC Test 1");
        result.IsNeeded.Should().BeTrue();
        result.LotNumber.Should().BeEmpty();
        result.TubeTypeId.Should().Be(2);
    }

    [Fact]
    public async Task Handle_ReturnsQualityControl_WhenMatchingWithBarcodeFormat()
    {
        // Arrange
        var query = new IsQualityControlQuery("QC12345LOT67", "Instrument1");
        var method = new Method
        {
            Id = 1,
            MethodName = "El-G"
        };
        var methodTest = new MethodTest
        {
            Id = 1,
            Method = method,
            TestName = "dn"
        };
        var test = new LabQualityControlTest
        {
            Id = 1,
            Test = methodTest
        };
        var barcodeFormats = new List<QualityControlBarcodeFormat>
        {
            new()
            {
                Id = 1,
                Name = "Format1",
                TotalLength = 12,
                QcStartPosition = 1,
                QcLength = 7,
                ContainsLotInformation = true,
                LotStartPosition = 8,
                LotLength = 5
            }
        };
        var labQualityControls = new List<LabQualityControl>
        {
            new()
            {
                Id = 1,
                Name = "QC Test",
                IdOnBarcode = "QC12345",
                BarcodeFormatId = 1,
                TubeTypeId = 3,
                Tests = new List<BaseQualityControlTest>
                {
                    test
                }
            }
        };

        _mockDbContext.Setup(db => db.LabQualityControls).ReturnsDbSet(labQualityControls);
        _mockDbContext.Setup(db => db.QualityControlBarcodeFormats).ReturnsDbSet(barcodeFormats);
        _mockIsNeededService.Setup(s => s.IsQualityControlNeeded(It.IsAny<LabQualityControl>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QualityControlNeededDto(true, 3));

        // Act
        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsQualityControl.Should().BeTrue();
        result.QualityControlIdentity.Should().Be("QC Test");
        result.IsNeeded.Should().BeTrue();
        result.LotNumber.Should().Be("LOT67");
        result.TubeTypeId.Should().Be(3);
    }

    [Fact]
    public async Task Handle_ReturnsQualityControl_WithEmptyLotNumber_WhenBarcodeFormatDoesNotContainLot()
    {
        // Arrange
        var query = new IsQualityControlQuery("QC12345EXTRA", "Instrument1");
        var method = new Method
        {
            Id = 1,
            MethodName = "sIgE"
        };
        var methodTest = new MethodTest
        {
            Id = 1,
            Method = method,
            TestName = "t1"
        };
        var test = new LabQualityControlTest
        {
            Id = 1,
            Test = methodTest
        };
        var barcodeFormats = new List<QualityControlBarcodeFormat>
        {
            new()
            {
                Id = 1,
                Name = "Format1",
                TotalLength = 12,
                QcStartPosition = 1,
                QcLength = 7,
                ContainsLotInformation = false,
                LotStartPosition = 0,
                LotLength = 0
            }
        };
        var labQualityControls = new List<LabQualityControl>
        {
            new()
            {
                Id = 1,
                Name = "QC Test",
                IdOnBarcode = "QC12345",
                BarcodeFormatId = 1,
                TubeTypeId = 4,
                Tests = new List<BaseQualityControlTest>
                {
                    test
                }
            }
        };

        _mockDbContext.Setup(db => db.LabQualityControls).ReturnsDbSet(labQualityControls);
        _mockDbContext.Setup(db => db.QualityControlBarcodeFormats).ReturnsDbSet(barcodeFormats);
        _mockIsNeededService.Setup(s => s.IsQualityControlNeeded(It.IsAny<LabQualityControl>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QualityControlNeededDto(true, 4));

        // Act
        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsQualityControl.Should().BeTrue();
        result.QualityControlIdentity.Should().Be("QC Test");
        result.IsNeeded.Should().BeTrue();
        result.LotNumber.Should().BeEmpty();
        result.TubeTypeId.Should().Be(4);
    }

    [Fact]
    public async Task Handle_IgnoresBarcodeFormat_WhenLengthDoesNotMatch()
    {
        // Arrange
        var query = new IsQualityControlQuery("SHORT", "Instrument1");
        var barcodeFormats = new List<QualityControlBarcodeFormat>
        {
            new()
            {
                Id = 1,
                Name = "Format1",
                TotalLength = 12,
                QcStartPosition = 1,
                QcLength = 7,
                ContainsLotInformation = false
            }
        };
        var labQualityControls = new List<LabQualityControl>
        {
            new()
            {
                Id = 1,
                Name = "QC Test",
                IdOnBarcode = "SHORT",
                BarcodeFormatId = 1,
                TubeTypeId = 1
            }
        };

        _mockDbContext.Setup(db => db.LabQualityControls).ReturnsDbSet(labQualityControls);
        _mockDbContext.Setup(db => db.QualityControlBarcodeFormats).ReturnsDbSet(barcodeFormats);

        // Act
        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsQualityControl.Should().BeFalse();
        result.QualityControlIdentity.Should().BeEmpty();
        result.IsNeeded.Should().BeFalse();
        result.LotNumber.Should().BeEmpty();
        result.TubeTypeId.Should().Be(-1);
    }

    [Fact]
    public async Task Handle_PrioritizesQualityControlsWithoutBarcodeFormat()
    {
        // Arrange
        var query = new IsQualityControlQuery("QCTEST123456", "Instrument1");
        var method = new Method
        {
            Id = 1,
            MethodName = "tIgE"
        };
        var methodTest = new MethodTest
        {
            Id = 1,
            Method = method,
            TestName = "IgE"
        };
        var test = new LabQualityControlTest
        {
            Id = 1,
            Test = methodTest
        };
        var barcodeFormats = new List<QualityControlBarcodeFormat>
        {
            new()
            {
                Id = 1,
                Name = "Format1",
                TotalLength = 12,
                QcStartPosition = 1,
                QcLength = 6,
                ContainsLotInformation = true,
                LotStartPosition = 7,
                LotLength = 6
            }
        };
        var labQualityControls = new List<LabQualityControl>
        {
            new()
            {
                Id = 1,
                Name = "QC Without Format",
                IdOnBarcode = "QCTEST123456",
                BarcodeFormatId = null,
                TubeTypeId = 5,
                Tests = new List<BaseQualityControlTest>
                {
                    test
                }
            },
            new()
            {
                Id = 2,
                Name = "QC With Format",
                IdOnBarcode = "QCTEST",
                BarcodeFormatId = 1,
                TubeTypeId = 5,
                Tests = new List<BaseQualityControlTest>
                {
                    test
                }
            }
        };

        _mockDbContext.Setup(db => db.LabQualityControls).ReturnsDbSet(labQualityControls);
        _mockDbContext.Setup(db => db.QualityControlBarcodeFormats).ReturnsDbSet(barcodeFormats);
        _mockIsNeededService.Setup(s => s.IsQualityControlNeeded(It.IsAny<LabQualityControl>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QualityControlNeededDto(true, 5));

        // Act
        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsQualityControl.Should().BeTrue();
        result.QualityControlIdentity.Should().Be("QC Without Format");
        result.IsNeeded.Should().BeTrue();
        result.LotNumber.Should().BeEmpty();
        result.TubeTypeId.Should().Be(5);
    }

    [Fact]
    public async Task Handle_ExtractsCorrectQcPartFromBarcode()
    {
        // Arrange
        var query = new IsQualityControlQuery("PREFIXQC123SUFFIX", "Instrument1");
        var method = new Method
        {
            Id = 1,
            MethodName = "El-G"
        };
        var methodTest = new MethodTest
        {
            Id = 1,
            Method = method,
            TestName = "dn"
        };
        var test = new LabQualityControlTest
        {
            Id = 1,
            Test = methodTest
        };
        var barcodeFormats = new List<QualityControlBarcodeFormat>
        {
            new()
            {
                Id = 1,
                Name = "Format1",
                TotalLength = 17,
                QcStartPosition = 7,
                QcLength = 5,
                ContainsLotInformation = false
            }
        };
        var labQualityControls = new List<LabQualityControl>
        {
            new()
            {
                Id = 1,
                Name = "QC Test",
                IdOnBarcode = "QC123",
                BarcodeFormatId = 1,
                TubeTypeId = 6,
                Tests = new List<BaseQualityControlTest>
                {
                    test
                }
            }
        };

        _mockDbContext.Setup(db => db.LabQualityControls).ReturnsDbSet(labQualityControls);
        _mockDbContext.Setup(db => db.QualityControlBarcodeFormats).ReturnsDbSet(barcodeFormats);
        _mockIsNeededService.Setup(s => s.IsQualityControlNeeded(It.IsAny<LabQualityControl>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QualityControlNeededDto(true, 6));

        // Act
        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsQualityControl.Should().BeTrue();
        result.QualityControlIdentity.Should().Be("QC Test");
        result.IsNeeded.Should().BeTrue();
        result.LotNumber.Should().BeEmpty();
        result.TubeTypeId.Should().Be(6);
    }

    [Fact]
    public async Task Handle_ReturnsNotQualityControl_WhenQcPartDoesNotMatch()
    {
        // Arrange
        var query = new IsQualityControlQuery("QC99999LOT67", "Instrument1");
        var barcodeFormats = new List<QualityControlBarcodeFormat>
        {
            new()
            {
                Id = 1,
                Name = "Format1",
                TotalLength = 12,
                QcStartPosition = 1,
                QcLength = 7,
                ContainsLotInformation = true,
                LotStartPosition = 8,
                LotLength = 5
            }
        };
        var labQualityControls = new List<LabQualityControl>
        {
            new()
            {
                Id = 1,
                Name = "QC Test",
                IdOnBarcode = "QC12345",
                BarcodeFormatId = 1,
                TubeTypeId = 1
            }
        };

        _mockDbContext.Setup(db => db.LabQualityControls).ReturnsDbSet(labQualityControls);
        _mockDbContext.Setup(db => db.QualityControlBarcodeFormats).ReturnsDbSet(barcodeFormats);

        // Act
        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsQualityControl.Should().BeFalse();
        result.QualityControlIdentity.Should().BeEmpty();
        result.IsNeeded.Should().BeFalse();
        result.LotNumber.Should().BeEmpty();
        result.TubeTypeId.Should().Be(-1);
    }

    [Fact]
    public async Task Handle_HandlesMultipleBarcodeFormats()
    {
        // Arrange
        var query = new IsQualityControlQuery("QC789012", "Instrument1");
        var method = new Method
        {
            Id = 1,
            MethodName = "sIgE"
        };
        var methodTest = new MethodTest
        {
            Id = 1,
            Method = method,
            TestName = "t1"
        };
        var test = new LabQualityControlTest
        {
            Id = 1,
            Test = methodTest
        };
        var barcodeFormats = new List<QualityControlBarcodeFormat>
        {
            new()
            {
                Id = 1,
                Name = "Format1",
                TotalLength = 8,
                QcStartPosition = 1,
                QcLength = 5,
                ContainsLotInformation = false
            },
            new()
            {
                Id = 2,
                Name = "Format2",
                TotalLength = 8,
                QcStartPosition = 1,
                QcLength = 8,
                ContainsLotInformation = false
            }
        };
        var labQualityControls = new List<LabQualityControl>
        {
            new()
            {
                Id = 1,
                Name = "QC Test",
                IdOnBarcode = "QC789012",
                BarcodeFormatId = 2,
                TubeTypeId = 7,
                Tests = new List<BaseQualityControlTest>
                {
                    test
                }
            }
        };

        _mockDbContext.Setup(db => db.LabQualityControls).ReturnsDbSet(labQualityControls);
        _mockDbContext.Setup(db => db.QualityControlBarcodeFormats).ReturnsDbSet(barcodeFormats);
        _mockIsNeededService.Setup(s => s.IsQualityControlNeeded(It.IsAny<LabQualityControl>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QualityControlNeededDto(true, 7));

        // Act
        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsQualityControl.Should().BeTrue();
        result.QualityControlIdentity.Should().Be("QC Test");
        result.IsNeeded.Should().BeTrue();
        result.LotNumber.Should().BeEmpty();
        result.TubeTypeId.Should().Be(7);
    }

    [Fact]
    public async Task Handle_ReturnsPhadiaQualityControl_WhenBarcodeIsLength7AndArticleIsQualityControlTube()
    {
        // Arrange
        var query = new IsQualityControlQuery("ABC1234", "Instrument1");
        var articles = new List<Article>
        {
            new()
            {
                Id = 1,
                ArticleNumber = "ABC",
                ArticleType = ArticleType.QualityControlTube,
                Identity = "QC-High"
            }
        };
        var method = new Method
        {
            Id = 1,
            MethodName = "sIgE"
        };
        var methodTest = new MethodTest
        {
            Id = 1,
            Method = method,
            TestName = "t1"
        };
        var phadiaQcTests = new List<PhadiaQualityControlTest>
        {
            new()
            {
                Id = 1,
                QualityControl = new PhadiaQualityControl { Name = "QC-High" },
                Test = methodTest,
                TestId = 1
            }
        };

        _mockDbContext.Setup(db => db.Articles).ReturnsDbSet(articles);
        _mockDbContext.Setup(db => db.PhadiaQualityControlTests).ReturnsDbSet(phadiaQcTests);
        _mockIsNeededService.Setup(s => s.IsQualityControlNeeded(It.IsAny<HashSet<string>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QualityControlNeededDto(true, 1));

        // Act
        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsQualityControl.Should().BeTrue();
        result.QualityControlIdentity.Should().Be("QC-High");
        result.IsNeeded.Should().BeTrue();
        result.LotNumber.Should().Be("34");
        result.TubeTypeId.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ReturnsNotQualityControl_WhenBarcodeIsLength7ButNoMatchingArticle()
    {
        // Arrange
        var query = new IsQualityControlQuery("XYZ1234", "Instrument1");
        var articles = new List<Article>();

        _mockDbContext.Setup(db => db.Articles).ReturnsDbSet(articles);
        _mockDbContext.Setup(db => db.PhadiaQualityControlTests).ReturnsDbSet(new List<PhadiaQualityControlTest>());
        _mockDbContext.Setup(db => db.LabQualityControls).ReturnsDbSet(new List<LabQualityControl>());
        _mockDbContext.Setup(db => db.QualityControlBarcodeFormats).ReturnsDbSet(new List<QualityControlBarcodeFormat>());

        // Act
        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsQualityControl.Should().BeFalse();
        result.QualityControlIdentity.Should().BeEmpty();
        result.IsNeeded.Should().BeFalse();
        result.LotNumber.Should().BeEmpty();
        result.TubeTypeId.Should().Be(-1);
    }

    [Fact]
    public async Task Handle_ReturnsNotQualityControl_WhenBarcodeIsLength7ButArticleIsNotQualityControlTube()
    {
        // Arrange
        var query = new IsQualityControlQuery("ABC1234", "Instrument1");
        var articles = new List<Article>
        {
            new()
            {
                Id = 1,
                ArticleNumber = "ABC",
                ArticleType = ArticleType.ConjugateCartridge,
                Identity = "Conjugate-1"
            }
        };

        _mockDbContext.Setup(db => db.Articles).ReturnsDbSet(articles);
        _mockDbContext.Setup(db => db.PhadiaQualityControlTests).ReturnsDbSet(new List<PhadiaQualityControlTest>());
        _mockDbContext.Setup(db => db.LabQualityControls).ReturnsDbSet(new List<LabQualityControl>());
        _mockDbContext.Setup(db => db.QualityControlBarcodeFormats).ReturnsDbSet(new List<QualityControlBarcodeFormat>());

        // Act
        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsQualityControl.Should().BeFalse();
        result.QualityControlIdentity.Should().BeEmpty();
        result.IsNeeded.Should().BeFalse();
        result.LotNumber.Should().BeEmpty();
        result.TubeTypeId.Should().Be(-1);
    }

    [Fact]
    public async Task Handle_ExtractsLotNumberFromLast2Characters_ForPhadiaQualityControl()
    {
        // Arrange
        var query = new IsQualityControlQuery("ABC9876", "Instrument1");
        var articles = new List<Article>
        {
            new()
            {
                Id = 1,
                ArticleNumber = "ABC",
                ArticleType = ArticleType.QualityControlTube,
                Identity = "QC-Low"
            }
        };
        var method = new Method
        {
            Id = 1,
            MethodName = "tIgE"
        };
        var methodTest = new MethodTest
        {
            Id = 1,
            Method = method,
            TestName = "IgE"
        };
        var phadiaQcTests = new List<PhadiaQualityControlTest>
        {
            new()
            {
                Id = 1,
                QualityControl = new PhadiaQualityControl { Name = "QC-Low" },
                Test = methodTest,
                TestId = 1
            }
        };

        _mockDbContext.Setup(db => db.Articles).ReturnsDbSet(articles);
        _mockDbContext.Setup(db => db.PhadiaQualityControlTests).ReturnsDbSet(phadiaQcTests);
        _mockIsNeededService.Setup(s => s.IsQualityControlNeeded(It.IsAny<HashSet<string>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QualityControlNeededDto(true, 1));

        // Act
        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsQualityControl.Should().BeTrue();
        result.QualityControlIdentity.Should().Be("QC-Low");
        result.LotNumber.Should().Be("76");
        result.TubeTypeId.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ReturnsPhadiaQcNeeded_WhenInstrumentHasActiveMethodMatchingQcTests()
    {
        // Arrange
        var query = new IsQualityControlQuery("QC12345", "Instrument1");
        var articles = new List<Article>
        {
            new()
            {
                Id = 1,
                ArticleNumber = "QC1",
                ArticleType = ArticleType.QualityControlTube,
                Identity = "QC-Multi"
            }
        };
        var method1 = new Method
        {
            Id = 1,
            MethodName = "sIgE"
        };
        var method2 = new Method
        {
            Id = 2,
            MethodName = "tIgE"
        };
        var methodTest1 = new MethodTest
        {
            Id = 1,
            Method = method1,
            TestName = "t1"
        };
        var methodTest2 = new MethodTest
        {
            Id = 2,
            Method = method2,
            TestName = "IgE"
        };
        var phadiaQcTests = new List<PhadiaQualityControlTest>
        {
            new()
            {
                Id = 1,
                QualityControl = new PhadiaQualityControl { Name = "QC-Multi" },
                Test = methodTest1,
                TestId = 1
            },
            new()
            {
                Id = 2,
                QualityControl = new PhadiaQualityControl { Name = "QC-Multi" },
                Test = methodTest2,
                TestId = 2
            }
        };

        _mockDbContext.Setup(db => db.Articles).ReturnsDbSet(articles);
        _mockDbContext.Setup(db => db.PhadiaQualityControlTests).ReturnsDbSet(phadiaQcTests);
        _mockIsNeededService.Setup(s => s.IsQualityControlNeeded(It.IsAny<HashSet<string>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QualityControlNeededDto(true, 1));

        // Act
        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsQualityControl.Should().BeTrue();
        result.QualityControlIdentity.Should().Be("QC-Multi");
        result.IsNeeded.Should().BeTrue();
        result.TubeTypeId.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ReturnsPhadiaQcNotNeeded_WhenInstrumentHasNoActiveMethodMatchingQcTests()
    {
        // Arrange
        var query = new IsQualityControlQuery("QC12345", "Instrument1");
        var articles = new List<Article>
        {
            new()
            {
                Id = 1,
                ArticleNumber = "QC1",
                ArticleType = ArticleType.QualityControlTube,
                Identity = "QC-Test"
            }
        };
        var method = new Method
        {
            Id = 1,
            MethodName = "sIgE"
        };
        var methodTest = new MethodTest
        {
            Id = 1,
            Method = method,
            TestName = "t1"
        };
        var phadiaQcTests = new List<PhadiaQualityControlTest>
        {
            new()
            {
                Id = 1,
                QualityControl = new PhadiaQualityControl { Name = "QC-Test" },
                Test = methodTest,
                TestId = 1
            }
        };

        _mockDbContext.Setup(db => db.Articles).ReturnsDbSet(articles);
        _mockDbContext.Setup(db => db.PhadiaQualityControlTests).ReturnsDbSet(phadiaQcTests);
        _mockIsNeededService.Setup(s => s.IsQualityControlNeeded(It.IsAny<HashSet<string>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QualityControlNeededDto(false, 1));

        // Act
        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsQualityControl.Should().BeTrue();
        result.QualityControlIdentity.Should().Be("QC-Test");
        result.IsNeeded.Should().BeFalse();
        result.TubeTypeId.Should().Be(1);
    }

    [Fact]
    public async Task Handle_PrioritizesPhadiaQualityControl_OverLabQualityControl()
    {
        // Arrange
        var query = new IsQualityControlQuery("QC12345", "Instrument1");
        var articles = new List<Article>
        {
            new()
            {
                Id = 1,
                ArticleNumber = "QC1",
                ArticleType = ArticleType.QualityControlTube,
                Identity = "Phadia-QC"
            }
        };
        var method = new Method
        {
            Id = 1,
            MethodName = "sIgE"
        };
        var methodTest = new MethodTest
        {
            Id = 1,
            Method = method,
            TestName = "t1"
        };
        var phadiaQcTests = new List<PhadiaQualityControlTest>
        {
            new()
            {
                Id = 1,
                QualityControl = new PhadiaQualityControl { Name = "Phadia-QC" },
                Test = methodTest,
                TestId = 1
            }
        };
        var labQualityControls = new List<LabQualityControl>
        {
            new()
            {
                Id = 1,
                Name = "Lab QC",
                IdOnBarcode = "QC12345",
                BarcodeFormatId = null,
                TubeTypeId = 1
            }
        };

        _mockDbContext.Setup(db => db.Articles).ReturnsDbSet(articles);
        _mockDbContext.Setup(db => db.PhadiaQualityControlTests).ReturnsDbSet(phadiaQcTests);
        _mockDbContext.Setup(db => db.LabQualityControls).ReturnsDbSet(labQualityControls);
        _mockDbContext.Setup(db => db.QualityControlBarcodeFormats).ReturnsDbSet(new List<QualityControlBarcodeFormat>());
        _mockIsNeededService.Setup(s => s.IsQualityControlNeeded(It.IsAny<HashSet<string>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QualityControlNeededDto(true, 1));

        // Act
        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsQualityControl.Should().BeTrue();
        result.QualityControlIdentity.Should().Be("Phadia-QC");
        result.IsNeeded.Should().BeTrue();
        result.LotNumber.Should().Be("45");
        result.TubeTypeId.Should().Be(1);
    }

    [Fact]
    public async Task Handle_SkipsPhadiaLogic_WhenBarcodeIsNot7Characters()
    {
        // Arrange
        var query = new IsQualityControlQuery("QC123456", "Instrument1");
        var method = new Method
        {
            Id = 1,
            MethodName = "sIgE"
        };
        var methodTest = new MethodTest
        {
            Id = 1,
            Method = method,
            TestName = "t1"
        };
        var test = new LabQualityControlTest
        {
            Id = 1,
            Test = methodTest
        };
        var articles = new List<Article>
        {
            new()
            {
                Id = 1,
                ArticleNumber = "QC1",
                ArticleType = ArticleType.QualityControlTube,
                Identity = "QC-Test"
            }
        };
        var labQualityControls = new List<LabQualityControl>
        {
            new()
            {
                Id = 1,
                Name = "Lab QC",
                IdOnBarcode = "QC123456",
                BarcodeFormatId = null,
                Tests = new List<BaseQualityControlTest>
                {
                    test
                }
            }
        };

        _mockDbContext.Setup(db => db.Articles).ReturnsDbSet(articles);
        _mockDbContext.Setup(db => db.LabQualityControls).ReturnsDbSet(labQualityControls);
        _mockDbContext.Setup(db => db.QualityControlBarcodeFormats).ReturnsDbSet(new List<QualityControlBarcodeFormat>());
        _mockIsNeededService.Setup(s => s.IsQualityControlNeeded(It.IsAny<LabQualityControl>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QualityControlNeededDto(true, 8));

        // Act
        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsQualityControl.Should().BeTrue();
        result.QualityControlIdentity.Should().Be("Lab QC");
        result.IsNeeded.Should().BeTrue();
        result.LotNumber.Should().BeEmpty();
        result.TubeTypeId.Should().Be(8);
    }

    [Fact]
    public async Task Handle_ReturnsLabQcNotNeeded_WhenInstrumentHasNoActiveMethodMatchingQcTests()
    {
        // Arrange
        var query = new IsQualityControlQuery("LAB-QC-123", "Instrument1");
        var method = new Method
        {
            Id = 1,
            MethodName = "sIgE"
        };
        var methodTest = new MethodTest
        {
            Id = 1,
            Method = method,
            TestName = "t1"
        };
        var test = new LabQualityControlTest
        {
            Id = 1,
            Test = methodTest
        };
        var labQualityControls = new List<LabQualityControl>
        {
            new()
            {
                Id = 1,
                Name = "Lab QC Test",
                IdOnBarcode = "LAB-QC-123",
                BarcodeFormatId = null,
                Tests = new List<BaseQualityControlTest>
                {
                    test
                }
            }
        };

        _mockDbContext.Setup(db => db.LabQualityControls).ReturnsDbSet(labQualityControls);
        _mockDbContext.Setup(db => db.QualityControlBarcodeFormats).ReturnsDbSet(new List<QualityControlBarcodeFormat>());
        _mockIsNeededService.Setup(s => s.IsQualityControlNeeded(It.IsAny<LabQualityControl>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QualityControlNeededDto(false, 9));

        // Act
        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsQualityControl.Should().BeTrue();
        result.QualityControlIdentity.Should().Be("Lab QC Test");
        result.IsNeeded.Should().BeFalse();
        result.LotNumber.Should().BeEmpty();
        result.TubeTypeId.Should().Be(9);
    }

    [Fact]
    public async Task Handle_ReturnsLabQcNeeded_WhenMultipleMethodsAndAnyMatch()
    {
        // Arrange
        var query = new IsQualityControlQuery("QC-MULTI", "Instrument1");
        var method1 = new Method
        {
            Id = 1,
            MethodName = "sIgE"
        };
        var method2 = new Method
        {
            Id = 2,
            MethodName = "tIgE"
        };
        var methodTest1 = new MethodTest
        {
            Id = 1,
            Method = method1,
            TestName = "t1"
        };
        var methodTest2 = new MethodTest
        {
            Id = 2,
            Method = method2,
            TestName = "IgE"
        };
        var test1 = new LabQualityControlTest
        {
            Id = 1,
            Test = methodTest1
        };
        var test2 = new LabQualityControlTest
        {
            Id = 2,
            Test = methodTest2
        };
        var labQualityControls = new List<LabQualityControl>
        {
            new()
            {
                Id = 1,
                Name = "Multi-Method QC",
                IdOnBarcode = "QC-MULTI",
                BarcodeFormatId = null,
                Tests = new List<BaseQualityControlTest>
                {
                    test1,
                    test2
                }
            }
        };

        _mockDbContext.Setup(db => db.LabQualityControls).ReturnsDbSet(labQualityControls);
        _mockDbContext.Setup(db => db.QualityControlBarcodeFormats).ReturnsDbSet(new List<QualityControlBarcodeFormat>());
        _mockIsNeededService.Setup(s => s.IsQualityControlNeeded(It.IsAny<LabQualityControl>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QualityControlNeededDto(true, 10));

        // Act
        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsQualityControl.Should().BeTrue();
        result.QualityControlIdentity.Should().Be("Multi-Method QC");
        result.IsNeeded.Should().BeTrue();
        result.LotNumber.Should().BeEmpty();
        result.TubeTypeId.Should().Be(10);
    }

    [Fact]
    public async Task Handle_ReturnsFirstAlphabeticalMatch_WhenMultipleQualityControlsMatchSameBarcodeFormat()
    {
        // Arrange
        var query = new IsQualityControlQuery("QC1QC2LOT67", "Instrument1");
        var method = new Method
        {
            Id = 1,
            MethodName = "sIgE"
        };
        var methodTest = new MethodTest
        {
            Id = 1,
            Method = method,
            TestName = "t1"
        };
        var test = new LabQualityControlTest
        {
            Id = 1,
            Test = methodTest
        };
        var barcodeFormats = new List<QualityControlBarcodeFormat>
        {
            new()
            {
                Id = 1,
                Name = "Format1",
                TotalLength = 11,
                QcStartPosition = 1,
                QcLength = 3,
                ContainsLotInformation = true,
                LotStartPosition = 7,
                LotLength = 5
            },
            new()
            {
                Id = 2,
                Name = "Format2",
                TotalLength = 11,
                QcStartPosition = 4,
                QcLength = 3,
                ContainsLotInformation = true,
                LotStartPosition = 7,
                LotLength = 5
            },
        };
        // Provide controls in reverse alphabetical order to verify OrderBy(Name) is applied
        var labQualityControls = new List<LabQualityControl>
        {
            new()
            {
                Id = 1,
                Name = "ZZ QC Control",
                IdOnBarcode = "QC1",
                BarcodeFormatId = 1,
                TubeTypeId = 5,
                Tests = new List<BaseQualityControlTest> { test }
            },
            new()
            {
                Id = 2,
                Name = "AA QC Control",
                IdOnBarcode = "QC2",
                BarcodeFormatId = 2,
                TubeTypeId = 5,
                Tests = new List<BaseQualityControlTest> { test }
            }
        };

        _mockDbContext.Setup(db => db.LabQualityControls).ReturnsDbSet(labQualityControls);
        _mockDbContext.Setup(db => db.QualityControlBarcodeFormats).ReturnsDbSet(barcodeFormats);
        _mockIsNeededService.Setup(s => s.IsQualityControlNeeded(It.IsAny<LabQualityControl>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QualityControlNeededDto(true, 5));

        // Act
        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.IsQualityControl.Should().BeTrue();
        result.QualityControlIdentity.Should().Be("AA QC Control");
        result.IsNeeded.Should().BeTrue();
        result.LotNumber.Should().Be("LOT67");
        result.TubeTypeId.Should().Be(5);
    }
}

