using MediatR;
using Pharos.Server.Application;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Reagents.Application.Queries;

namespace Pharos.Server.Reagents.Web.Controllers;

public class SolidWasteDrawerControllerTests
{

    [Trait("ProductRequirement", "PR_65")]
    [Trait("ModuleRequirement", "MR_5511")]
    [Fact]
    public async Task GetWasteDrawerDetails_ReturnsExpectedDto()
    {
        // Arrange
        var instrumentNumber = "SN12345";
        var expectedDto = new SolidWasteDrawerDetailsDto
        {
            LockState = LockState.ClosedUnlocked
        };

        var currentInstrumentServiceMock = new Mock<ICurrentInstrumentService>();
        currentInstrumentServiceMock.Setup(x => x.InstrumentNumber)
            .Returns(instrumentNumber);

        var senderMock = new Mock<ISender>();
        senderMock.Setup(
            x => x.Send(
                It.Is<GetSolidWasteDrawerDetailsQuery>(q => q.InstrumentNumber == instrumentNumber),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        var controller = new SolidWasteDrawerController(currentInstrumentServiceMock.Object, senderMock.Object);

        // Act
        var result = await controller.GetWasteDrawerDetails(TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(expectedDto);
        senderMock.Verify(
            x => x.Send(It.IsAny<GetSolidWasteDrawerDetailsQuery>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetWasteDrawerDetails_ThrowsNotFoundException_WhenInstrumentNumberIsNull()
    {
        // Arrange
        var currentInstrumentServiceMock = new Mock<ICurrentInstrumentService>();
        var senderMock = new Mock<ISender>();
        var controller = new SolidWasteDrawerController(currentInstrumentServiceMock.Object, senderMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => controller.GetWasteDrawerDetails(TestContext.Current.CancellationToken));
    }
}

