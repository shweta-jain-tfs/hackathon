using FluentAssertions;
using Moq;
using Pharos.Client.App.SystemUpdate.Models;
using Pharos.Client.App.SystemUpdate.Notifications;
using Pharos.Client.Common.Models;
using Pharos.Client.Common.Models.DataServices;
using Pharos.Client.Common.Models.Notifications;
using Pharos.Client.ViewModels;

namespace Pharos.Client.Tests.ViewModels;

public class AboutViewModelTests
{
    private readonly Mock<ISingletonDataSource<PharosInformation>> _pharosInfoDataSourceMock = new();
    private readonly Mock<ISingletonDataSource<ModuleInformation>> _moduleInfoDataSourceMock = new();
    private readonly Mock<ICurrentInstrumentNotificationProvider> _instrumentNotifProviderMock = new();
    private readonly Mock<ISystemUpdateNotificationProvider> _systemUpdateNotifProviderMock = new();
    private readonly Mock<ISystemUpdateDataService> _systemUpdateDataServiceMock = new();

    private AboutViewModel CreateSut(
        List<string>? availableUpdates = null,
        string? pharosVersion = "1.0.0",
        string? softwareDeviceIdentifier = "00000000000000",
        DateTimeOffset? softwareManufactureDate = null,
        bool hasSoftwareManufactureDate = true)
    {
        _pharosInfoDataSourceMock.Setup(x => x.GetAsync())
            .ReturnsAsync(new PharosInformation
            {
                PharosVersion = pharosVersion,
                SoftwareDeviceIdentifier = softwareDeviceIdentifier,
                SoftwareManufactureDate = hasSoftwareManufactureDate
                    ? softwareManufactureDate ?? new DateTimeOffset(2026, 8, 25, 0, 0, 0, TimeSpan.Zero)
                    : null
            });
        _moduleInfoDataSourceMock.Setup(x => x.GetAsync())
            .ReturnsAsync(new ModuleInformation { Modules = [] });

        _systemUpdateDataServiceMock
            .Setup(x => x.GetAvailableUpdates())
            .ReturnsAsync(availableUpdates ?? new List<string>());

        _systemUpdateDataServiceMock
            .Setup(x => x.InitiateUpdate())
            .Returns(Task.CompletedTask);

        return new AboutViewModel(
            _pharosInfoDataSourceMock.Object,
            _moduleInfoDataSourceMock.Object,
            _instrumentNotifProviderMock.Object,
            _systemUpdateNotifProviderMock.Object,
            _systemUpdateDataServiceMock.Object);
    }

    [Fact]
    public async Task LoadAsync_SetsAvailableUpdates_WhenUpdatesExist()
    {
        // Arrange
        using var sut = CreateSut(["9.0.1", "9.0.2"]);

        // Act
        await sut.LoadAsync();

        // Assert
        sut.AvailableUpdates.Should().BeEquivalentTo("9.0.1", "9.0.2");
        sut.HasAvailableUpdates.Should().BeTrue();
    }

    [Fact]
    public async Task LoadAsync_ClearsAvailableUpdates_WhenNoUpdates()
    {
        // Arrange
        using var sut = CreateSut(new List<string>());

        // Act
        await sut.LoadAsync();

        // Assert
        sut.AvailableUpdates.Should().BeEmpty();
        sut.HasAvailableUpdates.Should().BeFalse();
    }

    [Fact]
    public void OnUpdatesAvailableEvent_UpdatesProperties_And_RaisesPropertyChanged()
    {
        // Arrange
        EventHandler<List<string>>? updatesAvailableHandler = null;
        _systemUpdateNotifProviderMock
            .SetupAdd(x => x.UpdatesAvailable += It.IsAny<EventHandler<List<string>>>())
            .Callback<EventHandler<List<string>>>(h => updatesAvailableHandler = h);

        using var sut = CreateSut();
        var changed = new List<string?>();
        sut.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        // Act
        var updates = new List<string> { "9.1.0" };
        updatesAvailableHandler!.Invoke(_systemUpdateNotifProviderMock.Object, updates);

        // Assert
        sut.AvailableUpdates.Should().BeEquivalentTo("9.1.0");
        sut.HasAvailableUpdates.Should().BeTrue();
        changed.Should().Contain(nameof(AboutViewModel.AvailableUpdates));
        changed.Should().Contain(nameof(AboutViewModel.HasAvailableUpdates));
    }

