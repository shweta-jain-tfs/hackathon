using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Pharos.Contracts.Notifications;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.SystemUpdate.Application.Events;
using Pharos.Server.SystemUpdate.Application.Interfaces;
using Pharos.Server.SystemUpdate.Domain;

namespace Pharos.Server.SystemUpdate.Tests.Application.Events;

public static class UsbRemovedHandlerTests
{
    public class TheOnDriveRemovedMethod
    {
        private readonly UsbRemovedHandler _sut;
        private readonly Mock<IUpdateMediumStateStore> _mockStateStore;
        private readonly Mock<ISender> _sender;
        private readonly Mock<ISystemUpdateHub> _systemUpdateHub;
        private readonly Mock<ILogger<UsbRemovedHandler>> _logger;

        public TheOnDriveRemovedMethod()
        {
            _mockStateStore = new Mock<IUpdateMediumStateStore>();
            _sender = new Mock<ISender>();
            _systemUpdateHub = new Mock<ISystemUpdateHub>();
            _logger = new Mock<ILogger<UsbRemovedHandler>>();

            _sut = new UsbRemovedHandler(
                _mockStateStore.Object,
                _sender.Object,
                _systemUpdateHub.Object,
                _logger.Object);
        }

        [Fact]
        public async Task IgnoresEvent_WhenStateNotSet()
        {
            // Arrange
            const string DriveName = "E:\\";
            _mockStateStore.Setup(s => s.Get()).Returns((UpdateMediumState?)null);

            // Act
            await _sut.OnDriveRemovedAsync(DriveName, TestContext.Current.CancellationToken);

            // Assert
            _sender.Verify(m => m.Send(It.IsAny<RemoveTaskCardCommand>(), It.IsAny<CancellationToken>()), Times.Never);
            _systemUpdateHub.Verify(h => h.UpdatesAvailable(It.IsAny<List<string>>()), Times.Never);
        }

        [Fact]
        public async Task IgnoresEvent_WhenDriveNameDoesNotMatch()
        {
            // Arrange
            const string DriveName = "E:\\";
            _mockStateStore.Setup(s => s.Get()).Returns(new UpdateMediumState("F:\\"));

            // Act
            await _sut.OnDriveRemovedAsync(DriveName, TestContext.Current.CancellationToken);

            // Assert
            _mockStateStore.Verify(s => s.Clear(), Times.Never);
            _sender.Verify(m => m.Send(It.IsAny<RemoveTaskCardCommand>(), It.IsAny<CancellationToken>()), Times.Never);
            _systemUpdateHub.Verify(h => h.UpdatesAvailable(It.IsAny<List<string>>()), Times.Never);
        }

        [Fact]
        public async Task ClearsStateStore_WhenDriveNameMatches()
        {
            // Arrange
            const string DriveName = "E:\\";
            _mockStateStore.Setup(s => s.Get()).Returns(new UpdateMediumState(DriveName));

            // Act
            await _sut.OnDriveRemovedAsync(DriveName, TestContext.Current.CancellationToken);

            // Assert
            _mockStateStore.Verify(s => s.Clear(), Times.Once);
        }

        [Fact]
        public async Task SendsRemoveTaskCardCommand_WhenDriveNameMatches()
        {
            // Arrange
            const string DriveName = "E:\\";
            _mockStateStore.Setup(s => s.Get()).Returns(new UpdateMediumState(DriveName));

            // Act
            await _sut.OnDriveRemovedAsync(DriveName, TestContext.Current.CancellationToken);

            // Assert
            _sender.Verify(s => s.Send(
                It.Is<RemoveTaskCardCommand>(cmd => cmd.CardType == TaskCardType.UpdatesAvailable),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task PublishesEmptyUpdatesToHub_WhenDriveNameMatches()
        {
            // Arrange
            const string DriveName = "E:\\";
            _mockStateStore.Setup(s => s.Get()).Returns(new UpdateMediumState(DriveName));

            // Act
            await _sut.OnDriveRemovedAsync(DriveName, TestContext.Current.CancellationToken);

            // Assert
            _systemUpdateHub.Verify(h => h.UpdatesAvailable(
                It.Is<List<string>>(l => l.Count == 0)), Times.Once);
        }
    }
}
