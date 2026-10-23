using Pharos.Server.Domain.Parameters;
using Pharos.Server.Request.Application.Mapping;
using Tetris.Protocol.Common;
using TestOrder = Pharos.Server.Request.Domain.TestOrder;

namespace Pharos.Server.Request.Application.Commands.TestOrderScheduling;

public class MapperExtensionsTests
{
    private readonly Mock<IArticlesService> _articlesServiceMock;

    public MapperExtensionsTests()
    {
        _articlesServiceMock = new Mock<IArticlesService>();
    }

    [Fact]
    public async Task MapTestOrders_Should_MapBasicTestOrder_When_NoCustomParameters()
    {
        // Arrange
        var request = new Domain.Request
        {
            SampleId = "S1",
            Created = DateTimeOffset.UtcNow
        };

        var testOrder = new TestOrder
        {
            Uid = Guid.NewGuid(),
            MethodName = "Method1",
            TestName = "Test1",
            NumberOfReplicates = 2,
            TestSortOrder = 1,
            Request = request,
            TestOrderCustomParameters = null
        };

        var testOrders = new List<TestOrder> { testOrder };

        // Act
        var result = await TestOrderMapper.MapTestOrders(testOrders, _articlesServiceMock.Object);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(1);
        result[0].LotNumbers.Count.Should().Be(0);
        _articlesServiceMock.Verify(x => x.GetArticle(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task MapTestOrders_Should_AddCarrierLot_When_CarrierLotIsImmunoCapCarrier()
    {
        // Arrange
        var request = new Domain.Request
        {
            SampleId = "S1",
            Created = DateTimeOffset.UtcNow
        };

        var article = new Article
        {
            ArticleNumber = "ABC",
            ArticleType = ArticleType.ImmunoCapCarrier
        };

        var testOrder = new TestOrder
        {
            Uid = Guid.NewGuid(),
            MethodName = "Method1",
            TestName = "Test1",
            Request = request,
            TestOrderCustomParameters = new TestOrderCustomParameter
            {
                CarrierLot = "ABC123456"
            }
        };

        _articlesServiceMock
            .Setup(x => x.GetArticle("ABC"))
            .ReturnsAsync(article);

        var testOrders = new List<TestOrder> { testOrder };

        // Act
        var result = await TestOrderMapper.MapTestOrders(testOrders, _articlesServiceMock.Object);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(1);
        result[0].LotNumbers.Count.Should().Be(1);
        result[0].LotNumbers[0].LotNumber_.Should().Be("ABC123456");
        result[0].LotNumbers[0].ReagentType.Should().Be(EnumReagentType.ImmunoCapCarrier);
        _articlesServiceMock.Verify(x => x.GetArticle("ABC"), Times.Once);
    }

    [Fact]
    public async Task MapTestOrders_Should_AddCarrierLot_When_CarrierLotIsEliaCalibratorWellCarrier()
    {
        // Arrange
        var request = new Domain.Request
        {
            SampleId = "S1",
            Created = DateTimeOffset.UtcNow
        };

        var article = new Article
        {
            ArticleNumber = "DEF",
            ArticleType = ArticleType.EliaCalibratorWellCarrier
        };

        var testOrder = new TestOrder
        {
            Uid = Guid.NewGuid(),
            MethodName = "Method1",
            TestName = "Test1",
            Request = request,
            TestOrderCustomParameters = new TestOrderCustomParameter
            {
                CarrierLot = "DEF123456",
                CustomTestId = "Test1"
            }
        };

        _articlesServiceMock
            .Setup(x => x.GetArticle("DEF"))
            .ReturnsAsync(article);

        var testOrders = new List<TestOrder> { testOrder };

        // Act
        var result = await TestOrderMapper.MapTestOrders(testOrders, _articlesServiceMock.Object);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(1);
        result[0].LotNumbers.Count.Should().Be(1);
        result[0].LotNumbers[0].LotNumber_.Should().Be("DEF123456");
        result[0].LotNumbers[0].ReagentType.Should().Be(EnumReagentType.EliaCalibrationWellCarrier);
    }

    [Fact]
    public async Task MapTestOrders_Should_AddCarrierLot_When_CarrierLotIsEliaWellCarrier()
    {
        // Arrange
        var request = new Domain.Request
        {
            SampleId = "S1",
            Created = DateTimeOffset.UtcNow
        };

        var article = new Article
        {
            ArticleNumber = "GHI",
            ArticleType = ArticleType.EliaWellCarrier
        };

        var testOrder = new TestOrder
        {
            Uid = Guid.NewGuid(),
            MethodName = "Method1",
            TestName = "Test1",
            Request = request,
            TestOrderCustomParameters = new TestOrderCustomParameter
            {
                CarrierLot = "GHI123456",
                CustomTestId = "Test1"
            }
        };

        _articlesServiceMock
            .Setup(x => x.GetArticle("GHI"))
            .ReturnsAsync(article);

        var testOrders = new List<TestOrder> { testOrder };

        // Act
        var result = await TestOrderMapper.MapTestOrders(testOrders, _articlesServiceMock.Object);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(1);
        result[0].LotNumbers.Count.Should().Be(1);
        result[0].LotNumbers[0].LotNumber_.Should().Be("GHI123456");
        result[0].LotNumbers[0].ReagentType.Should().Be(EnumReagentType.EliaWellCarrier);
    }

    [Fact]
    public async Task MapTestOrders_Should_AssumeImmunoCapCarrier_When_ArticleNotFound()
    {
        // Arrange
        var request = new Domain.Request
        {
            SampleId = "S1",
            Created = DateTimeOffset.UtcNow
        };

        var testOrder = new TestOrder
        {
            Uid = Guid.NewGuid(),
            MethodName = "Method1",
            TestName = "Test1",
            Request = request,
            TestOrderCustomParameters = new TestOrderCustomParameter
            {
                CarrierLot = "XYZ123456",
                CustomTestId = "Test1"
            }
        };

        _articlesServiceMock
            .Setup(x => x.GetArticle("XYZ"))
            .ReturnsAsync((Article?)null);

        var testOrders = new List<TestOrder> { testOrder };

        // Act
        var result = await TestOrderMapper.MapTestOrders(testOrders, _articlesServiceMock.Object);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(1);
        result[0].LotNumbers.Count.Should().Be(1);
        result[0].LotNumbers[0].LotNumber_.Should().Be("XYZ123456");
        result[0].LotNumbers[0].ReagentType.Should().Be(EnumReagentType.ImmunoCapCarrier);
    }

    [Fact]
    public async Task MapTestOrders_Should_NotAddCarrierLot_When_CarrierLotIsTooShort()
    {
        // Arrange
        var request = new Domain.Request
        {
            SampleId = "S1",
            Created = DateTimeOffset.UtcNow
        };

        var testOrder = new TestOrder
        {
            Uid = Guid.NewGuid(),
            MethodName = "Method1",
            TestName = "Test1",
            Request = request,
            TestOrderCustomParameters = new TestOrderCustomParameter
            {
                CarrierLot = "AB",
                CustomTestId = "Test1"
            }
        };

        var testOrders = new List<TestOrder> { testOrder };

        // Act
        var result = await TestOrderMapper.MapTestOrders(testOrders, _articlesServiceMock.Object);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(1);
        result[0].LotNumbers.Count.Should().Be(0);
        _articlesServiceMock.Verify(x => x.GetArticle(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task MapTestOrders_Should_NotAddCarrierLot_When_CarrierLotIsEmpty()
    {
        // Arrange
        var request = new Domain.Request
        {
            SampleId = "S1",
            Created = DateTimeOffset.UtcNow
        };

        var testOrder = new TestOrder
        {
            Uid = Guid.NewGuid(),
            MethodName = "Method1",
            TestName = "Test1",
            Request = request,
            TestOrderCustomParameters = new TestOrderCustomParameter
            {
                CarrierLot = "",
                CustomTestId = "Test1"
            }
        };

        var testOrders = new List<TestOrder> { testOrder };

        // Act
        var result = await TestOrderMapper.MapTestOrders(testOrders, _articlesServiceMock.Object);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(1);
        result[0].LotNumbers.Count.Should().Be(0);
        _articlesServiceMock.Verify(x => x.GetArticle(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task MapTestOrders_Should_AddConjugateLot_When_ConjugateLotProvided()
    {
        // Arrange
        var request = new Domain.Request
        {
            SampleId = "S1",
            Created = DateTimeOffset.UtcNow
        };

        var testOrder = new TestOrder
        {
            Uid = Guid.NewGuid(),
            MethodName = "Method1",
            TestName = "Test1",
            Request = request,
            TestOrderCustomParameters = new TestOrderCustomParameter
            {
                ConjugateLot = "CONJ123",
                CustomTestId = "Test1"
            }
        };

        var testOrders = new List<TestOrder> { testOrder };

        // Act
        var result = await TestOrderMapper.MapTestOrders(testOrders, _articlesServiceMock.Object);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(1);
        result[0].LotNumbers.Count.Should().Be(1);
        result[0].LotNumbers[0].LotNumber_.Should().Be("CONJ123");
        result[0].LotNumbers[0].ReagentType.Should().Be(EnumReagentType.ConjugateCartridge);
    }

    [Fact]
    public async Task MapTestOrders_Should_AddDevelopmentLot_When_DevelopmentLotProvided()
    {
        // Arrange
        var request = new Domain.Request
        {
            SampleId = "S1",
            Created = DateTimeOffset.UtcNow
        };

        var testOrder = new TestOrder
        {
            Uid = Guid.NewGuid(),
            MethodName = "Method1",
            TestName = "Test1",
            Request = request,
            TestOrderCustomParameters = new TestOrderCustomParameter
            {
                DevelopmentLot = "DEV123",
                CustomTestId = "Test1"
            }
        };

        var testOrders = new List<TestOrder> { testOrder };

        // Act
        var result = await TestOrderMapper.MapTestOrders(testOrders, _articlesServiceMock.Object);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(1);
        result[0].LotNumbers.Count.Should().Be(1);
        result[0].LotNumbers[0].LotNumber_.Should().Be("DEV123");
        result[0].LotNumbers[0].ReagentType.Should().Be(EnumReagentType.DevelopmentSolutionCartridge);
    }

    [Fact]
    public async Task MapTestOrders_Should_AddDiluentLot_When_DiluentLotProvided()
    {
        // Arrange
        var request = new Domain.Request
        {
            SampleId = "S1",
            Created = DateTimeOffset.UtcNow
        };

        var testOrder = new TestOrder
        {
            Uid = Guid.NewGuid(),
            MethodName = "Method1",
            TestName = "Test1",
            Request = request,
            TestOrderCustomParameters = new TestOrderCustomParameter
            {
                DiluentLot = "DIL123",
                CustomTestId = "Test1"
            }
        };

        var testOrders = new List<TestOrder> { testOrder };

        // Act
        var result = await TestOrderMapper.MapTestOrders(testOrders, _articlesServiceMock.Object);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(1);
        result[0].LotNumbers.Count.Should().Be(1);
        result[0].LotNumbers[0].LotNumber_.Should().Be("DIL123");
        result[0].LotNumbers[0].ReagentType.Should().Be(EnumReagentType.DiluentCartridge);
    }

    [Fact]
    public async Task MapTestOrders_Should_AddAllLotNumbers_When_AllLotsProvided()
    {
        // Arrange
        var request = new Domain.Request
        {
            SampleId = "S1",
            Created = DateTimeOffset.UtcNow
        };

        var article = new Article
        {
            ArticleNumber = "ABC",
            ArticleType = ArticleType.ImmunoCapCarrier
        };

        var testOrder = new TestOrder
        {
            Uid = Guid.NewGuid(),
            MethodName = "Method1",
            TestName = "Test1",
            Request = request,
            TestOrderCustomParameters = new TestOrderCustomParameter
            {
                CarrierLot = "ABC123456",
                ConjugateLot = "CONJ123",
                DevelopmentLot = "DEV123",
                DiluentLot = "DIL123",
                CustomTestId = "Test1"
            }
        };

        _articlesServiceMock
            .Setup(x => x.GetArticle("ABC"))
            .ReturnsAsync(article);

        var testOrders = new List<TestOrder> { testOrder };

        // Act
        var result = await TestOrderMapper.MapTestOrders(testOrders, _articlesServiceMock.Object);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(1);
        result[0].LotNumbers.Count.Should().Be(4);

        result[0].LotNumbers.Should().Contain(ln =>
            ln.LotNumber_ == "ABC123456" && ln.ReagentType == EnumReagentType.ImmunoCapCarrier);
        result[0].LotNumbers.Should().Contain(ln =>
            ln.LotNumber_ == "CONJ123" && ln.ReagentType == EnumReagentType.ConjugateCartridge);
        result[0].LotNumbers.Should().Contain(ln =>
            ln.LotNumber_ == "DEV123" && ln.ReagentType == EnumReagentType.DevelopmentSolutionCartridge);
        result[0].LotNumbers.Should().Contain(ln =>
            ln.LotNumber_ == "DIL123" && ln.ReagentType == EnumReagentType.DiluentCartridge);
    }

    [Fact]
    public async Task MapTestOrders_Should_MapMultipleTestOrders_When_MultipleTestOrdersProvided()
    {
        // Arrange
        var request = new Domain.Request
        {
            SampleId = "S1",
            Created = DateTimeOffset.UtcNow
        };

        var testOrder1 = new TestOrder
        {
            Uid = Guid.NewGuid(),
            MethodName = "Method1",
            TestName = "Test1",
            Request = request,
            TestOrderCustomParameters = new TestOrderCustomParameter
            {
                ConjugateLot = "CONJ123",
                CustomTestId = "Test1"
            }
        };

        var testOrder2 = new TestOrder
        {
            Uid = Guid.NewGuid(),
            MethodName = "Method2",
            TestName = "Test2",
            Request = request,
            TestOrderCustomParameters = new TestOrderCustomParameter
            {
                DiluentLot = "DIL456",
                CustomTestId = "Test2"
            }
        };

        var testOrders = new List<TestOrder> { testOrder1, testOrder2 };

        // Act
        var result = await TestOrderMapper.MapTestOrders(testOrders, _articlesServiceMock.Object);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(2);
        result[0].LotNumbers.Count.Should().Be(1);
        result[0].LotNumbers[0].LotNumber_.Should().Be("CONJ123");
        result[1].LotNumbers.Count.Should().Be(1);
        result[1].LotNumbers[0].LotNumber_.Should().Be("DIL456");
    }

    [Fact]
    public async Task MapTestOrders_Should_ReturnEmptyRepeatedField_When_EmptyListProvided()
    {
        // Act
        var result = await TestOrderMapper.MapTestOrders([], _articlesServiceMock.Object);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(0);
    }
}
