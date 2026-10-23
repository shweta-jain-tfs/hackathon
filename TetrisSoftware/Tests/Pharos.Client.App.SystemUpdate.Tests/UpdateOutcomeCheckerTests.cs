using FluentAssertions;
using Microsoft.Extensions.Localization;
using Moq;
using Pharos.Client.App.SystemUpdate.Models;
using Pharos.Client.App.SystemUpdate.Notifications;
using Pharos.Client.App.SystemUpdate.Services;
using Pharos.Client.App.SystemUpdate.Views;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.Models;
using Pharos.Client.Common.Models.Services;
using Pharos.Common.Web.Components.Modal;
using Xunit;

namespace Pharos.Client.App.SystemUpdate.Tests;

public class UpdateOutcomeCheckerTests : IAsyncDisposable
{
    private readonly UpdateOutcomeChecker _sut;
    private readonly Mock<ISystemUpdateNotificationProvider> _notificationProvider;
    private readonly Mock<IConnectionHealthService> _connectionHealthService;
    private readonly Mock<ISystemUpdateDataService> _dataService;
    private readonly Mock<IModalService> _modalService;
    private readonly Mock<IModalReference> _modalReference;
    private readonly Mock<IModalReference> _inProgressModalReference;
    private readonly Mock<IAppNavigationService> _navigation;

    protected UpdateOutcomeCheckerTests()
    {
        _notificationProvider = new Mock<ISystemUpdateNotificationProvider>();
        _connectionHealthService = new Mock<IConnectionHealthService>();
        _dataService = new Mock<ISystemUpdateDataService>();
        _modalService = new Mock<IModalService>();
        _modalReference = new Mock<IModalReference>();
        _inProgressModalReference = new Mock<IModalReference>();
        _navigation = new Mock<IAppNavigationService>();
        var localizer = new Mock<IStringLocalizer<Translations>>();

        localizer
            .Setup(x => x[It.IsAny<string>()])
            .Returns<string>(key => new LocalizedString(key, key));

        _dataService
            .Setup(x => x.GetUnacknowledgedOutcome())
            .ReturnsAsync(new SystemUpdateOutcome { IsAcknowledged = true, IsSuccessful = true });

        _dataService
            .Setup(x => x.AcknowledgeUpdate())
            .Returns(Task.CompletedTask);

        _modalReference
            .SetupGet(x => x.Result)
            .Returns(Task.FromResult(ModalResult.Ok()));

        _modalService
            .Setup(x => x.ShowInformation(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ModalOptions>(), It.IsAny<bool>()))
            .Returns(_modalReference.Object);

        _modalService
            .Setup(x => x.Show<UpdateInProgressView>(It.IsAny<ModalOptions>()))
            .Returns(_inProgressModalReference.Object);

        _sut = new UpdateOutcomeChecker(
            dataService: _dataService.Object,
            notificationProvider: _notificationProvider.Object,
            modalService: _modalService.Object,
            connectionHealthService: _connectionHealthService.Object,
            navigation: _navigation.Object,
            loc: localizer.Object);
    }

    public class TheEnsureInitializedMethod : UpdateOutcomeCheckerTests
    {
        [Fact]
        public async Task InitializesOnlyOnce()
        {
            // Arrange
            var initializationCount = 0;
            _notificationProvider.SetupAdd(x => x.UpdateFailed += It.IsAny<EventHandler>())
                .Callback(() => initializationCount++);
            _notificationProvider.SetupAdd(x => x.UpdateOutcomeAcknowledged += It.IsAny<EventHandler>())
                .Callback(() => initializationCount++);
            _connectionHealthService.SetupAdd(x => x.ConnectionRestored += It.IsAny<Func<Task>>())
                .Callback(() => initializationCount++);

            // Act
            await _sut.EnsureInitializedAsync();
            await _sut.EnsureInitializedAsync();    // Call a second time to verify that initialization logic is not executed again

            // Assert
            initializationCount.Should().Be(3);
        }

