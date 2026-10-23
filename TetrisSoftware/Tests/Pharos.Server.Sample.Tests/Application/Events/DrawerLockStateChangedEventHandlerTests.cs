using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Pharos.Contracts.Notifications;
using Pharos.Server.Application.AuditTrail;
using Pharos.Server.Application.AuditTrail.Enums;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Request;
using Pharos.Server.Application.Settings.Sample;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Infrastructure;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Sample.Application.Events;
using Pharos.Server.Sample.Application.Interfaces;
using Pharos.Server.Sample.Application.Services;
using Pharos.Server.Sample.Domain;

namespace Pharos.Server.Sample.Tests.Application.Events;

[Collection(nameof(SampleTestsCollection))]
public class DrawerLockStateChangedEventHandlerTests(SampleTestsWaf factory) : DatabaseTestBase<SampleTestsWaf>(factory)
{
    private readonly string _instrumentNumber = "SEED_001";
    private readonly Mock<InstrumentSampleStorage> _sampleStorageMock = new();
    private readonly Mock<ISampleQcIdProvider> _sampleIdProviderMock = new();
    private readonly Mock<IOptionsSnapshot<SpecialSampleHandlingSettings>> _specialSampleHandlingSettingsMock = new();
    private readonly Mock<ISender> _senderMock = new();
    private readonly Mock<ISampleTrayHub> _sampleTrayHubMock = new();
    private readonly Mock<IDateTime> _dateTimeMock = new();

    private DrawerLockStateChangedEventHandler CreateHandler(ISampleDbContext dbContext)
    {
        return new(
            _sampleStorageMock.Object,
            dbContext,
            _sampleIdProviderMock.Object,
            _specialSampleHandlingSettingsMock.Object,
            _sampleTrayHubMock.Object,
            _dateTimeMock.Object,
            _senderMock.Object
        );
    }

    [Fact]
    public async Task Handle_SampleDrawer1Opened_ShouldCreateAuditTrailEventWithCorrectEventIDDrawerTypeAndInstrumentNumber()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();
        var settings = new SpecialSampleHandlingSettings();
        _specialSampleHandlingSettingsMock.Setup(x => x.Value).Returns(settings);
        var handler = CreateHandler(dbContext);

        var expectedDrawerType = DrawerType.Sample1;
        var notification = CreateDrawerLockStateChanged(
            _instrumentNumber,
            previousState: LockState.ClosedLocked,
            currentState: LockState.Open,
            expectedDrawerType);

        await handler.Handle(notification, TestContext.Current.CancellationToken);

