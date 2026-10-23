using Moq;
using Pharos.Server.SystemUpdate.Application.Interfaces;
using Pharos.Server.SystemUpdate.Application.Queries;
using Pharos.Server.SystemUpdate.Domain;

namespace Pharos.Server.SystemUpdate.Tests.Application.Queries;

public class GetAvailableUpdatesQueryHandlerTests
{
    private readonly Mock<IUpdateServiceFactory> _factory = new();
    private readonly Mock<IUpdateMediumStateStore> _state = new();
    private readonly Mock<IUpdateService> _updateService = new();

    public GetAvailableUpdatesQueryHandlerTests()
    {
        _factory.Setup(f => f.Resolve(UpdateSource.Usb)).Returns(_updateService.Object);
    }

    [Fact]
    public async Task ReturnsEmptyList_WhenStateIsCleared()
    {
        // Arrange
        _state.Setup(s => s.Get()).Returns((UpdateMediumState?)null);
        var sut = new GetAvailableUpdatesQueryHandler(_factory.Object, _state.Object);

        // Act
        var result = await sut.Handle(new GetAvailableUpdatesQuery(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ReturnsEmptyList_WhenDriveNameIsEmpty()
    {
        // Arrange
        _state.Setup(s => s.Get()).Returns(new UpdateMediumState(""));
        var sut = new GetAvailableUpdatesQueryHandler(_factory.Object, _state.Object);

        // Act
        var result = await sut.Handle(new GetAvailableUpdatesQuery(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ReturnsPackages_WhenUsbUpdatesAvailable()
    {
        // Arrange
        const string drive = "E:\\";
        var expected = new List<string> { "Update1.msi", "Update2.exe" };

        _state.Setup(s => s.Get()).Returns(new UpdateMediumState(drive));

        _updateService.Setup(u => u.GetAvailableUpdates(It.IsAny<CancellationToken>()))
            .Returns(expected);

        var sut = new GetAvailableUpdatesQueryHandler(_factory.Object, _state.Object);

        // Act
        var result = await sut.Handle(new GetAvailableUpdatesQuery(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task ReturnsEmptyList_WhenUsbUpdatesAreNotAvailable()
    {
        // Arrange
        const string drive = "E:\\";

        _state.Setup(s => s.Get()).Returns(new UpdateMediumState(drive));

        _updateService.Setup(u => u.GetAvailableUpdates(It.IsAny<CancellationToken>()))
            .Returns(new List<string>());   // Empty list; no updates available

        var sut = new GetAvailableUpdatesQueryHandler(_factory.Object, _state.Object);

        // Act
        var result = await sut.Handle(new GetAvailableUpdatesQuery(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ReturnsEmptyList_WhenUsbServiceThrows()
    {
        // Arrange
        const string drive = "E:\\";

        _state.Setup(s => s.Get()).Returns(new UpdateMediumState(drive));

        _updateService.Setup(u => u.GetAvailableUpdates(It.IsAny<CancellationToken>()))
            .Throws(new InvalidOperationException("Enumeration failed"));

        var sut = new GetAvailableUpdatesQueryHandler(_factory.Object, _state.Object);

        // Act
        var result = await sut.Handle(new GetAvailableUpdatesQuery(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(result);
    }
}