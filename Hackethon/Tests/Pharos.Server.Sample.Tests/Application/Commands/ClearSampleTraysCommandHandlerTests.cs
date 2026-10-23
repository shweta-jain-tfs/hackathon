using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Contracts.Notifications;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Request;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Infrastructure;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Sample.Application.Commands.ClearSampleTrays;
using Pharos.Server.Sample.Application.Interfaces;
using Pharos.Server.Sample.Application.Services;
using Pharos.Server.Sample.Domain;
using DrawerType = Pharos.Server.Domain.Enums.DrawerType;
using SampleTray = Pharos.Server.Sample.Domain.SampleTray;
using SampleTubeStatus = Pharos.Server.Domain.Enums.SampleTubeStatus;

namespace Pharos.Server.Sample.Tests.Application.Commands;

[Collection(nameof(SampleTestsCollection))]
public class ClearSampleTraysCommandHandlerTests(SampleTestsWaf factory) : DatabaseTestBase<SampleTestsWaf>(factory)
{
    private readonly string _instrumentNumber = "SEED_01";
    private readonly string _tubeId = "T1";
    private readonly string _sampleId = "S1";
    private readonly string _leftDrawerTrayId = "1234";
    private InstrumentSampleStorage _sampleStorage = default!;
    private Mock<IInstrumentManager> _instrumentServiceMock = default!;
    private Mock<IInstrument> _instrumentMock = default!;
    private Mock<ISampleTrayHub> _sampleTrayHubMock = default!;
    private IDateTime _dateTime = default!;
    private Mock<ISender> _senderMock = default!;

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();

