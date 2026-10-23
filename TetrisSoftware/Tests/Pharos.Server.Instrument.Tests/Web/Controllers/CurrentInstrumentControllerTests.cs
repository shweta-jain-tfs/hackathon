using MediatR;
using Moq;
using Pharos.Server.Application;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Instrument.Application.Commands.ControlLight;
using Pharos.Server.Instrument.Application.Queries.GetBarcodeDetails;
using Pharos.Server.Instrument.Application.Services;
using IAuthorizationService = Pharos.Server.Application.Security.IAuthorizationService;

namespace Pharos.Server.Instrument.Web.Controllers;

public class CurrentInstrumentControllerTests
{
    private readonly Mock<ISender> _senderMock;
    private readonly CurrentInstrumentController _controller;

    private const string _testInstrumentNumber = "S001";
    private const string _testBarcode = "TEST123";
    private const int _testInstrumentId = 1;

    public CurrentInstrumentControllerTests()
    {
        var currentInstrumentServiceMock = new Mock<ICurrentInstrumentService>();
        _senderMock = new Mock<ISender>();
        var automaticPowerSaveSchedulerMock = new Mock<IAutomaticPowerSaveScheduler>();
        var authorizationServiceMock = new Mock<IAuthorizationService>();

        currentInstrumentServiceMock
            .Setup(x => x.InstrumentId)
            .Returns(_testInstrumentId);
        currentInstrumentServiceMock
            .Setup(x => x.InstrumentNumber)
            .Returns(_testInstrumentNumber);

        _controller = new CurrentInstrumentController(
            currentInstrumentServiceMock.Object,
            _senderMock.Object,
            automaticPowerSaveSchedulerMock.Object,
            authorizationServiceMock.Object);
    }

    [Fact]
    public async Task GetBarcodeDetails_ShouldReturnDto_WhenBarcodeIsValid()
    {
        // Arrange
        var expectedDto = new ScannedBarcodeDto(
            _testBarcode,
            BarcodeType.Carrier,
            0,
            true);

        _senderMock
            .Setup(x => x.Send(It.IsAny<GetBarcodeDetailsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _controller.GetBarcodeDetails(_testBarcode, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedDto);
    }

    [Fact]
    public async Task GetBarcodeDetails_ShouldReturnNull_WhenBarcodeNotFound()
    {
        // Arrange
        _senderMock
            .Setup(x => x.Send(It.IsAny<GetBarcodeDetailsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetBarcodeDetailsQuery _, CancellationToken _) => null!);

        // Act
        var result = await _controller.GetBarcodeDetails(_testBarcode, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetBarcodeDetails_ShouldPassCorrectParameters_ToQueryHandler()
    {
        // Arrange
        const string expectedBarcode = "BARCODE456";
        GetBarcodeDetailsQuery? capturedQuery = null;

        _senderMock
            .Setup(x => x.Send(It.IsAny<GetBarcodeDetailsQuery>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<ScannedBarcodeDto?>, CancellationToken>((query, _) =>
            {
                capturedQuery = query as GetBarcodeDetailsQuery;
            })
            .ReturnsAsync(new ScannedBarcodeDto(expectedBarcode, BarcodeType.Carrier));

        // Act
        await _controller.GetBarcodeDetails(expectedBarcode, TestContext.Current.CancellationToken);

        // Assert
        capturedQuery.Should().NotBeNull();
        capturedQuery!.Barcode.Should().Be(expectedBarcode);
        capturedQuery.InstrumentNumber.Should().Be(_testInstrumentNumber);
    }

    [Fact]
    public async Task GetBarcodeDetails_ShouldPassCancellationToken()
    {
        // Arrange
        var cancellationToken = new CancellationToken();
        CancellationToken capturedToken = default;

        _senderMock
            .Setup(x => x.Send(It.IsAny<GetBarcodeDetailsQuery>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<ScannedBarcodeDto?>, CancellationToken>((_, ct) =>
            {
                capturedToken = ct;
            })
            .ReturnsAsync(new ScannedBarcodeDto(_testBarcode, BarcodeType.Carrier));

        // Act
        await _controller.GetBarcodeDetails(_testBarcode, cancellationToken);

        // Assert
        capturedToken.Should().Be(cancellationToken);
        _senderMock.Verify(
            x => x.Send(It.IsAny<GetBarcodeDetailsQuery>(), cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task ControlLight_ShouldSendCommandForCurrentInstrument()
    {
        // Act
        await _controller.ControlLight(TestContext.Current.CancellationToken);

        // Assert
        _senderMock.Verify(
            sender => sender.Send(
                It.Is<ControlLightCommand>(command => command.InstrumentNumber == _testInstrumentNumber),
                TestContext.Current.CancellationToken),
            Times.Once);
    }
}