        [Theory]
        [InlineData(true, ModalStatus.Info)]
        [InlineData(false, ModalStatus.Warning)]
        public async Task ShowsModal_WhenUnacknowledgedOutcomeExists(bool isSuccessful, ModalStatus modalType)
        {
            // Arrange
            _dataService
                .Setup(x => x.GetUnacknowledgedOutcome())
                .ReturnsAsync(new SystemUpdateOutcome { IsAcknowledged = false, IsSuccessful = isSuccessful });

            // Act
            await _sut.EnsureInitializedAsync();

            // Assert
            _modalService.Verify(
                x => x.ShowInformation(It.IsAny<string>(), It.IsAny<string>(),
                    It.Is<ModalOptions>(o => o.Status == modalType), It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task DoesNotShowModal_WhenOutcomeIsAlreadyAcknowledged()
        {
            // Arrange
            _dataService
                .Setup(x => x.GetUnacknowledgedOutcome())
                .ReturnsAsync(new SystemUpdateOutcome { IsAcknowledged = true, IsSuccessful = true });

            // Act
            await _sut.EnsureInitializedAsync();

            // Assert
            _modalService.Verify(
                x => x.ShowInformation(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ModalOptions>(), It.IsAny<bool>()),
                Times.Never);
        }

        [Fact]
        public async Task AcknowledgesUpdate_WhenUpdateIsNotAcknowledged_AndModalClosed()
        {
            // Arrange
            _dataService
                .Setup(x => x.GetUnacknowledgedOutcome())
                .ReturnsAsync(new SystemUpdateOutcome { IsAcknowledged = false, IsSuccessful = true });

            _modalReference
                .SetupGet(x => x.Result)
                .Returns(Task.FromResult(ModalResult.Ok()));    // Simulate user closing the modal

            // Act
            await _sut.EnsureInitializedAsync();

            // Assert
            _dataService.Verify(x => x.AcknowledgeUpdate(), Times.Once);
        }
    }

    public class TheUpdateFailedEventHandler : UpdateOutcomeCheckerTests
    {
        [Fact]
        public async Task DisplaysUpdateFailureModal()
        {
            // Arrange
            EventHandler? updateFailedHandler = null;
            _notificationProvider
                .SetupAdd(x => x.UpdateFailed += It.IsAny<EventHandler>())
                .Callback<EventHandler>(h => updateFailedHandler = h);

            await _sut.EnsureInitializedAsync();

            // Act
            updateFailedHandler!.Invoke(_notificationProvider.Object, EventArgs.Empty);

            // Assert
            _modalService.Verify(
                x => x.ShowInformation(It.IsAny<string>(), It.IsAny<string>(),
                    It.Is<ModalOptions>(o => o.Status == ModalStatus.Warning), It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task ClosesInProgressModal_WhenUpdateFailed()
        {
            // Arrange
            EventHandler? updateFailedHandler = null;
            _notificationProvider
                .SetupAdd(x => x.UpdateFailed += It.IsAny<EventHandler>())
                .Callback<EventHandler>(h => updateFailedHandler = h);

            EventHandler? updateInProgressHandler = null;
            _notificationProvider
                .SetupAdd(x => x.UpdateInProgress += It.IsAny<EventHandler>())
                .Callback<EventHandler>(h => updateInProgressHandler = h);

            await _sut.EnsureInitializedAsync();

            updateInProgressHandler!.Invoke(_notificationProvider.Object, EventArgs.Empty);   // Simulate update in progress to show the in progress modal

            // Act
            updateFailedHandler!.Invoke(_notificationProvider.Object, EventArgs.Empty);

            // Assert
            _modalService.Verify(x => x.CloseModal(_inProgressModalReference.Object), Times.Once);
        }
    }

    public class TheConnectionRestoredHandler : UpdateOutcomeCheckerTests
    {
        [Fact]
        public async Task ChecksForUnacknowledgedUpdate()
        {
            // Arrange
            Func<Task>? connectionRestoredHandler = null;
            _connectionHealthService
                .SetupAdd(x => x.ConnectionRestored += It.IsAny<Func<Task>>())
                .Callback<Func<Task>>(h => connectionRestoredHandler = h);

            _dataService
                .Setup(x => x.GetUnacknowledgedOutcome())
                .ReturnsAsync(new SystemUpdateOutcome { IsAcknowledged = true, IsSuccessful = true });

            await _sut.EnsureInitializedAsync();
            _dataService.Invocations.Clear();   // Clear invocations from initialization to verify that it's called when connection is restored

            // Act
            await connectionRestoredHandler!.Invoke();

            // Assert
            _dataService.Verify(x => x.GetUnacknowledgedOutcome(), Times.Once);
        }

        [Fact]
        public async Task ClosesInProgressModal_WhenConnectionRestored()
        {
            // Arrange
            Func<Task>? connectionRestoredHandler = null;
            _connectionHealthService
                .SetupAdd(x => x.ConnectionRestored += It.IsAny<Func<Task>>())
                .Callback<Func<Task>>(h => connectionRestoredHandler = h);

            EventHandler? updateInProgressHandler = null;
            _notificationProvider
                .SetupAdd(x => x.UpdateInProgress += It.IsAny<EventHandler>())
                .Callback<EventHandler>(h => updateInProgressHandler = h);

            _dataService
                .Setup(x => x.GetUnacknowledgedOutcome())
                .ReturnsAsync(new SystemUpdateOutcome { IsAcknowledged = false, IsSuccessful = true });

            await _sut.EnsureInitializedAsync();

            updateInProgressHandler!.Invoke(_notificationProvider.Object, EventArgs.Empty);   // Simulate update in progress to show the in progress modal

            // Act
            await connectionRestoredHandler!.Invoke();

            // Assert
            _modalService.Verify(x => x.CloseModal(_inProgressModalReference.Object), Times.Once);
        }
    }

    public class TheUpdateOutcomeAcknowledgedHandler : UpdateOutcomeCheckerTests
    {
        [Fact]
        public async Task ReloadsPage_WhenSuccessfulUpdate()
        {
            // Arrange
            EventHandler? acknowledgedHandler = null;
            _notificationProvider
                .SetupAdd(x => x.UpdateOutcomeAcknowledged += It.IsAny<EventHandler>())
                .Callback<EventHandler>(h => acknowledgedHandler = h);

            _dataService
                .Setup(x => x.GetUnacknowledgedOutcome())
                .ReturnsAsync(new SystemUpdateOutcome { IsAcknowledged = false, IsSuccessful = true });

            await _sut.EnsureInitializedAsync();

            // Act
            acknowledgedHandler!.Invoke(_notificationProvider.Object, EventArgs.Empty);

            // Assert
            _navigation.Verify(n => n.Reload(), Times.Once);
        }

        [Fact]
        public async Task DoesNotReloadPage_WhenFailedUpdate()
        {
            // Arrange
            EventHandler? acknowledgedHandler = null;
            _notificationProvider
                .SetupAdd(x => x.UpdateOutcomeAcknowledged += It.IsAny<EventHandler>())
                .Callback<EventHandler>(h => acknowledgedHandler = h);

            await _sut.EnsureInitializedAsync();

            // Act
            acknowledgedHandler!.Invoke(_notificationProvider.Object, EventArgs.Empty);

            // Assert
            _navigation.Verify(n => n.Reload(), Times.Never);
        }
    }

    public class TheUpdateInProgressEventHandler : UpdateOutcomeCheckerTests
    {
        [Fact]
        public async Task DisplaysInProgressModal()
        {
            // Arrange
            EventHandler? updateInProgressHandler = null;
            _notificationProvider
                .SetupAdd(x => x.UpdateInProgress += It.IsAny<EventHandler>())
                .Callback<EventHandler>(h => updateInProgressHandler = h);

            await _sut.EnsureInitializedAsync();

            // Act
            updateInProgressHandler!.Invoke(_notificationProvider.Object, EventArgs.Empty);

            // Assert
            _modalService.Verify(
                x => x.Show<UpdateInProgressView>(It.Is<ModalOptions>(o =>
                    o.ShowCloseButton == false &&
                    o.CloseOnNavigation == false &&
                    o.Status == ModalStatus.Info)),
                Times.Once);
        }
    }

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        await _sut.DisposeAsync();
    }
}
