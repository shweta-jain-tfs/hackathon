using Microsoft.Extensions.DependencyInjection;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Request;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Localization;
using Pharos.Server.Sample.Application.Interfaces;
using Pharos.Server.Sample.Application.Queries.GetSampleTrayDetails;
using Pharos.Server.Sample.Application.Services;
using Pharos.Server.Sample.Domain;

namespace Pharos.Server.Sample.Tests.Application.Queries;

[Collection(nameof(SampleTestsCollection))]
public class GetSampleTrayDetailsQueryHandlerTests(SampleTestsWaf factory) : DatabaseTestBase<SampleTestsWaf>(factory)
{
    private const int _maxSampleTubesInTray = 50;
    private readonly string _instrumentNumber = "S001";
    private readonly string _trayId = "TR01";

    private InstrumentMockHelper? _instrumentMockHelper;

    private Mock<IInstrumentManager> CreateInstrumentMockManager()
    {
        var instrumentServiceMock = new Mock<IInstrumentManager>();
        _instrumentMockHelper = new InstrumentMockHelper(instrumentServiceMock, _instrumentNumber);
        return instrumentServiceMock;
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenTrayDoesNotExist()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();
        var sampleIdFormatter = ServiceProvider.GetRequiredService<ISampleIdFormatter>();
        var sampleStorage = new InstrumentSampleStorage();
        var instrumentServiceMock = CreateInstrumentMockManager();
        var handler = new GetSampleTrayDetailsQueryHandler(dbContext, sampleStorage, instrumentServiceMock.Object, sampleIdFormatter);

        var query = new GetSampleTrayDetailsQuery("9999"); // Non-existent tray ID

        // Act
        Func<Task> act = async () => await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldReturnTrayDetailsFromDatabase_WhenTrayIsLoadedInInstrumentAndDrawerIsOpen()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();
        var sampleIdFormatter = ServiceProvider.GetRequiredService<ISampleIdFormatter>();
        var sampleStorage = new InstrumentSampleStorage();
        var instrumentServiceMock = CreateInstrumentMockManager();
        var handler = new GetSampleTrayDetailsQueryHandler(dbContext, sampleStorage, instrumentServiceMock.Object, sampleIdFormatter);

        // Add a tray to the database
        var tray = new SampleTray
        {
            Id = _trayId,
            TrayPosition = 1,
            Drawer = DrawerType.Sample1,
            InstrumentNumber = _instrumentNumber,
            SampleTubes = new List<SampleTube>
            {
                new()
                {
                    TubeId = "T01",
                    TubePosition = 1,
                    SampleId = "Sample1",
                    Status = SampleTubeStatus.InProgress
                },
                new()
                {
                    TubeId = "T02",
                    TubePosition = 2,
                    SampleId = "Sample2",
                    Status = SampleTubeStatus.Completed
                }
            }
        };
        dbContext.SampleTrays.Add(tray);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Add the tray to the sample storage
        sampleStorage.UpdateDrawer(_instrumentNumber, new SampleDrawer
        {
            DrawerType = DrawerType.Sample1,
            Trays =
            [
                new()
                {
                    Id = _trayId,
                    TrayPosition = 1
                }
            ]
        });

        // Add tubes to the sample storage
        sampleStorage.AddOrUpdateTube(new SampleTube
        {
            TubeId = "T01",
            TrayPosition = 1,
            TubePosition = 1,
            SampleId = "Sample1",
            Status = SampleTubeStatus.Completed, // Different status to ensure database version is used
            Area = SampleArea.SampleDrawer1,
            InstrumentNumber = _instrumentNumber
        }, _ => new SampleTube());

        sampleStorage.AddOrUpdateTube(new SampleTube
        {
            TubeId = "T02",
            TrayPosition = 1,
            TubePosition = 2,
            SampleId = "Sample2",
            Status = SampleTubeStatus.InProgress, // Different status to ensure database version is used
            Area = SampleArea.SampleDrawer1,
            InstrumentNumber = _instrumentNumber
        }, _ => new SampleTube());

        // Simulate open drawer
        var instrumentMock = _instrumentMockHelper!.CreateInstrumentMock(instrumentServiceMock);
        instrumentMock
            .Setup(x => x.DrawersLockState[DrawerType.Sample1])
            .Returns(new DrawerLockState(DrawerType.Sample1, LockState.Open, false));

        var query = new GetSampleTrayDetailsQuery(tray.Id);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(_trayId);
        result.TrayPosition.Should().Be(1);
        result.IsTrayEditable.Should().BeTrue(); // Drawer is open
        result.SamplePositions.Should().HaveCount(_maxSampleTubesInTray);
        result.SamplePositions[0].SampleId.Should().Be("Sample1");
        result.SamplePositions[0].SampleTubeStatus.Should().Be(SampleTubeStatus.InProgress);
        result.SamplePositions[1].SampleId.Should().Be("Sample2");
        result.SamplePositions[1].SampleTubeStatus.Should().Be(SampleTubeStatus.Completed);
    }

