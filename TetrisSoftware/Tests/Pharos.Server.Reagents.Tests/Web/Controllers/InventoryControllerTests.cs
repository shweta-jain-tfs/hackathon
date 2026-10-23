using MediatR;
using Microsoft.AspNetCore.Mvc;
using Pharos.Server.Reagents.Application.Commands;
using Pharos.Server.Application.Queries;
using Pharos.Server.Reagents.Application.Queries.Inventory;

namespace Pharos.Server.Reagents.Web.Controllers;

public class InventoryControllerTests
{
    [Fact]
    public async Task GetCarriers_ReturnsExpectedResult()
    {
        // Arrange
        var query = new GetCarriersQuery();
        var expected = new PartialQueryResult<CarrierListResultDto>(
            new List<CarrierListResultDto> { new() { Id = 1, FreePositions = 2, TotalPositions = 3 } }, 
            0, 
            1);
        var senderMock = new Mock<ISender>();
        senderMock.Setup(x => x.Send<PartialQueryResult<CarrierListResultDto>>(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = new InventoryController(senderMock.Object);

        // Act
        var result = await controller.GetCarriers(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEquivalentTo(expected);
        senderMock.Verify(x => x.Send<PartialQueryResult<CarrierListResultDto>>(query, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCartridges_ReturnsExpectedResult()
    {
        // Arrange
        var query = new GetCartridgesQuery();
        var expected = new PartialQueryResult<CartridgeListResultDto>(
            new List<CartridgeListResultDto> { new() { Id = 1, FreePositionsFront = 2, TotalPositionsFront = 3, FreePositionsRear = 1, TotalPositionsRear = 2 } }, 
            0, 
            1);
        var senderMock = new Mock<ISender>();
        senderMock.Setup(x => x.Send<PartialQueryResult<CartridgeListResultDto>>(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = new InventoryController(senderMock.Object);

        // Act
        var result = await controller.GetCartridges(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEquivalentTo(expected);
        senderMock.Verify(x => x.Send<PartialQueryResult<CartridgeListResultDto>>(query, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetBottles_ReturnsExpectedResult()
    {
        // Arrange
        var query = new GetBottlesQuery();
        var expected = new PartialQueryResult<BottleListResultDto>(
            new List<BottleListResultDto> { new() { Id = 1, FreePositions = 2, TotalPositions = 3 } }, 
            0, 
            1);
        var senderMock = new Mock<ISender>();
        senderMock.Setup(x => x.Send<PartialQueryResult<BottleListResultDto>>(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = new InventoryController(senderMock.Object);

        // Act
        var result = await controller.GetBottles(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEquivalentTo(expected);
        senderMock.Verify(x => x.Send<PartialQueryResult<BottleListResultDto>>(query, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UnloadReagent_SendsCommandAndReturnsOk()
    {
        // Arrange
        var items = new List<UnloadReagentRequestItemDto>
        {
            new() { InstrumentNumber = "INST1" },
            new() { InstrumentNumber = "INST2" }
        };
        var senderMock = new Mock<ISender>();
        senderMock.Setup(x => x.Send(It.IsAny<UnloadReagentCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(Unit.Value));
        var controller = new InventoryController(senderMock.Object);

        // Act
        var result = await controller.UnloadReagent(items);

        // Assert
        result.Should().BeOfType<OkResult>();
        senderMock.Verify(x => x.Send(It.IsAny<UnloadReagentCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UnloadReagent_SendsCommandWithEmptyList_ReturnsOk()
    {
        // Arrange
        var items = new List<UnloadReagentRequestItemDto>();
        var senderMock = new Mock<ISender>();
        senderMock.Setup(x => x.Send(It.IsAny<UnloadReagentCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(Unit.Value));
        var controller = new InventoryController(senderMock.Object);

        // Act
        var result = await controller.UnloadReagent(items);

        // Assert
        result.Should().BeOfType<OkResult>();
        senderMock.Verify(x => x.Send(It.IsAny<UnloadReagentCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetDrawerDilutionPlates_ReturnsExpectedResult()
    {
        // Arrange
        var query = new GetDrawerDilutionPlatesQuery();
        var expected = new PartialQueryResult<DrawerDilutionPlatesResultDto>(
            new List<DrawerDilutionPlatesResultDto> { new() { Id = 1, PlatesCount = 5, WellsCount = 480 } }, 
            0, 
            1);
        var senderMock = new Mock<ISender>();
        senderMock.Setup(x => x.Send<PartialQueryResult<DrawerDilutionPlatesResultDto>>(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = new InventoryController(senderMock.Object);

        // Act
        var result = await controller.GetDrawerDilutionPlates(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEquivalentTo(expected);
        senderMock.Verify(x => x.Send<PartialQueryResult<DrawerDilutionPlatesResultDto>>(query, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetInternalDilutionPlates_ReturnsExpectedResult()
    {
        // Arrange
        var query = new GetInternalDilutionPlatesQuery();
        var expected = new PartialQueryResult<InternalDilutionPlatesResultDto>(
            new List<InternalDilutionPlatesResultDto> { new() { Id = 1, PlatesCount = 10 } }, 
            0, 
            1);
        var senderMock = new Mock<ISender>();
        senderMock.Setup(x => x.Send<PartialQueryResult<InternalDilutionPlatesResultDto>>(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = new InventoryController(senderMock.Object);

        // Act
        var result = await controller.GetInternalDilutionPlates(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEquivalentTo(expected);
        senderMock.Verify(x => x.Send<PartialQueryResult<InternalDilutionPlatesResultDto>>(query, It.IsAny<CancellationToken>()), Times.Once);
    }
}
