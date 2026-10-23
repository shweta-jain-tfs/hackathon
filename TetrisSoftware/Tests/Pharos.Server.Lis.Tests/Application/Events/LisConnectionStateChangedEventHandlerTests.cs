using MediatR;
using Pharos.Contracts.Notifications;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Lis.Application.Events;
using Pharos.Server.Lis.Application.Interfaces;
using Pharos.Server.Lis.Application.Queries;

namespace Pharos.Server.Lis.Tests.Application.Events;

public class LisConnectionStateChangedEventHandlerTests
{
    private readonly Mock<ILisConnectionHub> _hubMock;
    private readonly Mock<ISender> _senderMock;
    private readonly LisConnectionStateEventHandler _handler;

    public LisConnectionStateChangedEventHandlerTests()
    {
        _hubMock = new Mock<ILisConnectionHub>();
        _senderMock = new Mock<ISender>();

        _handler = new LisConnectionStateEventHandler(_hubMock.Object, _senderMock.Object);
    }

    [Trait("ProductRequirement", "PR_950")]
    [Fact]
    public async Task Handle_WhenAllConnectionsAreOpen_ShouldRemoveConnectionFailureTaskCard()
    {
        // Arrange
        var lisConnections = new[]
        {
            new LisConnectionDto { Id = 0, State = LisConnectionState.Open },
            new LisConnectionDto { Id = 1, State = LisConnectionState.Open }
        };

        _senderMock
            .Setup(x => x.Send(It.IsAny<GetLisQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lisConnections);

        var notification = new LisConnectionStateChangedEvent();

        // Act
        await _handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        _senderMock.Verify(
            x => x.Send(
                It.Is<RemoveTaskCardCommand>(cmd => cmd.CardType == TaskCardType.ConnectionFailure),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "Should remove ConnectionFailure task card when all connections are open");

        _senderMock.Verify(
            x => x.Send(
                It.IsAny<TaskCardCommand>(),
                It.IsAny<CancellationToken>()),
            Times.Never,
            "Should not create a new task card when all connections are open");

        _hubMock.Verify(x => x.LisStatusChanged(), Times.Once);
    }

    [Trait("ProductRequirement", "PR_950")]
    [Fact]
    public async Task Handle_WhenAnyConnectionIsNotOpen_ShouldRaiseConnectionFailureTaskCard()
    {
        // Arrange
        var lisConnections = new[]
        {
            new LisConnectionDto { Id = 0, State = LisConnectionState.Open },
            new LisConnectionDto { Id = 1, State = LisConnectionState.Closed }
        };

        _senderMock
            .Setup(x => x.Send(It.IsAny<GetLisQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lisConnections);

        var notification = new LisConnectionStateChangedEvent();

        // Act
        await _handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        _senderMock.Verify(
            x => x.Send(
                It.Is<TaskCardCommand>(cmd =>
                    cmd.CardType == TaskCardType.ConnectionFailure &&
                    cmd.Rules.Any(r => r.CardState == TaskCardState.Error) &&
                    cmd.InstrumentNumber == null),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "Should raise ConnectionFailure task card with Error state when any connection is not open");

        _senderMock.Verify(
            x => x.Send(
                It.IsAny<RemoveTaskCardCommand>(),
                It.IsAny<CancellationToken>()),
            Times.Never,
            "Should not remove task card when connections are not all open");

        _hubMock.Verify(x => x.LisStatusChanged(), Times.Once);
    }
}