    [Fact]
    public async Task Handle_ShouldReturnTrayDetailsFromStorage_WhenTrayIsLoadedInInstrumentAndDrawerIsClosed()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();
        var sampleIdFormatter = ServiceProvider.GetRequiredService<ISampleIdFormatter>();
        var sampleStorage = new InstrumentSampleStorage();
        var instrumentServiceMock = CreateInstrumentMockManager();
        var handler = new GetSampleTrayDetailsQueryHandler(dbContext, sampleStorage, instrumentServiceMock.Object, sampleIdFormatter);

        // Add a tray to the database
        var tray = new SampleTray
        {
            Id = _trayId,
            TrayPosition = 1,
            Drawer = DrawerType.Sample1,
            InstrumentNumber = _instrumentNumber
        };
        dbContext.SampleTrays.Add(tray);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Add the tray to the sample storage
        sampleStorage.UpdateDrawer(_instrumentNumber, new SampleDrawer
        {
            DrawerType = DrawerType.Sample1,
            Trays =
            [
                new()
                {
                    Id = _trayId,
                    TrayPosition = 1
                }
            ]
        });
        // Add tubes to the sample storage
        sampleStorage.AddOrUpdateTube(new SampleTube
        {
            TubeId = "T01",
            TrayPosition = 1,
            TubePosition = 1,
            SampleId = "Sample1",
            Status = SampleTubeStatus.InProgress,
            Area = SampleArea.SampleDrawer1,
            InstrumentNumber = _instrumentNumber
        }, _ => new SampleTube());

        sampleStorage.AddOrUpdateTube(new SampleTube
        {
            TubeId = "T02",
            TrayPosition = 1,
            TubePosition = 2,
            SampleId = "Sample2",
            Status = SampleTubeStatus.Completed,
            Area = SampleArea.SampleDrawer1,
            InstrumentNumber = _instrumentNumber
        }, _ => new SampleTube());

        // Simulate closed drawer
        var instrumentMock = _instrumentMockHelper!.CreateInstrumentMock(instrumentServiceMock);
        instrumentMock
            .Setup(x => x.DrawersLockState[DrawerType.Sample1])
            .Returns(new DrawerLockState(DrawerType.Sample1, LockState.ClosedLocked, false));

