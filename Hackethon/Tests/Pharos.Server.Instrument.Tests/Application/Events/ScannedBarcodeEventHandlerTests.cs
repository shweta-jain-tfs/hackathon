using Moq;
using Pharos.Server.Application.Instrument;

namespace Pharos.Server.Instrument.Application.Events;

public class ScannedBarcodeEventHandlerTests
{
    private readonly Mock<ICurrentInstrumentServerHub> _hubMock;
    private readonly Mock<IInstrumentManager> _instrumentServiceMock;
    private readonly ScannedBarcodeEventHandler _handler;

    private const string _testInstrumentNumber = "S001";
    private const string _testBarcode = "TEST123";
    private const int _testInstrumentId = 1;

    public ScannedBarcodeEventHandlerTests()
    {
        _hubMock = new Mock<ICurrentInstrumentServerHub>();
        _instrumentServiceMock = new Mock<IInstrumentManager>();

        _handler = new ScannedBarcodeEventHandler(
            _hubMock.Object,
            _instrumentServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldSendScannedBarcodeToHub_WhenInstrumentIdExists()
    {
        // Arrange
        var @event = new ScannedBarcodeEvent(_testInstrumentNumber, _testBarcode);
        _instrumentServiceMock
            .Setup(x => x.FindInstrument(_testInstrumentNumber))
            .Returns(new Domain.Instrument { Id = _testInstrumentId });

        // Act
        await _handler.Handle(@event, TestContext.Current.CancellationToken);

        // Assert
        _hubMock.Verify(
            x => x.SendScannedBarcode(_testInstrumentId, _testBarcode),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldNotSendToHub_WhenInstrumentIdIsNull()
    {
        // Arrange
        var @event = new ScannedBarcodeEvent(_testInstrumentNumber, _testBarcode);
        _instrumentServiceMock
            .Setup(x => x.FindInstrument(_testInstrumentNumber))
            .Returns((IInstrument?)null);

        // Act
        await _handler.Handle(@event, TestContext.Current.CancellationToken);

        // Assert
        _hubMock.Verify(
            x => x.SendScannedBarcode(It.IsAny<int>(), It.IsAny<string>()),
            Times.Never);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_ShouldNotSendToHub_WhenBarcodeIsNullOrWhitespace(string? barcode)
    {
        // Arrange
        var @event = new ScannedBarcodeEvent(_testInstrumentNumber, barcode!);
        _instrumentServiceMock
            .Setup(x => x.FindInstrument(_testInstrumentNumber))
            .Returns(new Domain.Instrument { Id = _testInstrumentId });

        // Act
        await _handler.Handle(@event, TestContext.Current.CancellationToken);

        // Assert
        _hubMock.Verify(
            x => x.SendScannedBarcode(It.IsAny<int>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldPassCorrectParameters_ToHubMethod()
    {
        // Arrange
        const string expectedBarcode = "BARCODE456";
        const int expectedInstrumentId = 42;
        var @event = new ScannedBarcodeEvent(_testInstrumentNumber, expectedBarcode);
        _instrumentServiceMock
            .Setup(x => x.FindInstrument(_testInstrumentNumber))
            .Returns(new Domain.Instrument { Id = expectedInstrumentId });

        // Act
        await _handler.Handle(@event, TestContext.Current.CancellationToken);

        // Assert
        _hubMock.Verify(
            x => x.SendScannedBarcode(expectedInstrumentId, expectedBarcode),
            Times.Once);
    }
}