        _senderMock.Verify(x => x.Send(
            It.Is<CreateAuditTrailRecordCommand>(cmd =>
                cmd.EventId == EventId.SampleDrawerOpened &&
                cmd.EventData.Any(d =>
                    d.Data.ContainsKey(EventDataFields.DrawerType.ToString()) &&
                    d.Data[EventDataFields.DrawerType.ToString()].ToString() == expectedDrawerType.ToString())),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SampleDrawer2Closed_ShouldCreateAuditTrailEventWithCorrectDrawerType()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();
        var settings = new SpecialSampleHandlingSettings();
        _specialSampleHandlingSettingsMock.Setup(x => x.Value).Returns(settings);
        var handler = CreateHandler(dbContext);

        var expectedDrawerType = DrawerType.Sample2;
        var notification = CreateDrawerLockStateChanged(
            _instrumentNumber,
            previousState: LockState.Open,
            currentState: LockState.ClosedLocked,
            expectedDrawerType);

        await handler.Handle(notification, TestContext.Current.CancellationToken);

        _senderMock.Verify(x => x.Send(
            It.Is<CreateAuditTrailRecordCommand>(cmd =>
            cmd.EventId == EventId.SampleDrawerClosed &&
                cmd.EventData.Any(d =>
                    d.Data.ContainsKey(EventDataFields.DrawerType.ToString()) &&
                    d.Data[EventDataFields.DrawerType.ToString()].ToString() == expectedDrawerType.ToString())),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldUnmapPersistedTube_WhenRequestExistsButTubeMissingFromStorage()
    {
        // Arrange
        _sampleStorageMock.Object.Clear(_instrumentNumber);
        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();
        var trayId = "T001";
        const int trayPosition = 1;
        var requestCreatedDate = new DateTimeOffset(new DateTime(2025, 1, 1));

        var existingTray = new SampleTray
        {
            Id = trayId,
            Drawer = DrawerType.Sample1,
            InstrumentNumber = _instrumentNumber,
            TrayPosition = trayPosition,
            SampleTubes = new List<SampleTube>
            {
                new()
                {
                    TubeId = "Tube-Unmap",
                    SampleId = "Sample-Unmap",
                    TrayId = trayId,
                    TubePosition = 2,
                    InstrumentNumber = _instrumentNumber,
                    RequestCreatedDate = requestCreatedDate,
                    Status = SampleTubeStatus.InProgress,
                    IdentitySource = SampleTubeIdentitySource.ManualInput
                }
            }
        };

        dbContext.SampleTrays.Add(existingTray);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        _specialSampleHandlingSettingsMock.Setup(x => x.Value).Returns(new SpecialSampleHandlingSettings());

        _sampleStorageMock.Object.UpdateDrawer(_instrumentNumber, new SampleDrawer
        {
            DrawerType = DrawerType.Sample1,
            Trays = new List<SampleTray>
            {
                new()
                {
                    Id = trayId,
                    Drawer = DrawerType.Sample1,
                    TrayPosition = trayPosition,
                    Error = SampleTrayError.None
                }
            }
        });

        _senderMock.Setup(x => x.Send(It.IsAny<GetRequestIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RequestIdResponseDto(1, requestCreatedDate, true));

        var handler = CreateHandler(dbContext);
        var notification = CreateDrawerLockStateChanged(
            _instrumentNumber,
            LockState.ClosedLocked,
            LockState.Open,
            DrawerType.Sample1);

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        var updatedTray = await dbContext.SampleTrays
            .Include(x => x.SampleTubes)
            .FirstOrDefaultAsync(x => x.Id == trayId, TestContext.Current.CancellationToken);

        updatedTray.Should().NotBeNull();
        var tube = updatedTray!.SampleTubes.FirstOrDefault();
        tube.Should().BeNull(); // Tube should be removed from tray's collection

        var allTubes = await dbContext.SampleTubes.ToListAsync(TestContext.Current.CancellationToken);
        var unmappedTube = allTubes.FirstOrDefault(t => t.TubeId == "Tube-Unmap");
        unmappedTube.Should().NotBeNull();
        unmappedTube!.TrayId.Should().BeNull();
        unmappedTube.TubePosition.Should().BeNull();
        unmappedTube.InstrumentNumber.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldDeletePersistedTube_WhenRequestDoesNotExistAndTubeMissingFromStorage()
    {
        // Arrange
        _sampleStorageMock.Object.Clear(_instrumentNumber);
        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();
        var trayId = "T002";
        const int trayPosition = 2;

        var existingTray = new SampleTray
        {
            Id = trayId,
            Drawer = DrawerType.Sample1,
            InstrumentNumber = _instrumentNumber,
            TrayPosition = trayPosition,
            SampleTubes = new List<SampleTube>
            {
                new()
                {
                    TubeId = "Tube-Delete",
                    SampleId = "Sample-Delete",
                    TrayId = trayId,
                    TubePosition = 3,
                    InstrumentNumber = _instrumentNumber,
                    RequestCreatedDate = null, // No request created - unidentified tube
                    Status = SampleTubeStatus.InProgress,
                    IdentitySource = SampleTubeIdentitySource.BarcodeScanning
                }
            }
        };

        dbContext.SampleTrays.Add(existingTray);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        _specialSampleHandlingSettingsMock.Setup(x => x.Value).Returns(new SpecialSampleHandlingSettings());

        _sampleStorageMock.Object.UpdateDrawer(_instrumentNumber, new SampleDrawer
        {
            DrawerType = DrawerType.Sample1,
            Trays = new List<SampleTray>
            {
                new()
                {
                    Id = trayId,
                    Drawer = DrawerType.Sample1,
                    TrayPosition = trayPosition,
                    Error = SampleTrayError.None
                }
            }
        });

        var handler = CreateHandler(dbContext);
        var notification = CreateDrawerLockStateChanged(
            _instrumentNumber,
            LockState.ClosedLocked,
            LockState.Open,
            DrawerType.Sample1);

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        var updatedTray = await dbContext.SampleTrays
            .Include(x => x.SampleTubes)
            .FirstOrDefaultAsync(x => x.Id == trayId, TestContext.Current.CancellationToken);

        updatedTray.Should().NotBeNull();
        updatedTray!.SampleTubes.Should().BeEmpty(); // Tube should be removed from tray's collection

        var allTubes = await dbContext.SampleTubes.ToListAsync(TestContext.Current.CancellationToken);
        allTubes.Should().NotContain(t => t.TubeId == "Tube-Delete"); // Tube should be deleted from database
    }

    private static DrawerLockStateChanged CreateDrawerLockStateChanged(
        string instrumentNumber,
        LockState previousState,
        LockState currentState,
        DrawerType drawerType)
    {
        var previousStates = CreateDrawerStates(drawerType, previousState);
        var currentStates = CreateDrawerStates(drawerType, currentState);

        return new DrawerLockStateChanged(instrumentNumber, previousStates, currentStates);
    }

    private static List<DrawerLockState> CreateDrawerStates(DrawerType targetDrawerType, LockState targetState)
    {
        var states = new List<DrawerLockState>();

        foreach (var drawerType in Enum.GetValues<DrawerType>())
        {
            var state = drawerType == targetDrawerType ? targetState : LockState.ClosedLocked;
            states.Add(new DrawerLockState(drawerType, state, false, null));
        }

        return states;
    }
}