    [Fact]
    public async Task OnApplyUpdates_InitiatesUpdateCommand()
    {
        // Arrange
        using var sut = CreateSut();

        // Act
        await sut.ApplyUpdatesAsync();

        // Assert
        _systemUpdateDataServiceMock.Verify(x => x.InitiateUpdate(), Times.Once);
    }

    [Fact]
    public async Task LoadAsync_SetsSoftwareLabelInformation()
    {
        // Arrange
        using var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        sut.PharosVersion.Should().Be("1.0.0");
        sut.SoftwareUdiVersion.Should().Be("010000");
        sut.SoftwareDeviceIdentifier.Should().Be("00000000000000");
        sut.SoftwareManufactureDate.Should().Be(new DateTimeOffset(2026, 8, 25, 0, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task LoadAsync_UsesSoftwareLabelFallbacks_WhenValuesAreMissing()
    {
        // Arrange
        using var sut = CreateSut(
            pharosVersion: "",
            softwareDeviceIdentifier: null,
            hasSoftwareManufactureDate: false);

        // Act
        await sut.LoadAsync();

        // Assert
        sut.PharosVersion.Should().Be("0.0.0");
        sut.SoftwareUdiVersion.Should().Be("000000");
        sut.SoftwareDeviceIdentifier.Should().Be("00000000000000");
        sut.SoftwareManufactureDate.Should().BeNull();
    }

    [Fact]
    public async Task LoadAsync_FormatsSoftwareUdiVersionLikePrime()
    {
        // Arrange
        using var sut = CreateSut(pharosVersion: "2.7.22.1");

        // Act
        await sut.LoadAsync();

        // Assert
        sut.SoftwareUdiVersion.Should().Be("02072200001");
    }

    [Fact]
    public async Task OnApplyUpdates_SetsIsUpdatingProperty()
    {
        // Arrange
        using var sut = CreateSut();

        // Act
        await sut.ApplyUpdatesAsync();

        // Assert
        sut.IsUpdating.Should().BeTrue();
    }

    [Fact]
    public void OnUpdateFailure_ClearsIsUpdatingProperty()
    {
        // Arrange
        EventHandler? updateFailedHandler = null;
        _systemUpdateNotifProviderMock
            .SetupAdd(x => x.UpdateFailed += It.IsAny<EventHandler>())
            .Callback<EventHandler>(h => updateFailedHandler = h);
        using var sut = CreateSut();

        // Act
        sut.IsUpdating = true;
        updateFailedHandler!.Invoke(_systemUpdateNotifProviderMock.Object, null!);

        // Assert
        sut.IsUpdating.Should().BeFalse();
    }

    [Fact]
    public void Dispose_UnsubscribesAvailableUpdateNotification()
    {
        // Arrange
        var subscribedHandler = (EventHandler<List<string>>?)null;
        var removedHandler = (EventHandler<List<string>>?)null;

        _systemUpdateNotifProviderMock
            .SetupAdd(x => x.UpdatesAvailable += It.IsAny<EventHandler<List<string>>>())
            .Callback<EventHandler<List<string>>>(h => subscribedHandler = h);

        _systemUpdateNotifProviderMock
            .SetupRemove(x => x.UpdatesAvailable -= It.IsAny<EventHandler<List<string>>>())
            .Callback<EventHandler<List<string>>>(h => removedHandler = h);

        var sut = CreateSut();

        // Act
        sut.Dispose();

        // Assert
        removedHandler.Should().Be(subscribedHandler);
    }

    [Fact]
    public void Dispose_UnsubscribesUpdateFailedNotification()
    {
        // Arrange
        var subscribedHandler = (EventHandler?)null;
        var removedHandler = (EventHandler?)null;

        _systemUpdateNotifProviderMock
            .SetupAdd(x => x.UpdateFailed += It.IsAny<EventHandler>())
            .Callback<EventHandler>(h => subscribedHandler = h);

        _systemUpdateNotifProviderMock
            .SetupRemove(x => x.UpdateFailed -= It.IsAny<EventHandler>())
            .Callback<EventHandler>(h => removedHandler = h);

        var sut = CreateSut();

        // Act
        sut.Dispose();

        // Assert
        removedHandler.Should().Be(subscribedHandler);
    }
}
