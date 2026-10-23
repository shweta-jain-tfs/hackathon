using MediatR;
using Pharos.Server.Application.Quality;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Parameters;
using Pharos.Server.Quality.Application.Queries;

namespace Pharos.Server.Quality.Web.Controllers;

public class QualityControllerTests
{
    private readonly Mock<ISender> _senderMock;
    private readonly QualityController _controller;

    public QualityControllerTests()
    {
        _senderMock = new Mock<ISender>();
        _controller = new QualityController(_senderMock.Object);
    }

    #region GetQcCartridgeInfo Tests

    [Fact]
    public async Task GetQcCartridgeInfo_ValidDataMatrix_ReturnsQcCartridgeInformation()
    {
        // Arrange
        var dataMatrix = "|QC1011ABCDEF12H4sIAAAAAAAEAMtIzcnJBwCGphA2BQAAAA==";
        var expectedResult = CreateQcCartridgeInformationDto();

        _senderMock
            .Setup(x => x.Send(It.Is<GetQcCartridgeInfoQuery>(q => q.DataMatrix == dataMatrix), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetQcCartridgeInfo(dataMatrix, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedResult);
        _senderMock.Verify(x => x.Send(It.Is<GetQcCartridgeInfoQuery>(q => q.DataMatrix == dataMatrix), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetQcCartridgeInfo_InvalidDataMatrix_ReturnsNull()
    {
        // Arrange
        var dataMatrix = "invalid-data";
        _senderMock
            .Setup(x => x.Send(It.Is<GetQcCartridgeInfoQuery>(q => q.DataMatrix == dataMatrix), It.IsAny<CancellationToken>()))
            .ReturnsAsync((QcCartridgeInformationDto?)null);

        // Act
        var result = await _controller.GetQcCartridgeInfo(dataMatrix, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeNull();
        _senderMock.Verify(x => x.Send(It.IsAny<GetQcCartridgeInfoQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetQcCartridgeInfo_EmptyDataMatrix_ReturnsNull()
    {
        // Arrange
        var dataMatrix = string.Empty;
        _senderMock
            .Setup(x => x.Send(It.Is<GetQcCartridgeInfoQuery>(q => q.DataMatrix == dataMatrix), It.IsAny<CancellationToken>()))
            .ReturnsAsync((QcCartridgeInformationDto?)null);

        // Act
        var result = await _controller.GetQcCartridgeInfo(dataMatrix, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetQcCartridgeInfo_ReturnsQcCartridgeWithTests()
    {
        // Arrange
        var dataMatrix = "|QC1011ABCDEF12H4sIAAAAAAAEAMtIzcnJBwCGphA2BQAAAA==";
        var expectedResult = CreateQcCartridgeInformationDtoWithTests();

        _senderMock
            .Setup(x => x.Send(It.IsAny<GetQcCartridgeInfoQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetQcCartridgeInfo(dataMatrix, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result!.Tests.Should().HaveCount(2);
        result.Tests[0].TestName.Should().Be("IGA");
        result.Tests[1].TestName.Should().Be("IGE");
    }

    [Fact]
    public async Task GetQcCartridgeInfo_PropagatesException_WhenSenderThrows()
    {
        // Arrange
        var dataMatrix = "|QC1011ABCDEF12H4sIAAAAAAAEAMtIzcnJBwCGphA2BQAAAA==";
        _senderMock
            .Setup(x => x.Send(It.IsAny<GetQcCartridgeInfoQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Parsing failed"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _controller.GetQcCartridgeInfo(dataMatrix, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetQcCartridgeInfo_RespectsCancellationToken()
    {
        // Arrange
        var dataMatrix = "|QC1011ABCDEF12H4sIAAAAAAAEAMtIzcnJBwCGphA2BQAAAA==";
        using var cts = new CancellationTokenSource();

        _senderMock
            .Setup(x => x.Send(It.IsAny<GetQcCartridgeInfoQuery>(), cts.Token))
            .ReturnsAsync(CreateQcCartridgeInformationDto());

        // Act
        await _controller.GetQcCartridgeInfo(dataMatrix, cts.Token);

        // Assert
        _senderMock.Verify(x => x.Send(It.IsAny<GetQcCartridgeInfoQuery>(), cts.Token), Times.Once);
    }

    [Theory]
    [InlineData("|ABC121DEADBEEF")]
    [InlineData("|QC1011ABCDEF12payload")]
    [InlineData("|XYZAB2CAFEBABE")]
    public async Task GetQcCartridgeInfo_DifferentDataMatrixFormats_SendsCorrectQuery(string dataMatrix)
    {
        // Arrange
        _senderMock
            .Setup(x => x.Send(It.Is<GetQcCartridgeInfoQuery>(q => q.DataMatrix == dataMatrix), It.IsAny<CancellationToken>()))
            .ReturnsAsync((QcCartridgeInformationDto?)null);

        // Act
        await _controller.GetQcCartridgeInfo(dataMatrix, TestContext.Current.CancellationToken);

        // Assert
        _senderMock.Verify(x => x.Send(It.Is<GetQcCartridgeInfoQuery>(q => q.DataMatrix == dataMatrix), It.IsAny<CancellationToken>()), Times.Once);
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
            Colors = ["#FF0000", "#00FF00"],
            Texts = ["QC", "1"],
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
