using MediatR;
using Pharos.Server.Application.Reagents;
using Tetris.Protocol.Pharos;

namespace Pharos.Server.Reagents.Application.Commands;

public class SendScannedCarrierCommandHandlerTests
{
    [Theory]
    [InlineData("barcode1", "01073330660041291722033110CTS19", true)]
    [InlineData("barcode2", "01073330660041291722033110CTS19", false)]
    [InlineData("", "", true)]
    [InlineData("barcode3", "01073330660041291722033110CTS19", false)]
    public async Task Handle_ReturnsExpectedResult_BasedOnMediatorResponse(string barcode, string udi, bool carrierFound)
    {
        // Arrange
        var mediatorMock = new Mock<IMediator>();

        var scannedCarrier = new ScannedCarrierRequest
        {
            ReagentBarcode = barcode,
            ReagentUdiBarcode = udi
        };

        var request = new SendScannedCarrierCommand("Inst1", scannedCarrier);

        var carrierContentDto = carrierFound ? new ReagentContentDto
        {
            CarrierUdi = udi,
            ReagentBarcode = barcode,
            ContentCells = new List<ReagentContentCellDto> { new() { Position = 1, RemainingQuantity = 20 } }
        } : null;
        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetCarrierContentQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(carrierContentDto);

        var handler = new SendScannedCarrierCommandHandler(mediatorMock.Object);

        // Act
        var result = await handler.Handle(request, TestContext.Current.CancellationToken);

        // Assert
        if (carrierFound)
        {
            result.UnknownCarrier.Should().BeFalse();
            result.Content.Should().NotBeNull();
            result.Content.ReagentUdiBarcode.Should().Be(udi);
            result.Content.ReagentBarcode.Should().Be(barcode);
        }
        else
        {
            result.UnknownCarrier.Should().BeTrue();
            result.Content.Should().NotBeNull();
            result.Content.ReagentUdiBarcode.Should().Be(udi);
        }
    }

    [Fact]
    public async Task Handle_MapsEmptyContent_WhenMediatorReturnsNull()
    {
        // Arrange
        var mediatorMock = new Mock<IMediator>();

        var scannedCarrier = new ScannedCarrierRequest { ReagentBarcode = "b", ReagentUdiBarcode = "u" };
        var request = new SendScannedCarrierCommand("Inst1", scannedCarrier);

        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetCarrierContentQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(null as ReagentContentDto);

        var handler = new SendScannedCarrierCommandHandler(mediatorMock.Object);

        // Act
        var result = await handler.Handle(request, TestContext.Current.CancellationToken);

        // Assert
        result.UnknownCarrier.Should().BeTrue();
        result.Content.Should().NotBeNull();
        result.Content.ReagentUdiBarcode.Should().Be("u");
    }
}