        _sampleStorage = new InstrumentSampleStorage();
        _instrumentServiceMock = new Mock<IInstrumentManager>();
        _instrumentMock = new Mock<IInstrument>();
        _instrumentServiceMock.Setup(x => x.GetInstrument(_instrumentNumber)).Returns(_instrumentMock.Object);
        _instrumentMock
            .Setup(x => x.DrawersLockState[It.IsAny<DrawerType>()])
            .Returns((DrawerType drawer) => new DrawerLockState(drawer, LockState.Open, false));
        _sampleTrayHubMock = new Mock<ISampleTrayHub>();
        _dateTime = ServiceProvider.GetRequiredService<IDateTime>();
        _senderMock = new Mock<ISender>();
    }

    public override ValueTask DisposeAsync()
    {
        _sampleStorage = null!;
        _instrumentServiceMock = null!;
        _instrumentMock = null!;
        _sampleTrayHubMock = null!;
        _dateTime = null!;
        _senderMock = null!;

        return base.DisposeAsync();
    }

    [Fact]
    public async Task Handle_ShouldClearSampleTrayAndUnmapTube_WhenTrayIsLoadedInInstrumentAndDrawerIsOpenAndRequestExists()
    {
        // Arrange
        var drawer = DrawerType.Sample1;
        var requestCreatedDate = _dateTime.Now;
        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();

        await AddSampleTrayToDatabase(dbContext, _leftDrawerTrayId, drawer, _tubeId, _sampleId, requestCreatedDate, SampleTubeStatus.PartlyCompleted);

        AddTrayToSampleStorage(_leftDrawerTrayId, 1, drawer);

        // Setup drawer lock state to open
        _instrumentMock
           .Setup(x => x.DrawersLockState[drawer])
           .Returns(new DrawerLockState(drawer, LockState.Open, false));

        // Setup Sender to return existing request
        _senderMock
            .Setup(x => x.Send(
                It.Is<GetRequestIdQuery>(q => q.SampleId == _sampleId && q.RequestCreatedDate == requestCreatedDate),
                It.IsAny<CancellationToken>()
            ))
            .ReturnsAsync(new RequestIdResponseDto(1, requestCreatedDate, true));

        var handler = new ClearSampleTraysCommandHandler(dbContext, _sampleTrayHubMock.Object, _sampleStorage, _instrumentServiceMock.Object, _senderMock.Object);
        var command = new ClearSampleTraysCommand([_leftDrawerTrayId]);

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeEmpty().And.Contain(_leftDrawerTrayId);
        _senderMock.Verify(x => x.Send(It.IsAny<GetRequestIdQuery>(), It.IsAny<CancellationToken>()), Times.Once);

        var sampleTray = await dbContext.SampleTrays
            .Where(x => x.Id == _leftDrawerTrayId)
            .Include(t => t.SampleTubes)
            .FirstAsync(TestContext.Current.CancellationToken);
        sampleTray.SampleTubes.Should().BeEmpty();

        var sampleTube = await dbContext.SampleTubes.FirstAsync(t => t.TubeId == _tubeId, TestContext.Current.CancellationToken);
        sampleTube.TrayId.Should().BeNull();
        sampleTube.TubePosition.Should().BeNull();
        sampleTube.InstrumentNumber.Should().BeNull();

        _sampleTrayHubMock.Verify(x => x.PositionStatusUpdated(It.Is<List<string>>(ids => ids.Contains(_leftDrawerTrayId))), Times.Once);
        _sampleTrayHubMock.Verify(x => x.FreePositionAndErrorCountUpdated(), Times.Once);

        _senderMock.Verify(
            x => x.Send(
                It.Is<RemoveTaskCardCommand>(cmd => cmd.CardType == TaskCardType.SamplePartlyCompleted && cmd.InstrumentNumber == _instrumentNumber),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldClearSampleTrayAndRemoveTube_WhenTrayIsLoadedInInstrumentAndDrawerIsOpenAndRequestDoesNotExist()
    {
        // Arrange
        var drawer = DrawerType.Sample1;
        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();

        await AddSampleTrayToDatabase(dbContext, _leftDrawerTrayId, drawer, _tubeId, _sampleId, null, SampleTubeStatus.InProgress);

        AddTrayToSampleStorage(_leftDrawerTrayId, 1, drawer);

        // Setup drawer lock state to open
        _instrumentMock
            .Setup(x => x.DrawersLockState[drawer])
            .Returns(new DrawerLockState(drawer, LockState.Open, false));

        // Setup Sender to return no request
        _senderMock
            .Setup(x => x.Send(It.IsAny<GetRequestIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RequestIdResponseDto(0, new DateTimeOffset(), false));

        var handler = new ClearSampleTraysCommandHandler(dbContext, _sampleTrayHubMock.Object, _sampleStorage, _instrumentServiceMock.Object, _senderMock.Object);
        var command = new ClearSampleTraysCommand([_leftDrawerTrayId]);

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeEmpty().And.Contain(_leftDrawerTrayId);
        _senderMock.Verify(x => x.Send(It.IsAny<GetRequestIdQuery>(), It.IsAny<CancellationToken>()), Times.Once);

        var sampleTray = await dbContext.SampleTrays
           .Where(x => x.Id == _leftDrawerTrayId)
           .Include(t => t.SampleTubes)
           .FirstAsync(TestContext.Current.CancellationToken);
        sampleTray.SampleTubes.Should().BeEmpty();

        var tubeExists = await dbContext.SampleTubes.AnyAsync(t => t.TubeId == _tubeId, TestContext.Current.CancellationToken);
        tubeExists.Should().BeFalse();

        _sampleTrayHubMock.Verify(x => x.PositionStatusUpdated(It.Is<List<string>>(ids => ids.Contains(_leftDrawerTrayId))), Times.Once);
        _sampleTrayHubMock.Verify(x => x.FreePositionAndErrorCountUpdated(), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldNotClearSampleTray_WhenTrayIsLoadedInInstrumentAndDrawerIsClosed()
    {
        // Arrange
        var drawer = DrawerType.Sample1;
        var requestCreatedDate = _dateTime.Now;
        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();

        await AddSampleTrayToDatabase(dbContext, _leftDrawerTrayId, drawer, _tubeId, _sampleId, requestCreatedDate, SampleTubeStatus.InProgress);

        AddTrayToSampleStorage(_leftDrawerTrayId, 1, drawer);

        // Setup drawer lock state to closed
        _instrumentMock
            .Setup(x => x.DrawersLockState[drawer])
            .Returns(new DrawerLockState(drawer, LockState.ClosedLocked, false));

        // Setup Sender to return existing request
        _senderMock
            .Setup(x => x.Send(
                It.Is<GetRequestIdQuery>(q => q.SampleId == _sampleId && q.RequestCreatedDate == requestCreatedDate),
                It.IsAny<CancellationToken>()
            ))
            .ReturnsAsync(new RequestIdResponseDto(1, requestCreatedDate, true));

        var handler = new ClearSampleTraysCommandHandler(dbContext, _sampleTrayHubMock.Object, _sampleStorage, _instrumentServiceMock.Object, _senderMock.Object);
        var command = new ClearSampleTraysCommand([_leftDrawerTrayId]);

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEmpty().And.NotContain(_leftDrawerTrayId);
        _senderMock.Verify(x => x.Send(It.IsAny<GetRequestIdQuery>(), It.IsAny<CancellationToken>()), Times.Never);

        var sampleTray = await dbContext.SampleTrays
            .Where(x => x.Id == _leftDrawerTrayId)
            .Include(t => t.SampleTubes)
            .FirstAsync(TestContext.Current.CancellationToken);
        sampleTray.SampleTubes.Should().NotBeEmpty();

        var sampleTube = await dbContext.SampleTubes.FirstAsync(t => t.TubeId == _tubeId, TestContext.Current.CancellationToken);
        sampleTube.TrayId.Should().Be(_leftDrawerTrayId);
        sampleTube.TubePosition.Should().Be(1);
        sampleTube.InstrumentNumber.Should().Be(_instrumentNumber);

        _sampleTrayHubMock.Verify(x => x.PositionStatusUpdated(It.Is<List<string>>(ids => ids.Contains(_leftDrawerTrayId))), Times.Never);
        _sampleTrayHubMock.Verify(x => x.FreePositionAndErrorCountUpdated(), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmpty_WhenNoTraysFoundInDatabase()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();
        var handler = new ClearSampleTraysCommandHandler(dbContext, _sampleTrayHubMock.Object, _sampleStorage, _instrumentServiceMock.Object, _senderMock.Object);
        var command = new ClearSampleTraysCommand(["9999"]);

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEmpty();

        _senderMock.Verify(x => x.Send(It.IsAny<GetRequestIdQuery>(), It.IsAny<CancellationToken>()), Times.Never);
        _sampleTrayHubMock.Verify(x => x.PositionStatusUpdated(It.IsAny<List<string>>()), Times.Never);
        _sampleTrayHubMock.Verify(x => x.FreePositionAndErrorCountUpdated(), Times.Never);
        _senderMock.Verify(x => x.Send(It.IsAny<RemoveTaskCardCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldClearSampleTrayAndUnmapTube_WhenTrayIsNotLoadedInInstrumentAndRequestExists()
    {
        // Arrange
        var requestCreatedDate = _dateTime.Now;
        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();

        // Add tray to database but NOT to sample storage
        dbContext.SampleTrays.Add(new SampleTray
        {
            Id = _leftDrawerTrayId,
            Drawer = null,
            InstrumentNumber = null,
            SampleTubes = new List<SampleTube>
            {
                new()
                {
                    TubeId = _tubeId,
                    SampleId = _sampleId,
                    Status = SampleTubeStatus.InProgress,
                    TubePosition = 1,
                    RequestCreatedDate = requestCreatedDate
                }
            }
        });

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Explicitly do NOT add the tray to sample storage
        // This ensures GetTray(trayId) will return null

        // Setup Sender to return existing request
        _senderMock
             .Setup(x => x.Send(
                 It.Is<GetRequestIdQuery>(q => q.SampleId == _sampleId && q.RequestCreatedDate == requestCreatedDate),
                 It.IsAny<CancellationToken>()
             ))
             .ReturnsAsync(new RequestIdResponseDto(1, requestCreatedDate, true));

        var handler = new ClearSampleTraysCommandHandler(dbContext, _sampleTrayHubMock.Object, _sampleStorage, _instrumentServiceMock.Object, _senderMock.Object);
        var command = new ClearSampleTraysCommand([_leftDrawerTrayId]);

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeEmpty().And.Contain(_leftDrawerTrayId);
        _senderMock.Verify(x => x.Send(It.IsAny<GetRequestIdQuery>(), It.IsAny<CancellationToken>()), Times.Once);

        // Verify the tray was cleared
        var clearedTray = await dbContext.SampleTrays
            .Where(x => x.Id == _leftDrawerTrayId)
            .Include(t => t.SampleTubes)
            .FirstAsync(TestContext.Current.CancellationToken);
        clearedTray.SampleTubes.Should().BeEmpty();

        // Verify the tube was detached from the tray
        var tube = await dbContext.SampleTubes.FirstOrDefaultAsync(t => t.TubeId == _tubeId, TestContext.Current.CancellationToken);
        tube.Should().NotBeNull();
        tube.TrayId.Should().BeNull();
        tube.TubePosition.Should().BeNull();
        tube.InstrumentNumber.Should().BeNull();

        _sampleTrayHubMock.Verify(x => x.PositionStatusUpdated(It.Is<List<string>>(ids => ids.Contains(_leftDrawerTrayId))), Times.Once);
        _sampleTrayHubMock.Verify(x => x.FreePositionAndErrorCountUpdated(), Times.Once);
    }

    [Theory]
    [InlineData(SampleTubeStatus.PartlyCompleted, TaskCardType.SamplePartlyCompleted)]
    public async Task Handle_ShouldSkipTraysInClosedDrawers_WhenClearingMultipleTrays(SampleTubeStatus status, TaskCardType taskCardType)
    {
        // Arrange
        var rightDrawerTrayId = "2222";
        var rightDrawerTubeId = "T2";
        var rightDrawerSampleId = "S2";
        var requestCreatedDate = DateTimeOffset.UtcNow;
        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();

        await AddSampleTrayToDatabase(dbContext, _leftDrawerTrayId, DrawerType.Sample1, _tubeId, _sampleId, requestCreatedDate, SampleTubeStatus.Completed);
        await AddSampleTrayToDatabase(dbContext, rightDrawerTrayId, DrawerType.Sample2, rightDrawerTubeId, rightDrawerSampleId, requestCreatedDate, status);

        AddTrayToSampleStorage(_leftDrawerTrayId, 1, DrawerType.Sample1);
        AddTrayToSampleStorage(rightDrawerTrayId, 1, DrawerType.Sample2);

        // Setup drawer lock states - left drawer open(clear the tray), right drawer closed(skip clearing the tray)
        _instrumentMock
           .Setup(x => x.DrawersLockState[DrawerType.Sample1])
           .Returns(new DrawerLockState(DrawerType.Sample1, LockState.Open, false));
        _instrumentMock
           .Setup(x => x.DrawersLockState[DrawerType.Sample2])
           .Returns(new DrawerLockState(DrawerType.Sample2, LockState.ClosedLocked, false));

        // Setup Sender to return existing request
        _senderMock
            .Setup(x => x.Send(It.IsAny<GetRequestIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RequestIdResponseDto(1, requestCreatedDate, true));

        var handler = new ClearSampleTraysCommandHandler(dbContext, _sampleTrayHubMock.Object, _sampleStorage, _instrumentServiceMock.Object, _senderMock.Object);
        var command = new ClearSampleTraysCommand([_leftDrawerTrayId, rightDrawerTrayId]);

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        // Verify left drawer tray was cleared
        result.Should().NotBeEmpty().And.Contain(_leftDrawerTrayId);

        var clearedTray = await dbContext.SampleTrays
            .Where(x => x.Id == _leftDrawerTrayId)
            .Include(t => t.SampleTubes)
            .FirstAsync(TestContext.Current.CancellationToken);
        clearedTray.SampleTubes.Should().BeEmpty();

        var sampleTube = await dbContext.SampleTubes.FirstAsync(t => t.TubeId == _tubeId, TestContext.Current.CancellationToken);
        sampleTube.TrayId.Should().BeNull();
        sampleTube.TubePosition.Should().BeNull();
        sampleTube.InstrumentNumber.Should().BeNull();

        // Verify right drawer tray was skipped
        result.Should().NotContain(rightDrawerTrayId);

        var skippedTray = await dbContext.SampleTrays
            .Where(x => x.Id == rightDrawerTrayId)
            .Include(t => t.SampleTubes)
            .FirstAsync(TestContext.Current.CancellationToken);
        skippedTray.SampleTubes.Should().NotBeEmpty();

        var skippedTube = await dbContext.SampleTubes.FirstAsync(t => t.TubeId == rightDrawerTubeId, TestContext.Current.CancellationToken);
        skippedTube.TrayId.Should().Be(rightDrawerTrayId);
        skippedTube.TubePosition.Should().Be(1);
        skippedTube.InstrumentNumber.Should().Be(_instrumentNumber);

        _sampleTrayHubMock.Verify(x => x.PositionStatusUpdated(It.Is<List<string>>(ids => ids.Contains(_leftDrawerTrayId))), Times.Once);
        _sampleTrayHubMock.Verify(x => x.FreePositionAndErrorCountUpdated(), Times.Once);

        _senderMock.Verify(
            x => x.Send(
                It.Is<RemoveTaskCardCommand>(cmd => cmd.CardType == TaskCardType.SampleTubeError && cmd.InstrumentNumber == _instrumentNumber),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _senderMock.Verify(
            x => x.Send(
                It.Is<RemoveTaskCardCommand>(cmd => cmd.CardType == taskCardType && cmd.InstrumentNumber == _instrumentNumber),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldClearSampleTrayAndRemoveSampleTubeErrorTaskCard_WhenTrayContainsTubeWithErrorAndDrawerIsOpen()
    {
        // Arrange
        var drawer = DrawerType.Sample1;
        var requestCreatedDate = _dateTime.Now;
        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();

        // Add tray with tube that has an error
        dbContext.SampleTrays.Add(new SampleTray
        {
            Id = _leftDrawerTrayId,
            Drawer = drawer,
            InstrumentNumber = _instrumentNumber,
            SampleTubes = new List<SampleTube>
            {
                new()
                {
                    TubeId = _tubeId,
                    SampleId = _sampleId,
                    Status = SampleTubeStatus.InProgress,
                    InstrumentNumber = _instrumentNumber,
                    TubePosition = 1,
                    RequestCreatedDate = requestCreatedDate,
                    InternalError = SampleTubeError.MissingTestOrders // Tube with error
                }
            }
        });

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        AddTrayToSampleStorage(_leftDrawerTrayId, 1, drawer);

        // Setup drawer lock state to open
        _instrumentMock
           .Setup(x => x.DrawersLockState[drawer])
           .Returns(new DrawerLockState(drawer, LockState.Open, false));

        // Setup that no other tubes with errors exist after clearing this tray
        _senderMock
            .Setup(x => x.Send(It.IsAny<GetRequestIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RequestIdResponseDto(1, requestCreatedDate, true));

        var handler = new ClearSampleTraysCommandHandler(dbContext, _sampleTrayHubMock.Object, _sampleStorage, _instrumentServiceMock.Object, _senderMock.Object);
        var command = new ClearSampleTraysCommand([_leftDrawerTrayId]);

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeEmpty().And.Contain(_leftDrawerTrayId);

        var sampleTray = await dbContext.SampleTrays
            .Where(x => x.Id == _leftDrawerTrayId)
            .Include(t => t.SampleTubes)
            .FirstAsync(TestContext.Current.CancellationToken);
        sampleTray.SampleTubes.Should().BeEmpty();

        var sampleTube = await dbContext.SampleTubes.FirstAsync(t => t.TubeId == _tubeId, TestContext.Current.CancellationToken);
        sampleTube.TrayId.Should().BeNull();
        sampleTube.TubePosition.Should().BeNull();
        sampleTube.InstrumentNumber.Should().BeNull();

        _sampleTrayHubMock.Verify(x => x.PositionStatusUpdated(It.Is<List<string>>(ids => ids.Contains(_leftDrawerTrayId))), Times.Once);
        _sampleTrayHubMock.Verify(x => x.FreePositionAndErrorCountUpdated(), Times.Once);

        // Verify that SampleTubeError task card is removed when no error tubes remain
        _senderMock.Verify(
            x => x.Send(
                It.Is<RemoveTaskCardCommand>(cmd => cmd.CardType == TaskCardType.SampleTubeError && cmd.InstrumentNumber == _instrumentNumber),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private async Task AddSampleTrayToDatabase(ISampleDbContext dbContext, string trayId, DrawerType drawer, string tubeId, string sampleId, DateTimeOffset? requestCreatedDate, SampleTubeStatus sampleTubeStatus)
    {
        dbContext.SampleTrays.Add(new SampleTray
        {
            Id = trayId,
            Drawer = drawer,
            InstrumentNumber = _instrumentNumber,
            SampleTubes = new List<SampleTube>
            {
                new()
                {
                    TubeId = tubeId,
                    SampleId = sampleId,
                    Status = sampleTubeStatus,
                    InstrumentNumber = _instrumentNumber,
                    TubePosition = 1,
                    RequestCreatedDate = requestCreatedDate
                }
            }
        });

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private void AddTrayToSampleStorage(string trayId, int trayPosition, DrawerType drawerType)
    {
        _sampleStorage.UpdateDrawer(_instrumentNumber, new SampleDrawer
        {
            DrawerType = drawerType,
            Trays = new List<SampleTray>
            {
                new()
                {
                    Id = trayId,
                    Drawer = drawerType,
                    TrayPosition = trayPosition,
                }
            }
        });
    }
}
