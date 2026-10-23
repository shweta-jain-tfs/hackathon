using Pharos.Server.Application.Quality;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Parameters;

namespace Pharos.Server.Quality.Application.Queries;

public class GetQcCartridgeInfoQueryHandlerTests
{
    private readonly Mock<IQcCartridgeInfoService> _qcCartridgeInfoServiceMock;
    private readonly GetQcCartridgeInfoQueryHandler _handler;

    public GetQcCartridgeInfoQueryHandlerTests()
    {
        _qcCartridgeInfoServiceMock = new Mock<IQcCartridgeInfoService>();
        _handler = new GetQcCartridgeInfoQueryHandler(_qcCartridgeInfoServiceMock.Object);
    }

    #region Handle Tests

    [Fact]
    public async Task Handle_ValidDataMatrix_ReturnsQcCartridgeInformation()
    {
        // Arrange
        var dataMatrix = "|QC1011ABCDEF12payload";
        var expectedResult = CreateQcCartridgeInformationDto();
        var query = new GetQcCartridgeInfoQuery(dataMatrix);

        _qcCartridgeInfoServiceMock
            .Setup(x => x.ParseQcDataMatrixAsync(dataMatrix, TestContext.Current.CancellationToken))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedResult);
        _qcCartridgeInfoServiceMock.Verify(x => x.ParseQcDataMatrixAsync(dataMatrix, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidDataMatrix_ReturnsNull()
    {
        // Arrange
        var dataMatrix = "invalid-data";
        var query = new GetQcCartridgeInfoQuery(dataMatrix);

        _qcCartridgeInfoServiceMock
            .Setup(x => x.ParseQcDataMatrixAsync(dataMatrix, TestContext.Current.CancellationToken))
            .ReturnsAsync((QcCartridgeInformationDto?)null);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_EmptyDataMatrix_ReturnsNull()
    {
        // Arrange
        var query = new GetQcCartridgeInfoQuery(string.Empty);

        _qcCartridgeInfoServiceMock
            .Setup(x => x.ParseQcDataMatrixAsync(string.Empty, TestContext.Current.CancellationToken))
            .ReturnsAsync((QcCartridgeInformationDto?)null);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ReturnsQcCartridgeWithTests()
    {
        // Arrange
        var dataMatrix = "|QC1011ABCDEF12payload";
        var expectedResult = CreateQcCartridgeInformationDtoWithTests();
        var query = new GetQcCartridgeInfoQuery(dataMatrix);

        _qcCartridgeInfoServiceMock
            .Setup(x => x.ParseQcDataMatrixAsync(dataMatrix, TestContext.Current.CancellationToken))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result!.Tests.Should().HaveCount(2);
        result.Tests[0].TestName.Should().Be("IGA");
        result.Tests[0].Limits.Should().HaveCount(2);
        result.Tests[1].TestName.Should().Be("IGE");
        result.Tests[1].Limits.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_PropagatesException_WhenServiceThrows()
    {
        // Arrange
        var dataMatrix = "|QC1011ABCDEF12payload";
        var query = new GetQcCartridgeInfoQuery(dataMatrix);

        _qcCartridgeInfoServiceMock
            .Setup(x => x.ParseQcDataMatrixAsync(dataMatrix, TestContext.Current.CancellationToken))
            .ThrowsAsync(new InvalidOperationException("Checksum validation failed"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(query, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_PassesDataMatrixToService()
    {
        // Arrange
        var dataMatrix = "|ABC121DEADBEEF";
        var query = new GetQcCartridgeInfoQuery(dataMatrix);

        _qcCartridgeInfoServiceMock
            .Setup(x => x.ParseQcDataMatrixAsync(It.IsAny<string>(), TestContext.Current.CancellationToken))
            .ReturnsAsync((QcCartridgeInformationDto?)null);

        // Act
        await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        _qcCartridgeInfoServiceMock.Verify(x => x.ParseQcDataMatrixAsync(dataMatrix, TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsCorrectLotNumberFormat()
    {
        // Arrange
        var dataMatrix = "|QCT011ABCDEF12payload";
        var expectedResult = new QcCartridgeInformationDto
        {
            ArticleId = "QCT",
            LotNumber = "QCT01",  // Combined: ArticleId + DisplayLotNumber
            DisplayLotNumber = "01",
            Version = "1",
            Tests = []
        };
        var query = new GetQcCartridgeInfoQuery(dataMatrix);

        _qcCartridgeInfoServiceMock
            .Setup(x => x.ParseQcDataMatrixAsync(dataMatrix, TestContext.Current.CancellationToken))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result!.ArticleId.Should().Be("QCT");
        result.LotNumber.Should().Be("QCT01");
        result.DisplayLotNumber.Should().Be("01");
    }

    [Theory]
    [InlineData("QC1", "01", "QC101")]
    [InlineData("ABC", "B9", "ABCB9")]
    [InlineData("XYZ", "99", "XYZ99")]
    public async Task Handle_LotNumberCombination_IsCorrect(string articleId, string displayLot, string expectedFullLot)
    {
        // Arrange
        var dataMatrix = $"|{articleId}{displayLot}1ABCDEF12payload";
        var expectedResult = new QcCartridgeInformationDto
        {
            ArticleId = articleId,
            LotNumber = expectedFullLot,
            DisplayLotNumber = displayLot,
            Tests = []
        };
        var query = new GetQcCartridgeInfoQuery(dataMatrix);

        _qcCartridgeInfoServiceMock
            .Setup(x => x.ParseQcDataMatrixAsync(dataMatrix, TestContext.Current.CancellationToken))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result!.LotNumber.Should().Be(expectedFullLot);
    }

    #endregion

    #region Query Record Tests

    [Fact]
    public void GetQcCartridgeInfoQuery_Record_StoresDataMatrix()
    {
        // Arrange & Act
        var query = new GetQcCartridgeInfoQuery("test-data-matrix");

        // Assert
        query.DataMatrix.Should().Be("test-data-matrix");
    }

    [Fact]
    public void GetQcCartridgeInfoQuery_Record_Equality()
    {
        // Arrange
        var query1 = new GetQcCartridgeInfoQuery("test-data");
        var query2 = new GetQcCartridgeInfoQuery("test-data");
        var query3 = new GetQcCartridgeInfoQuery("different-data");

        // Assert
        query1.Should().Be(query2);
        query1.Should().NotBe(query3);
    }

    #endregion

    #region Helper Methods

    private static QcCartridgeInformationDto CreateQcCartridgeInformationDto()
    {
        return new QcCartridgeInformationDto
        {
            ArticleId = "QC1",
            LotNumber = "QC101",
            DisplayLotNumber = "01",
            Version = "1",
            ArticleType = ArticleType.QualityControlCartridge,
            FriendlyName = "QC Cartridge",
            MethodName = "TestMethod",
            Shape = ArticleIndicatorShape.Round,
            Colors = ["#FF0000"],
            Texts = ["QC"],
            Tests = []
        };
    }

    private static QcCartridgeInformationDto CreateQcCartridgeInformationDtoWithTests()
    {
        return new QcCartridgeInformationDto
        {
            ArticleId = "QC1",
            LotNumber = "QC101",
            DisplayLotNumber = "01",
            Version = "1",
            ArticleType = ArticleType.QualityControlCartridge,
            FriendlyName = "QC Cartridge",
            MethodName = "TestMethod",
            Shape = ArticleIndicatorShape.Round,
            Colors = ["#FF0000"],
            Texts = ["QC"],
            Tests =
            [
                new QcCartridgeTestInfo
                {
                    TestName = "IGA",
                    Unit = "kUA/l",
                    Limits =
                    [
                        new QcCartridgeTestLimit { LimitType = QcCartridgeLimitType.GreaterThan, LimitValue = 50 },
                        new QcCartridgeTestLimit { LimitType = QcCartridgeLimitType.LessThan, LimitValue = 150 }
                    ]
                },
                new QcCartridgeTestInfo
                {
                    TestName = "IGE",
                    Unit = "mg/L",
                    Limits =
                    [
                        new QcCartridgeTestLimit { LimitType = QcCartridgeLimitType.Equal, LimitValue = 100 }
                    ]
                }
            ]
        };
    }

    #endregion
}
