using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Contracts.Notifications;
using Pharos.Server.Application.Lis;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Sample.Application.Events;
using Pharos.Server.Sample.Application.Interfaces;
using Pharos.Server.Sample.Domain;

namespace Pharos.Server.Sample.Tests.Application.Events;

[Collection(nameof(SampleTestsCollection))]
public class MissingTestOrdersEventHandlerTests(SampleTestsWaf factory) : DatabaseTestBase<SampleTestsWaf>(factory)
{
    private readonly string _instrumentNumber = "SEED_01";
    private readonly string _instrumentNumber2 = "SEED_02";
    private readonly string _sampleId = "S001";
    private readonly string _sampleId2 = "S002";
    private readonly string _tray1Id = "1111";
    private readonly string _tray2Id = "2222";
    private readonly string _tray3Id = "3333";

    [Fact(Skip = "Randomly fails. Works on its own but sometime fails when run together with other tests")]
    public async Task Handle_ShouldSetInternalErrorInStorageAndDb_ForMultipleTubesInDrawers_AndDrawerOpened()
    {
        // Arrange
        var sampleStorage = ServiceProvider.GetRequiredService<IInstrumentSampleStorage>();
        sampleStorage.UpdateDrawer(_instrumentNumber, new SampleDrawer
        {
            DrawerType = DrawerType.Sample1,
            Trays = new List<SampleTray>
            {
                new() { Id = _tray1Id, TrayPosition = 1 },
                new() { Id = _tray2Id, TrayPosition = 2 }
            }
        });
        sampleStorage.AddOrUpdateTube(new SampleTube
        {
            TubeId = _sampleId + "_T1",
            SampleId = _sampleId,
            Area = SampleArea.SampleDrawer2,
            InstrumentNumber = _instrumentNumber,
            TrayPosition = 1,
            TrayId = _tray1Id,
            TubePosition = 10,
            Status = SampleTubeStatus.InProgress
        }, _ => new SampleTube());
        sampleStorage.AddOrUpdateTube(new SampleTube
        {
            TubeId = _sampleId + "_T2",
            SampleId = _sampleId,
            Area = SampleArea.SampleDrawer2,
            InstrumentNumber = _instrumentNumber,
            TrayPosition = 2,
            TrayId = _tray2Id,
            TubePosition = 11,
            Status = SampleTubeStatus.InProgress
        }, _ => new SampleTube());

        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();
        dbContext.SampleTrays.Add(new SampleTray
        {
            Id = _tray1Id,
            Drawer = DrawerType.Sample2,
            InstrumentNumber = _instrumentNumber,
            SampleTubes = new List<SampleTube>
            {
                new()
                {
                    TubeId = _sampleId + "_T1",
                    SampleId = _sampleId,
                    TubePosition = 10,
                    Status = SampleTubeStatus.InProgress
                }
            }
        });
        dbContext.SampleTrays.Add(new SampleTray
        {
            Id = _tray2Id,
            Drawer = DrawerType.Sample2,
            InstrumentNumber = _instrumentNumber,
            SampleTubes = new List<SampleTube>
            {
                new()
                {
                    TubeId = _sampleId + "_T2",
                    SampleId = _sampleId,
                    TubePosition = 11,
                    Status = SampleTubeStatus.InProgress
                }
            }
        });
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var senderMock = new Mock<ISender>();
        senderMock.Setup(x => x.Send(It.IsAny<TaskCardCommand>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var sender = senderMock.Object;
        var sampleTrayHub = ServiceProvider.GetRequiredService<ISampleTrayHub>();

        var handler = new MissingTestOrdersEventHandler(
               sampleStorage,
               sender,
               dbContext,
               sampleTrayHub
           );

        var missingTestOrdersEvent = new MissingTestOrdersEvent(_sampleId);

        // Act
        await handler.Handle(missingTestOrdersEvent, TestContext.Current.CancellationToken);

        // Assert storage
        var tubes = sampleStorage.GetTubesBySampleId(_sampleId);
        tubes.Should().NotBeNull();
        tubes.Count.Should().Be(2);
        foreach (var tube in tubes)
        {
            tube.InternalError.Should().Be(SampleTubeError.MissingTestOrders);
        }

        // Assert DB
        var dbTubes = await dbContext.SampleTubes.Where(x => x.SampleId == _sampleId).ToListAsync(TestContext.Current.CancellationToken);
        dbTubes.Count.Should().Be(2);
        foreach (var tube in dbTubes)
        {
            tube.InternalError.Should().Be(SampleTubeError.MissingTestOrders);
        }

        // Assert TaskCardCommand was sent
        senderMock.Verify(x => x.Send(It.IsAny<TaskCardCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldUpdateTrayErrorCount_WhenTubesHaveMissingTestOrdersError()
    {
        // Arrange
        var sampleStorage = ServiceProvider.GetRequiredService<IInstrumentSampleStorage>();
        sampleStorage.UpdateDrawer(_instrumentNumber2, new SampleDrawer
        {
            DrawerType = DrawerType.Sample1,
            Trays = new List<SampleTray>
            {
                new() { Id = _tray3Id, TrayPosition = 1 }
            }
        });
        sampleStorage.AddOrUpdateTube(new SampleTube
        {
            TubeId = _sampleId2 + "_T1",
            SampleId = _sampleId2,
            Area = SampleArea.SampleDrawer1,
            InstrumentNumber = _instrumentNumber2,
            TrayPosition = 1,
            TrayId = _tray3Id,
            TubePosition = 1,
            Status = SampleTubeStatus.InProgress
        }, _ => new SampleTube());
        sampleStorage.AddOrUpdateTube(new SampleTube
        {
            TubeId = _sampleId2 + "_T2",
            SampleId = _sampleId2,
            Area = SampleArea.SampleDrawer1,
            InstrumentNumber = _instrumentNumber2,
            TrayPosition = 1,
            TrayId = _tray3Id,
            TubePosition = 2,
            Status = SampleTubeStatus.InProgress
        }, _ => new SampleTube());

        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();
        var senderMock = new Mock<ISender>();
        senderMock.Setup(x => x.Send(It.IsAny<TaskCardCommand>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var sender = senderMock.Object;
        var sampleTrayHub = ServiceProvider.GetRequiredService<ISampleTrayHub>();

        var handler = new MissingTestOrdersEventHandler(
            sampleStorage,
            sender,
            dbContext,
            sampleTrayHub
        );

        var missingTestOrdersEvent = new MissingTestOrdersEvent(_sampleId2);

        // Act
        await handler.Handle(missingTestOrdersEvent, TestContext.Current.CancellationToken);

        // Assert
        var tray = sampleStorage.GetTray(_tray3Id);
        tray.Should().NotBeNull();
        tray!.TubeErrorCount.Should().Be(2);
        tray.FreePositions.Should().Be(48);

        senderMock.Verify(x => x.Send(It.IsAny<TaskCardCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}