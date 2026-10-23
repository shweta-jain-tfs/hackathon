using MediatR;
using Pharos.Server.Application;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Domain.Parameters;
using Pharos.Server.Reagents.Application.Queries;

namespace Pharos.Server.Reagents.Web.Controllers;

public class BottleDrawerControllerTests
{
    [Trait("ProductRequirement", "PR_2180")]
    [Trait("ModuleRequirement", "MR_5059")]
    [Fact]
    public async Task GetBottleDrawerDetails_ReturnsExpectedDto()
    {
        // Arrange
        var instrumentNumber = "SN12345";
        var expectedDto = new BottleDrawerDetailsDto
        {
            LockState = LockState.ClosedUnlocked,
            Positions =
                new List<BottlePositionDto> { new() { Position = 1, ArticleType = ArticleType.StopSolutionBottle } }
        };

        var currentInstrumentServiceMock = new Mock<ICurrentInstrumentService>();
        currentInstrumentServiceMock.Setup(x => x.InstrumentNumber)
            .Returns(instrumentNumber);

        var senderMock = new Mock<ISender>();
        senderMock.Setup(
            x => x.Send(
                It.Is<GetBottleDrawerDetailsQuery>(q => q.InstrumentNumber == instrumentNumber),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        var controller = new BottleDrawerController(currentInstrumentServiceMock.Object, senderMock.Object);

        // Act
        var result = await controller.GetBottleDrawerDetails(TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(expectedDto);
        senderMock.Verify(
            x => x.Send(It.IsAny<GetBottleDrawerDetailsQuery>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetBottleDrawerDetails_ThrowsNotFoundException_WhenInstrumentNumberIsNull()
    {
        // Arrange
        var currentInstrumentServiceMock = new Mock<ICurrentInstrumentService>();
        var senderMock = new Mock<ISender>();
        var controller = new BottleDrawerController(currentInstrumentServiceMock.Object, senderMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => controller.GetBottleDrawerDetails(TestContext.Current.CancellationToken));
    }

    [Trait("ProductRequirement", "PR_1370")]
    [Trait("ModuleRequirement", "MR_5064")]
    [Trait("ModuleRequirement", "MR_5066")]
    [Trait("ModuleRequirement", "MR_5067")]
    [Trait("ModuleRequirement", "MR_5069")]
    [Trait("ModuleRequirement", "MR_5070")]
    [Trait("ProductRequirement", "PR_1377")]
    [Trait("ModuleRequirement", "MR_5065")]
    [Trait("ModuleRequirement", "MR_5068")]
    [Theory]
    [InlineData("BC123", LoadingPositionStatus.Found, ArticleType.WashConcentrateBottle, 2, 1, 100, ArticleQuantityUnit.Milliliters, "Wash Concentrate", "LOT123")]
    [InlineData("BC456", LoadingPositionStatus.NotFound, ArticleType.StopSolutionBottle, 1, 2, 50, ArticleQuantityUnit.Tests, "Stop Solution", "LOT456")]
    [InlineData("BC789", LoadingPositionStatus.Found, ArticleType.WashAdditiveBottle, 3, 3, 0, ArticleQuantityUnit.Milliliters, null, null)]
    public async Task GetLoadingPosition_ReturnsExpectedDto(
        string barcode,
        LoadingPositionStatus status,
        ArticleType articleType,
        int position,
        int id,
        int quantity,
        ArticleQuantityUnit unitOfMeasure,
        string? name,
        string? lotNumber)
    {
        // Arrange
        var instrumentNumber = "SN12345";
        var expectedDto = new LoadingPositionDto
        {
            Status = status,
            ArticleType = articleType,
            Position = position,
            Colors = new List<string?> { "#005B8B", "#FF0000" },
            Id = id,
            Quantity = quantity,
            UnitOfMeasure = unitOfMeasure,
            Name = name,
            LotNumber = lotNumber,
            ExpiryDate = DateTimeOffset.UtcNow.AddMonths(6)
        };

        var currentInstrumentServiceMock = new Mock<ICurrentInstrumentService>();
        currentInstrumentServiceMock.Setup(x => x.InstrumentNumber)
            .Returns(instrumentNumber);

        var senderMock = new Mock<ISender>();
        senderMock.Setup(
            x => x.Send(
                It.Is<GetLoadingPositionQuery>(q => q.Barcode == barcode && q.InstrumentNumber == instrumentNumber),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        var controller = new BottleDrawerController(currentInstrumentServiceMock.Object, senderMock.Object);

        // Act
        var result = await controller.GetLoadingPosition(barcode, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Should()
            .BeEquivalentTo(
                expectedDto,
                options => options
            .RespectingRuntimeTypes()
                    .WithStrictOrdering());
        result.Status.Should().Be(status);
        result.ArticleType.Should().Be(articleType);
        result.Position.Should().Be(position);
        result.Id.Should().Be(id);
        result.Quantity.Should().Be(quantity);
        result.UnitOfMeasure.Should().Be(unitOfMeasure);
        result.Name.Should().Be(name);
        result.LotNumber.Should().Be(lotNumber);
        result.Colors.Should().BeEquivalentTo(expectedDto.Colors);
        senderMock.Verify(x => x.Send(It.IsAny<GetLoadingPositionQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetLoadingPosition_ThrowsNotFoundException_WhenInstrumentNumberIsNull()
    {
        // Arrange
        var barcode = "BC123456789";
        var currentInstrumentServiceMock = new Mock<ICurrentInstrumentService>();
        var senderMock = new Mock<ISender>();
        var controller = new BottleDrawerController(currentInstrumentServiceMock.Object, senderMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => controller.GetLoadingPosition(barcode, TestContext.Current.CancellationToken));
    }
}