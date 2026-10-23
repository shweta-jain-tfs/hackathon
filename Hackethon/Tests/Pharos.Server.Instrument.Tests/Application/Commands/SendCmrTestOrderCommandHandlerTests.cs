using Moq;
using Pharos.Server.Application.Exceptions;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Instrument.Application.Commands.SendCmrTestOrder;
using Pharos.Server.Instrument.Application.Interfaces;
using Tetris.Protocol.Instrument.TestOrder;

namespace Pharos.Server.Instrument.Application.Commands;

public class SendCmrTestOrderCommandHandlerTests
{
    private readonly Mock<ICommunicationSessionManager> _sessionManagerMock;
    private readonly Mock<IInstrumentManager> _instrumentManagerMock;
    private readonly Mock<IInstrumentConnection> _connectionMock;
    private readonly SendCmrTestOrderCommandHandler _sut;

    private const string _serialNumber = "SN001";

    public SendCmrTestOrderCommandHandlerTests()
    {
        _sessionManagerMock = new Mock<ICommunicationSessionManager>();
        _instrumentManagerMock = new Mock<IInstrumentManager>();
        _connectionMock = new Mock<IInstrumentConnection>();

        _sut = new SendCmrTestOrderCommandHandler(_sessionManagerMock.Object, _instrumentManagerMock.Object);

        var session = new CommunicationSession
        {
            SessionId = Guid.NewGuid(),
            InstrumentNumber = _serialNumber,
            Connection = _connectionMock.Object
        };
        session.SetSynchronizationFlag(CommunicationSynchronizationState.Synchronized);
        _sessionManagerMock.Setup(m => m.GetSession(_serialNumber)).Returns(session);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5838")]
    [Trait("ProductRequirement", "PR_784")]
    public async Task Handle_WhenInstrumentInStandby_ShouldReturnSuccess()
    {
        // Arrange
        SetupInstrumentInState(InstrumentState.Standby);
        var command = new SendCmrTestOrderCommand(_serialNumber, [1, 2, 3]);
        _connectionMock
            .Setup(c => c.SendCmrTestOrderAsync(It.IsAny<CmrTestOrderRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CmrTestOrderResponse { Status = EnumCmrTestOrderStatus.Ok });

        // Act
        var result = await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Status.Should().Be(CmrTestOrderStatus.Ok);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5839")]
    [Trait("ProductRequirement", "PR_784")]
    public async Task Handle_WhenInstrumentInAssayCmrFinished_ShouldReturnSuccess()
    {
        // Arrange
        SetupInstrumentInState(InstrumentState.AssayCmrFinished);
        var command = new SendCmrTestOrderCommand(_serialNumber, [1, 2, 3]);
        _connectionMock
            .Setup(c => c.SendCmrTestOrderAsync(It.IsAny<CmrTestOrderRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CmrTestOrderResponse { Status = EnumCmrTestOrderStatus.Ok });

        // Act
        var result = await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Status.Should().Be(CmrTestOrderStatus.Ok);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5839")]
    [Trait("ProductRequirement", "PR_784")]
    public async Task Handle_WhenInstrumentInCmrPrepare_ShouldReturnSuccess()
    {
        // Arrange
        SetupInstrumentInState(InstrumentState.CmrPrepare);
        var command = new SendCmrTestOrderCommand(_serialNumber, [1, 2, 3]);
        _connectionMock
            .Setup(c => c.SendCmrTestOrderAsync(It.IsAny<CmrTestOrderRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CmrTestOrderResponse { Status = EnumCmrTestOrderStatus.Ok });

        // Act
        var result = await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Status.Should().Be(CmrTestOrderStatus.Ok);
    }

    [Theory]
    [Trait("ModuleRequirement", "MR_5838")]
    [Trait("ProductRequirement", "PR_784")]
    [InlineData(InstrumentState.Assay)]
    [InlineData(InstrumentState.Startup)]
    [InlineData(InstrumentState.Shutdown)]
    [InlineData(InstrumentState.Rinsing)]
    public async Task Handle_WhenInstrumentInInvalidState_ShouldReturnNotAllowedResult(InstrumentState state)
    {
        // Arrange
        SetupInstrumentInState(state);
        var command = new SendCmrTestOrderCommand(_serialNumber, [1, 2, 3]);

        // Act
        var result = await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Status.Should().Be(CmrTestOrderStatus.NotAllowed);
    }

    [Fact]
    public async Task Handle_WhenSessionIsNull_ShouldThrowNotAllowedException()
    {
        // Arrange
        SetupInstrumentInState(InstrumentState.Standby);
        _sessionManagerMock.Setup(m => m.GetSession(_serialNumber)).Returns((CommunicationSession?)null);
        var command = new SendCmrTestOrderCommand(_serialNumber, [1, 2, 3]);

        // Act
        var act = () => _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<NotAllowedException>();
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5840")]
    [Trait("ProductRequirement", "PR_784")]
    public async Task Handle_WhenStatusIsParseError_ShouldReturnFailedResultWithParseErrorCode()
    {
        // Arrange
        SetupInstrumentInState(InstrumentState.Standby);
        var command = new SendCmrTestOrderCommand(_serialNumber, [1, 2, 3]);
        _connectionMock
            .Setup(c => c.SendCmrTestOrderAsync(It.IsAny<CmrTestOrderRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CmrTestOrderResponse { Status = EnumCmrTestOrderStatus.ParseError });

        // Act
        var result = await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Status.Should().Be(CmrTestOrderStatus.ParseError);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5840")]
    [Trait("ProductRequirement", "PR_784")]
    public async Task Handle_WhenStatusIsInvalid_ShouldReturnFailedResultWithInvalidCode()
    {
        // Arrange
        SetupInstrumentInState(InstrumentState.Standby);
        var command = new SendCmrTestOrderCommand(_serialNumber, [1, 2, 3]);
        _connectionMock
            .Setup(c => c.SendCmrTestOrderAsync(It.IsAny<CmrTestOrderRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CmrTestOrderResponse { Status = EnumCmrTestOrderStatus.Invalid });

        // Act
        var result = await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Status.Should().Be(CmrTestOrderStatus.Invalid);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5840")]
    [Trait("ProductRequirement", "PR_784")]
    public async Task Handle_WhenStatusIsUnspecified_ShouldReturnFailedResultWithUnspecifiedCode()
    {
        // Arrange
        SetupInstrumentInState(InstrumentState.Standby);
        var command = new SendCmrTestOrderCommand(_serialNumber, [1, 2, 3]);
        _connectionMock
            .Setup(c => c.SendCmrTestOrderAsync(It.IsAny<CmrTestOrderRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CmrTestOrderResponse { Status = EnumCmrTestOrderStatus.UnspecifiedCmrTestOrderStatus });

        // Act
        var result = await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Status.Should().Be(CmrTestOrderStatus.Unspecified);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5837")]
    [Trait("ProductRequirement", "PR_784")]
    public async Task Handle_ShouldSendCorrectDataToInstrument()
    {
        // Arrange
        SetupInstrumentInState(InstrumentState.Standby);
        var data = new byte[] { 10, 20, 30 };
        var command = new SendCmrTestOrderCommand(_serialNumber, data);
        CmrTestOrderRequest? capturedRequest = null;

        _connectionMock
            .Setup(c => c.SendCmrTestOrderAsync(It.IsAny<CmrTestOrderRequest>(), It.IsAny<CancellationToken>()))
            .Callback<CmrTestOrderRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new CmrTestOrderResponse { Status = EnumCmrTestOrderStatus.Ok });

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.CustomMadeRunData.ToByteArray().Should().BeEquivalentTo(data);
    }

    private void SetupInstrumentInState(InstrumentState state)
    {
        _instrumentManagerMock
            .Setup(m => m.GetInstrument(_serialNumber))
            .Returns(new Domain.Instrument
            {
                SerialNumber = _serialNumber,
                State = state
            });
    }
}
