using MediatR;
using Pharos.Server.Application;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Reagents.Application.Queries;

namespace Pharos.Server.Reagents.Web.Controllers;

public class LiquidDrawerControllerTests
{

    [Trait("ProductRequirement", "PR_737")]
    [Trait("ModuleRequirement", "MR_5340")]
    [Fact]
    public async Task GetLiquidDrawerDetails_ReturnsExpectedDto()
    {
        // Arrange
        var instrumentNumber = "SN12345";
        var expectedDto = new LiquidDrawerDetailsDto
        {
            LockState = LockState.ClosedUnlocked,
            Containers = new List<LiquidContainer>
            {
                new()
                {
                    Position = 2,
                    Type = LiquidContainerType.Waste,
                    InUse = true,
                    Percentage = 45,
                    Status = ReagentPositionStatus.Available,
                    IsPipedEnabled = false,
                    IsMissing = false,
                    FillOrEmpty = FillOrEmptyIndicationType.None
                },
                new()
                {
                    Position = 1,
                    Type = LiquidContainerType.Waste,
                    InUse = false,
                    Percentage = 10,
                    Status = ReagentPositionStatus.Available,
                    IsPipedEnabled = false,
                    IsMissing = false,
                    FillOrEmpty = FillOrEmptyIndicationType.None
                },
                new()
                {
                    Position = 2,
                    Type = LiquidContainerType.Rinse,
                    InUse = true,
                    Percentage = 75,
                    Status = ReagentPositionStatus.Available,
                    IsPipedEnabled = false,
                    IsMissing = false,
                    FillOrEmpty = FillOrEmptyIndicationType.None
                },
                new()
                {
                    Position = 1,
                    Type = LiquidContainerType.Rinse,
                    InUse = false,
                    Percentage = 90,
                    Status = ReagentPositionStatus.Available,
                    IsPipedEnabled = false,
                    IsMissing = false,
                    FillOrEmpty = FillOrEmptyIndicationType.None
                }
            }
        };

        var currentInstrumentServiceMock = new Mock<ICurrentInstrumentService>();
        currentInstrumentServiceMock.Setup(x => x.InstrumentNumber)
            .Returns(instrumentNumber);

        var senderMock = new Mock<ISender>();
        senderMock.Setup(
            x => x.Send(
                It.Is<GetLiquidDrawerDetailsQuery>(q => q.InstrumentNumber == instrumentNumber),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        var controller = new LiquidDrawerController(currentInstrumentServiceMock.Object, senderMock.Object);

        // Act
        var result = await controller.GetLiquidDrawerDetails(TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(expectedDto);
        senderMock.Verify(
            x => x.Send(It.IsAny<GetLiquidDrawerDetailsQuery>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetLiquidDrawerDetails_ThrowsNotFoundException_WhenInstrumentNumberIsNull()
    {
        // Arrange
        var currentInstrumentServiceMock = new Mock<ICurrentInstrumentService>();
        var senderMock = new Mock<ISender>();
        var controller = new LiquidDrawerController(currentInstrumentServiceMock.Object, senderMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => controller.GetLiquidDrawerDetails(TestContext.Current.CancellationToken));
    }
}
