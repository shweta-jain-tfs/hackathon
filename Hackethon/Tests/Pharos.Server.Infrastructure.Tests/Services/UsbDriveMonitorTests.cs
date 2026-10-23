using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Pharos.Server.Infrastructure.Events;
using Pharos.Server.Infrastructure.Services;

namespace Pharos.Server.Infrastructure.Tests.Services;

public class UsbDriveMonitorTests
{
    private readonly Mock<IVolumeChangeEventSource> _eventSourceMock;
    private readonly Mock<IDriveInsertedHandler> _insertedMock;
    private readonly Mock<IDriveRemovedHandler> _removedMock;
    private readonly UsbDriveMonitor _sut;

    public UsbDriveMonitorTests()
    {
        _eventSourceMock = new Mock<IVolumeChangeEventSource>();

        _insertedMock = new Mock<IDriveInsertedHandler>();
        _removedMock = new Mock<IDriveRemovedHandler>();

        var services = new ServiceCollection()
            .AddSingleton(_insertedMock.Object)
            .AddSingleton(_removedMock.Object)
            .BuildServiceProvider();

        _sut = new UsbDriveMonitor(
            NullLogger<UsbDriveMonitor>.Instance,
            services.GetRequiredService<IServiceScopeFactory>(),
            _eventSourceMock.Object);
    }

    [Fact]
    public async Task GivenEventType2Raised_ThenCallsOnDriveInserted()
    {
        // Arrange
        const string driveName = "E:\\";
        const ushort eventType = 2;

        _insertedMock
            .Setup(h => h.OnDriveInsertedAsync(driveName, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.StartAsync(TestContext.Current.CancellationToken);
        await _sut.ExecuteTask!.WaitAsync(TestContext.Current.CancellationToken);

        _eventSourceMock.Raise(es => es.VolumeChanged += null!, eventType, driveName);

        // Assert
        _insertedMock.Verify(h => h.OnDriveInsertedAsync(driveName, It.IsAny<CancellationToken>()), Times.Once);
        _removedMock.Verify(h => h.OnDriveRemovedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenEventType3Raised_ThenCallsOnDriveRemoved()
    {
        // Arrange
        const string driveName = "F:\\";
        const ushort eventType = 3;

        _removedMock
            .Setup(h => h.OnDriveRemovedAsync(driveName, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.StartAsync(TestContext.Current.CancellationToken);
        await _sut.ExecuteTask!.WaitAsync(TestContext.Current.CancellationToken);
        _eventSourceMock.Raise(es => es.VolumeChanged += null!, eventType, driveName);

        // Assert
        _removedMock.Verify(h => h.OnDriveRemovedAsync(driveName, It.IsAny<CancellationToken>()), Times.Once);
        _insertedMock.Verify(h => h.OnDriveInsertedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenInvalidEventTypeRaised_ThenDoesNotCallAnyHandler()
    {
        // Arrange
        const string driveName = "G:\\";
        const ushort eventType = 99;

        // Act
        await _sut.StartAsync(TestContext.Current.CancellationToken);
        await _sut.ExecuteTask!.WaitAsync(TestContext.Current.CancellationToken);
        _eventSourceMock.Raise(es => es.VolumeChanged += null!, eventType, driveName);

        // Assert
        _insertedMock.Verify(h => h.OnDriveInsertedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _removedMock.Verify(h => h.OnDriveRemovedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
