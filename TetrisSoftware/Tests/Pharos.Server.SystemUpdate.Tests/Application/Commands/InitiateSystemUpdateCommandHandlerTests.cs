using Microsoft.Extensions.Logging;
using Moq;
using Pharos.Contracts.Notifications;
using Pharos.Server.SystemUpdate.Application.Commands;
using Pharos.Server.SystemUpdate.Application.Interfaces;
using Pharos.Server.SystemUpdate.Domain;

namespace Pharos.Server.SystemUpdate.Tests.Application.Commands;

public static class InitiateSystemUpdateCommandHandlerTests
{
    public class TheHandleMethod
    {
        private readonly Mock<IUpdateServiceFactory> _factory;
        private readonly Mock<IUpdateMediumStateStore> _stateStore;
        private readonly Mock<IUpdateService> _updateService;
        private readonly InitiateSystemUpdateCommandHandler _sut;
        private readonly Mock<ISystemUpdateHub> _systemUpdateHub;
        private readonly Mock<IUpdateOutcomeEvaluator> _updateEvaluator;

        public TheHandleMethod()
        {
            _factory = new Mock<IUpdateServiceFactory>();
            _stateStore = new Mock<IUpdateMediumStateStore>();
            _updateService = new Mock<IUpdateService>();
            _systemUpdateHub = new Mock<ISystemUpdateHub>();
            _updateEvaluator = new Mock<IUpdateOutcomeEvaluator>();

            var driveName = "E:";
            _stateStore.Setup(s => s.Get()).Returns(new UpdateMediumState(driveName));

            _sut = new InitiateSystemUpdateCommandHandler(_factory.Object, _stateStore.Object, _systemUpdateHub.Object, _updateEvaluator.Object, new Mock<ILogger<InitiateSystemUpdateCommandHandler>>().Object);
        }

        [Fact]
        public async Task InitiatesUsbUpdate_WhenUsbMediumStatePresent()
        {
            // Arrange
            _factory.Setup(f => f.Resolve(UpdateSource.Usb)).Returns(_updateService.Object);

            // Act
            await _sut.Handle(new InitiateSystemUpdateCommand(), TestContext.Current.CancellationToken);

            // Assert
            _updateService.Verify(s => s.InitiateUpdate(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task InitiatesRemoteUpdate_WhenNoUsbMediumStatePresent()
        {
            // Arrange
            _stateStore.Setup(s => s.Get()).Returns((UpdateMediumState?)null);
            _factory.Setup(f => f.Resolve(UpdateSource.Remote)).Returns(_updateService.Object);

            // Act
            await _sut.Handle(new InitiateSystemUpdateCommand(), TestContext.Current.CancellationToken);

            // Assert
            _updateService.Verify(s => s.InitiateUpdate(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task PublishesInstallFailure_WhenInitiateUpdateThrows()
        {
            // Arrange
            _factory.Setup(f => f.Resolve(UpdateSource.Usb)).Returns(_updateService.Object);
            _updateService.Setup(s => s.InitiateUpdate(It.IsAny<CancellationToken>())).ThrowsAsync(new UpdateFailedException("Update initiation failed"));

            // Act
            await _sut.Handle(new InitiateSystemUpdateCommand(), TestContext.Current.CancellationToken);

            // Assert
            _systemUpdateHub.Verify(m => m.UpdateFailed(), Times.Once);
        }

        [Fact]
        public async Task EvaluatesUpdateOutcome_WhenInitiateUpdateThrows()
        {
            // Arrange
            _factory.Setup(f => f.Resolve(UpdateSource.Usb)).Returns(_updateService.Object);
            _updateService.Setup(s => s.InitiateUpdate(It.IsAny<CancellationToken>())).ThrowsAsync(new UpdateFailedException("Update initiation failed"));

            // Act
            await _sut.Handle(new InitiateSystemUpdateCommand(), TestContext.Current.CancellationToken);

            // Assert
            _updateEvaluator.Verify(e => e.EvaluateUpdateOutcome(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task PublishesUpdateInProgress()
        {
            // Arrange
            _factory.Setup(f => f.Resolve(UpdateSource.Usb)).Returns(_updateService.Object);

            // Act
            await _sut.Handle(new InitiateSystemUpdateCommand(), TestContext.Current.CancellationToken);

            // Assert
            _systemUpdateHub.Verify(h => h.UpdateInProgress(), Times.Once);
        }
    }
}