        var query = new GetSampleTrayDetailsQuery(tray.Id);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(_trayId);
        result.TrayPosition.Should().Be(1);
        result.IsTrayEditable.Should().BeFalse(); // Drawer is closed
        result.SamplePositions.Should().HaveCount(_maxSampleTubesInTray);
        result.SamplePositions[0].SampleId.Should().Be("Sample1");
        result.SamplePositions[0].SampleTubeStatus.Should().Be(SampleTubeStatus.InProgress);
        result.SamplePositions[1].SampleId.Should().Be("Sample2");
        result.SamplePositions[1].SampleTubeStatus.Should().Be(SampleTubeStatus.Completed);
    }

    [Fact]
    public async Task Handle_ShouldReturnTrayDetailsFromDatabase_WhenTrayIsLoadedInInstrumentAndDrawerIsClosedButScanningOngoing()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();
        var sampleIdFormatter = ServiceProvider.GetRequiredService<ISampleIdFormatter>();
        var sampleStorage = new InstrumentSampleStorage();
        var instrumentServiceMock = CreateInstrumentMockManager();
        var handler = new GetSampleTrayDetailsQueryHandler(dbContext, sampleStorage, instrumentServiceMock.Object, sampleIdFormatter);

        // Add a tray to the database
        var tray = new SampleTray
        {
            Id = _trayId,
            TrayPosition = 1,
            Drawer = DrawerType.Sample2,
            InstrumentNumber = _instrumentNumber,
            SampleTubes = new List<SampleTube>
            {
                new()
                {
                    TubeId = "T01",
                    TubePosition = 2,
                    SampleId = "Sample1",
                    Status = SampleTubeStatus.InProgress
                }
            }
        };
        dbContext.SampleTrays.Add(tray);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Add tray to storage
        sampleStorage.UpdateDrawer(_instrumentNumber, new SampleDrawer
        {
            DrawerType = DrawerType.Sample2,
            ScanningOngoing = true,
            Trays = [new() { Id = _trayId, TrayPosition = 1 }]
        });

        // Setup drawer as closed
        var instrumentMock = _instrumentMockHelper!.CreateInstrumentMock(instrumentServiceMock);
        instrumentMock
            .Setup(x => x.DrawersLockState[DrawerType.Sample2])
            .Returns(new DrawerLockState(DrawerType.Sample2, LockState.ClosedLocked, false));

        var query = new GetSampleTrayDetailsQuery(_trayId);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(_trayId);
        result.IsTrayEditable.Should().BeTrue(); // Editable when drawer is scanning
        result.SamplePositions.Should().HaveCount(_maxSampleTubesInTray);
        result.SamplePositions[1].SampleId.Should().Be("Sample1");
        result.SamplePositions[1].SampleTubeStatus.Should().Be(SampleTubeStatus.InProgress);
    }

    [Fact]
    public async Task Handle_ShouldReturnTrayDetailsFromDatabase_WhenTrayIsNotLoadedInInstrument()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();
        var sampleIdFormatter = ServiceProvider.GetRequiredService<ISampleIdFormatter>();
        var instrumentServiceMock = new Mock<IInstrumentManager>();
        var sampleStorage = new InstrumentSampleStorage();
        var handler = new GetSampleTrayDetailsQueryHandler(dbContext, sampleStorage, instrumentServiceMock.Object, sampleIdFormatter);

        // Add a tray to the database without loading it to storage (instrumentNumber is null)
        var tray = new SampleTray
        {
            Id = _trayId,
            TrayPosition = null,
            Drawer = null,
            InstrumentNumber = null, // This will trigger the "Not Loaded" location
            SampleTubes = new List<SampleTube>
            {
                new()
                {
                    TubeId = "T01",
                    TubePosition = 5,
                    SampleId = "Sample1",
                    TubeBarcode = "BC001",
                    Status = SampleTubeStatus.NotIdentified,
                    HighPriority = true,
                    IdentitySource = SampleTubeIdentitySource.BarcodeScanning,
                    InstrumentError = SampleTubeError.UnreadableBarcode
                }
            }
        };
        dbContext.SampleTrays.Add(tray);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var query = new GetSampleTrayDetailsQuery(_trayId);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(_trayId);
        result.TrayLocation.Should().Be(Translations.TrayLocation_NotLoaded);
        result.IsTrayEditable.Should().BeTrue();
        result.SamplePositions.Should().HaveCount(_maxSampleTubesInTray);
        result.SamplePositions[4].SampleId.Should().Be("Sample1");
        result.SamplePositions[4].SampleTubeStatus.Should().Be(SampleTubeStatus.NotIdentified);
    }

    private class InstrumentMockHelper
    {
        private readonly Mock<IInstrumentManager> _instrumentServiceMock;
        private readonly string _instrumentNumber;

        public InstrumentMockHelper(Mock<IInstrumentManager> instrumentServiceMock, string instrumentNumber)
        {
            _instrumentServiceMock = instrumentServiceMock;
            _instrumentNumber = instrumentNumber;
        }

        public Mock<IInstrument> CreateInstrumentMock(Mock<IInstrumentManager>? manager = null)
        {
            manager ??= _instrumentServiceMock;
            var instrumentMock = new Mock<IInstrument>();
            manager.Setup(x => x.GetInstrument(_instrumentNumber)).Returns(instrumentMock.Object);
            return instrumentMock;
        }
    }
}
