using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Contracts.Notifications;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Infrastructure;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Sample.Application.Commands.SetTrayNotInInstrument;
using Pharos.Server.Sample.Application.Interfaces;
using Pharos.Server.Sample.Application.Services;
using Pharos.Server.Sample.Domain;

namespace Pharos.Server.Sample.Tests.Application.Commands;

[Collection(nameof(SampleTestsCollection))]
public class SetTrayNotInInstrumentCommandTests(SampleTestsWaf factory) : DatabaseTestBase<SampleTestsWaf>(factory)
{
    private readonly string _instrumentNumber = "SEED_01";
    private readonly string _trayId = "1234";
    private readonly string _tubeId = "T1";
    private readonly string _sampleId = "S1";

    private Mock<ISender> _senderMock = default!;
    private Mock<IInstrumentManager> _instrumentServiceMock = default!;
    private Mock<ISampleTrayHub> _sampleTrayHubMock = default!;
    private Mock<IInstrument> _instrumentMock = default!;
    private IDateTime _dateTime = default!;

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();

        _senderMock = new Mock<ISender>();
        _instrumentServiceMock = new Mock<IInstrumentManager>();
        _sampleTrayHubMock = new Mock<ISampleTrayHub>();
        _instrumentMock = new Mock<IInstrument>();
        _dateTime = ServiceProvider.GetRequiredService<IDateTime>();
    }

    [Fact]
    public async Task Handle_ShouldNotPersistOrNotify_WhenTrayExistsInOpenDrawer()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();
        var tray = new SampleTray
        {
            Id = _trayId,
            Drawer = DrawerType.Sample1,
            InstrumentNumber = _instrumentNumber,
            TrayPosition = 1
        };

        dbContext.SampleTrays.Add(tray);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sampleStorage = new InstrumentSampleStorage();
        sampleStorage.UpdateDrawer(_instrumentNumber, new SampleDrawer
        {
            DrawerType = DrawerType.Sample1,
            Trays =
           [
               new SampleTray
                {
                    Id = _trayId,
                    TrayPosition = 1
                }
           ]
        });

        _instrumentServiceMock
            .Setup(x => x.GetInstrument(_instrumentNumber))
            .Returns(_instrumentMock.Object);

        _instrumentMock
            .Setup(x => x.DrawersLockState[DrawerType.Sample1])
            .Returns(new DrawerLockState(DrawerType.Sample1, LockState.Open, false));

        var handler = CreateHandler(dbContext, sampleStorage);

        // Act
        await handler.Handle(new SetTrayNotInInstrumentCommand(_trayId), TestContext.Current.CancellationToken);

        // Assert
        var dbTray = await dbContext.SampleTrays.SingleAsync(x => x.Id == _trayId, TestContext.Current.CancellationToken);
        dbTray.InstrumentNumber.Should().Be(_instrumentNumber);
        dbTray.Drawer.Should().Be(DrawerType.Sample1);

        var storageTray = sampleStorage.GetTray(_trayId);
        storageTray.Should().NotBeNull();
        storageTray.Id.Should().NotBeEmpty();
        storageTray.Error.Should().Be(SampleTrayError.None);

        _sampleTrayHubMock.Verify(x => x.PositionStatusUpdated(It.IsAny<List<string>>()), Times.Never);
        _senderMock.Verify(x => x.Send(It.IsAny<SendTrayNotUsableAllInstrumentsCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5544")]
    public async Task Handle_ShouldPersistStorageTrayAndSetTrayNotInInstrument_WhenTrayExistsInClosedDrawer()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();
        var tray = new SampleTray
        {
            Id = _trayId,
            Drawer = DrawerType.Sample1,
            InstrumentNumber = _instrumentNumber,
            TrayPosition = 1
        };

        dbContext.SampleTrays.Add(tray);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sampleStorage = new InstrumentSampleStorage();
        sampleStorage.UpdateDrawer(_instrumentNumber, new SampleDrawer
        {
            DrawerType = DrawerType.Sample1,
            Trays =
           [
               new SampleTray
                {
                    Id = _trayId,
                    TrayPosition = 1
                }
           ]
        });

        var tube = new SampleTube
        {
            TubeId = _tubeId,
            SampleId = _sampleId,
            TrayId = _trayId,
            TrayPosition = 1,
            TubePosition = 1,
            InstrumentNumber = _instrumentNumber,
            Area = SampleArea.SampleDrawer1,
            Status = SampleTubeStatus.InProgress,
            RequestCreatedDate = _dateTime.Now
        };
        sampleStorage.AddOrUpdateTube(tube, _ => tube);

        _instrumentServiceMock
            .Setup(x => x.GetInstrument(_instrumentNumber))
            .Returns(_instrumentMock.Object);

        _instrumentMock
            .Setup(x => x.DrawersLockState[DrawerType.Sample1])
            .Returns(new DrawerLockState(DrawerType.Sample1, LockState.ClosedLocked, false));

        var handler = CreateHandler(dbContext, sampleStorage);

        // Act
        await handler.Handle(new SetTrayNotInInstrumentCommand(_trayId), TestContext.Current.CancellationToken);

        // Assert
        var dbTray = await dbContext.SampleTrays.Include(x => x.SampleTubes).SingleAsync(x => x.Id == _trayId, TestContext.Current.CancellationToken);
        dbTray.InstrumentNumber.Should().BeNull();
        dbTray.Drawer.Should().BeNull();
        dbTray.TrayPosition.Should().BeNull();
        dbTray.SampleTubes.Count.Should().NotBe(0);

        var storageTray = sampleStorage.GetTrays(_instrumentNumber, DrawerType.Sample1).Single(x => x.Error == SampleTrayError.UnknownObject);
        storageTray.Id.Should().BeEmpty();

        sampleStorage.GetTube(_tubeId).Should().BeNull();

        _sampleTrayHubMock.Verify(x => x.PositionStatusUpdated(It.Is<List<string>>(ids => ids.Single() == _trayId)), Times.Once);
        _senderMock.Verify(x => x.Send(It.Is<SendTrayNotUsableAllInstrumentsCommand>(command => command.TrayBarcode == _trayId), It.IsAny<CancellationToken>()), Times.Once);
    }

    private SetTrayNotInInstrumentCommandHandler CreateHandler(ISampleDbContext dbContext, IInstrumentSampleStorage sampleStorage)
    {
        return new SetTrayNotInInstrumentCommandHandler(
            dbContext,
            _senderMock.Object,
            sampleStorage,
            _dateTime,
            _instrumentServiceMock.Object,
            _sampleTrayHubMock.Object);
    }
}
