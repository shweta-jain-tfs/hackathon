using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.SystemUpdate.Application.Events;
using Pharos.Server.SystemUpdate.Application.Interfaces;
using Pharos.Server.SystemUpdate.Domain;
using Pharos.Contracts.Notifications;

namespace Pharos.Server.SystemUpdate.Tests.Application.Events;
public static class UsbInsertedHandlerTests
{
    public class TheOnDriveInsertedMethod
    {
        private readonly UsbInsertedHandler _sut;
        private readonly Mock<IUpdateService> _mockUpdateService;
        private readonly Mock<IUpdateMediumStateStore> _mockStateStore;
        private readonly Mock<ILogger<UsbInsertedHandler>> _logger;
        private readonly Mock<ISender> _sender;
        private readonly Mock<ISystemUpdateHub> _systemUpdateHub;
        private readonly Mock<IUpdateServiceFactory> _mockFactory;
        private readonly Mock<IUpdateMediumValidator> _updateMediumValidator;
        private readonly string _driveName;

        public TheOnDriveInsertedMethod()
        {
            _driveName = "E:\\";
            _mockStateStore = new Mock<IUpdateMediumStateStore>();
            _mockStateStore.Setup(s => s.Get()).Returns((UpdateMediumState?)null);

            _mockFactory = new Mock<IUpdateServiceFactory>();
            _mockUpdateService = new Mock<IUpdateService>();
            _mockUpdateService.Setup(s => s.GetAvailableUpdates(It.IsAny<CancellationToken>()))
                .Returns(new List<string>());

            _mockFactory.Setup(f => f.Resolve(UpdateSource.Usb))
                .Returns(_mockUpdateService.Object);

            _sender = new Mock<ISender>();
            _logger = new Mock<ILogger<UsbInsertedHandler>>();
            _systemUpdateHub = new Mock<ISystemUpdateHub>();

            _updateMediumValidator = new Mock<IUpdateMediumValidator>();
            _updateMediumValidator.Setup(v => v.IsValidUpdateMedium(It.IsAny<string>())).Returns(true);

            _sut = new UsbInsertedHandler(
                _mockFactory.Object,
                _mockStateStore.Object,
                _sender.Object,
                _systemUpdateHub.Object,
                _updateMediumValidator.Object,
                _logger.Object);
        }

        [Fact]
        public async Task IgnoresEvent_WhenDriveNameNotPresent()
        {
            // Act
            await _sut.OnDriveInsertedAsync(string.Empty, TestContext.Current.CancellationToken);

            // Assert
            _mockUpdateService.Verify(m => m.GetAvailableUpdates(It.IsAny<CancellationToken>()), Times.Never);
            _systemUpdateHub.Verify(h => h.UpdatesAvailable(It.IsAny<List<string>>()), Times.Never);
        }

        [Fact]
        public async Task IgnoresEvent_WhenStateStoreIsAlreadySet()
        {
            // Arrange
            _mockStateStore.Setup(s => s.Get()).Returns(new UpdateMediumState(_driveName));

            // Act
            await _sut.OnDriveInsertedAsync(_driveName, TestContext.Current.CancellationToken);

            // Assert
            _mockUpdateService.Verify(m => m.GetAvailableUpdates(It.IsAny<CancellationToken>()), Times.Never);
            _systemUpdateHub.Verify(h => h.UpdatesAvailable(It.IsAny<List<string>>()), Times.Never);
        }

        [Fact]
        public async Task ResolvesServiceForUsbUpdates()
        {
            // Act
            await _sut.OnDriveInsertedAsync(_driveName, TestContext.Current.CancellationToken);

            // Assert
            _mockFactory.Verify(f => f.Resolve(UpdateSource.Usb), Times.Once);
        }

        [Fact]
        public async Task LogsException_WhenResolvingServiceFails()
        {
            // Arrange
            _mockFactory.Setup(f => f.Resolve(UpdateSource.Usb))
                .Throws(new InvalidOperationException("Test exception"));

            // Act
            await _sut.OnDriveInsertedAsync(_driveName, TestContext.Current.CancellationToken);

            // Assert
            _logger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v!.ToString()!.Contains(_driveName)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetsAvailableUpdates()
        {
            // Act
            await _sut.OnDriveInsertedAsync(_driveName, TestContext.Current.CancellationToken);

            // Assert
            _mockUpdateService.Verify(m => m.GetAvailableUpdates(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SetsStateStore_WhenInsertedMediumIsValidUpdateMedium()
        {
            // Act
            await _sut.OnDriveInsertedAsync(_driveName, TestContext.Current.CancellationToken);

            // Assert
            _mockStateStore.Verify(s => s.Set(It.Is<UpdateMediumState>(state =>
                state.DriveName == _driveName
            )), Times.Once);
        }

        [Fact]
        public async Task DoesNotSetStateStore_WhenInsertedMediumIsNotValidUpdateMedium()
        {
            // Arrange
            _updateMediumValidator.Setup(v => v.IsValidUpdateMedium(It.IsAny<string>())).Returns(false);

            // Act
            await _sut.OnDriveInsertedAsync(_driveName, TestContext.Current.CancellationToken);

            // Assert
            _mockStateStore.Verify(s => s.Set(It.IsAny<UpdateMediumState>()), Times.Never);
        }

        [Fact]
        public async Task LogsException_WhenGetAvailableUpdatesThrows()
        {
            // Arrange
            _mockUpdateService.Setup(s => s.GetAvailableUpdates(It.IsAny<CancellationToken>()))
                .Throws(new InvalidOperationException("Test exception"));

            // Act
            await _sut.OnDriveInsertedAsync(_driveName, TestContext.Current.CancellationToken);

            // Assert
            _logger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v!.ToString()!.Contains(_driveName)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task SendsTaskCardCommand_WhenUpdatesAreFound()
        {
            // Arrange
            var availableUpdates = new List<string> { "Update1", "Update2" };
            _mockUpdateService.Setup(s => s.GetAvailableUpdates(It.IsAny<CancellationToken>()))
                .Returns(availableUpdates);

            // Act
            await _sut.OnDriveInsertedAsync(_driveName, TestContext.Current.CancellationToken);

            // Assert
            _sender.Verify(s => s.Send(
                It.Is<TaskCardCommand>(cmd => cmd.CardType == TaskCardType.UpdatesAvailable),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DoesNotSendTaskCardCommand_WhenNoUpdatesFound()
        {
            // Act
            await _sut.OnDriveInsertedAsync(_driveName, TestContext.Current.CancellationToken);

            // Assert
            _sender.Verify(s => s.Send(
                It.IsAny<TaskCardCommand>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task PublishesUpdatesToHub_WhenUpdatesAreFound()
        {
            // Arrange
            var availableUpdates = new List<string> { "Update1.msi", "Update2.exe" };
            _mockUpdateService.Setup(s => s.GetAvailableUpdates(It.IsAny<CancellationToken>()))
                .Returns(availableUpdates);

            // Act
            await _sut.OnDriveInsertedAsync(_driveName, TestContext.Current.CancellationToken);

            // Assert
            _systemUpdateHub.Verify(h => h.UpdatesAvailable(
                It.Is<List<string>>(l => l.SequenceEqual(availableUpdates))), Times.Once);
        }

        [Fact]
        public async Task DoesNotPublishToHub_WhenNoUpdatesFound()
        {
            // Act
            await _sut.OnDriveInsertedAsync(_driveName, TestContext.Current.CancellationToken);

            // Assert
            _systemUpdateHub.Verify(h => h.UpdatesAvailable(It.IsAny<List<string>>()), Times.Never);
        }

        [Fact]
        public async Task DoesNotGetAvailableUpdates_BeforeStateStoreIsSet()
        {
            // Arrange
            var callOrder = new List<string>();
            _mockUpdateService.Setup(m => m.GetAvailableUpdates(It.IsAny<CancellationToken>()))
                .Callback(() => callOrder.Add("GetAvailableUpdates"))
                .Returns(new List<string>());
            _mockStateStore.Setup(s => s.Set(It.IsAny<UpdateMediumState>()))
                .Callback(() => callOrder.Add("SetStateStore"));

            // Act
            await _sut.OnDriveInsertedAsync(_driveName, TestContext.Current.CancellationToken);

            // Assert
            var getIndex = callOrder.IndexOf("GetAvailableUpdates");
            var setIndex = callOrder.IndexOf("SetStateStore");
            Assert.True(setIndex < getIndex, "State store should be set before getting available updates.");
        }
    }
}
