using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Server.Application.Request;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Domain.Entities;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Sample.Application.Interfaces;
using Pharos.Server.Sample.Domain;
using EntityState = Pharos.Server.Application.Request.EntityState;

namespace Pharos.Server.Sample.Tests.Application.Events;

[Collection(nameof(SampleTestsCollection))]
public class RequestUpdatedEventHandlerTests(SampleTestsWaf factory) : DatabaseTestBase<SampleTestsWaf>(factory)
{
    private readonly string _sampleId = "S001";
    private readonly string _trayId = "TR01";

    private record TestOrder(Guid Uid, TestOrderState State, string? TubeId, bool HasReflexTests, int? LisChannelId = null, EntityState EntityState = EntityState.Unchanged, bool IsActive = true) : ITestOrder;

    [Fact]
    public async Task Handle_ShouldUpdateTubesInSampleStorage_WhenConditionsAreMet()
    {
        // Arrange
        var sampleStorage = ServiceProvider.GetRequiredService<IInstrumentSampleStorage>();
        await using (var asyncScope = Factory.Services.CreateAsyncScope())
        {
            var publisher = asyncScope.ServiceProvider.GetRequiredService<IPublisher>();

            sampleStorage.AddOrUpdateTube(new SampleTube
            {
                TubeId = "T01",
                SampleId = _sampleId,
                InstrumentNumber = "Instrument1",
                Area = SampleArea.InternalStorage,
                Status = SampleTubeStatus.InProgress,
                TubePosition = 1,
            }, _ => new SampleTube());

            var testOrderStates = new List<TestOrder>
            {
                new(Guid.NewGuid(), TestOrderState.Pipetted, "T01", false),
                new(Guid.NewGuid(), TestOrderState.NotStarted, "T01", false)
            };
            var requestUid = new RequestUid
            {
                RequestCreatedDate = DateTimeOffset.Now,
                SampleId = _sampleId
            };
            var notification = new RequestUpdatedEvent(1, requestUid, testOrderStates);

            // Act
            await publisher.Publish(notification, TestContext.Current.CancellationToken);
        }

        await WaitForNotificationsAsync();

        // Assert
        var storageTube = sampleStorage.GetTube("T01");
        storageTube.Should().NotBeNull();
        storageTube!.Status.Should().Be(SampleTubeStatus.PartlyCompleted);
    }

    [Fact]
    public async Task Handle_ShouldUpdateTubesInDatabase_WhenConditionsAreMet()
    {
        // Arrange
        await using (var asyncScope = Factory.Services.CreateAsyncScope())
        {
            var publisher = asyncScope.ServiceProvider.GetRequiredService<IPublisher>();
            var dbContext = asyncScope.ServiceProvider.GetRequiredService<ISampleDbContext>();

            var requestUid = new RequestUid
            {
                RequestCreatedDate = DateTimeOffset.Now,
                SampleId = _sampleId
            };

            dbContext.SampleTrays.Add(new SampleTray
            {
                Id = _trayId,
                SampleTubes = new List<SampleTube>
                {
                    new()
                    {
                        TubeId = "T01",
                        SampleId = requestUid.SampleId,
                        RequestCreatedDate = requestUid.RequestCreatedDate,
                        TubePosition = 1,
                        Status = SampleTubeStatus.InProgress
                    }
                }
            });

            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            var testOrderStates = new List<TestOrder>
            {
                new(Guid.NewGuid(), TestOrderState.Pipetted, "T01", false),
                new(Guid.NewGuid(), TestOrderState.NotStarted, "T01", false)
            };

            var notification = new RequestUpdatedEvent(1, requestUid, testOrderStates);

            // Act
            await publisher.Publish(notification, TestContext.Current.CancellationToken);

        }

        await WaitForNotificationsAsync();

        // Assert
        var dbContext1 = ServiceProvider.GetRequiredService<ISampleDbContext>();
        var updatedTube = await dbContext1.SampleTubes.FirstAsync(t => t.TubeId == "T01", TestContext.Current.CancellationToken);
        updatedTube.Status.Should().Be(SampleTubeStatus.PartlyCompleted);
    }

    [Fact]
    public async Task Handle_ShouldNotUpdateTubesInSampleStorage_WhenNoMatchingTubeFound()
    {
        // Arrange
        var sampleStorage = ServiceProvider.GetRequiredService<IInstrumentSampleStorage>();
        var publisher = ServiceProvider.GetRequiredService<IPublisher>();

        var testOrderStates = new List<TestOrder>
        {
            new(Guid.NewGuid(), TestOrderState.Pipetted, "T99", false)
        };
        var requestUid = new RequestUid
        {
            RequestCreatedDate = DateTimeOffset.Now,
            SampleId = _sampleId
        };
        var notification = new RequestUpdatedEvent(1, requestUid, testOrderStates);

        // Act
        await publisher.Publish(notification, TestContext.Current.CancellationToken);

        // Assert
        var storageTube = sampleStorage.GetTube("T99");
        storageTube.Should().BeNull(); // Tube should not exist in storage
    }

    [Fact]
    public async Task Handle_ShouldNotUpdateTubesInDatabase_WhenNoMatchingTubeFound()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();
        var publisher = ServiceProvider.GetRequiredService<IPublisher>();

        dbContext.SampleTrays.Add(new SampleTray
        {
            Id = _trayId
        });

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var testOrderStates = new List<TestOrder>
        {
            new(Guid.NewGuid(), TestOrderState.Pipetted, "T99", false)
        };
        var requestUid = new RequestUid
        {
            RequestCreatedDate = DateTimeOffset.Now,
            SampleId = _sampleId
        };
        var notification = new RequestUpdatedEvent(1, requestUid, testOrderStates);

        // Act
        await publisher.Publish(notification, TestContext.Current.CancellationToken);

        // Assert
        var tubeCount = await dbContext.SampleTubes.CountAsync(TestContext.Current.CancellationToken);
        tubeCount.Should().Be(0); // No tubes should be updated
    }

    [Fact]
    public async Task Handle_ShouldCreatePartlyCompletedTaskCard_WhenPartlyCompletedTubesExistAndNoTaskCardExists()
    {
        // Arrange
        var sampleStorage = ServiceProvider.GetRequiredService<IInstrumentSampleStorage>();
        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();

            sampleStorage.AddOrUpdateTube(new SampleTube
            {
                TubeId = "T02",
                SampleId = _sampleId,
                InstrumentNumber = "Instrument1",
                Area = SampleArea.SampleDrawer1,
                TrayId = _trayId,
                Status = SampleTubeStatus.InProgress,
                TubePosition = 2,
                TrayPosition = 1,
                InternalError = SampleTubeError.None
            }, _ => new SampleTube());

            var testOrderStates = new List<TestOrder>
            {
                new(Guid.NewGuid(), TestOrderState.Pipetted, "T02", false),
                new(Guid.NewGuid(), TestOrderState.NotStarted, "T02", false)
            };

            var requestUid = new RequestUid
            {
                RequestCreatedDate = DateTimeOffset.Now,
                SampleId = _sampleId
            };

            var notification = new RequestUpdatedEvent(1, requestUid, testOrderStates);

            var taskCardExistsBeforeEvent = await sender.Send(new DoesTaskCardExistQuery(TaskCardType.SamplePartlyCompleted, "Instrument1"), TestContext.Current.CancellationToken);
            taskCardExistsBeforeEvent.Should().BeFalse();

            await publisher.Publish(notification, TestContext.Current.CancellationToken);
        }

        await WaitForNotificationsAsync();

        var updatedTube = sampleStorage.GetTube("T02");
        updatedTube.Should().NotBeNull();
        updatedTube!.Status.Should().Be(SampleTubeStatus.PartlyCompleted);

        var sender1 = ServiceProvider.GetRequiredService<ISender>();
        var taskCardExistsAfterEvent = await sender1.Send(new DoesTaskCardExistQuery(TaskCardType.SamplePartlyCompleted, "Instrument1"), TestContext.Current.CancellationToken);
        taskCardExistsAfterEvent.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldUpdateTraySummary_WhenTubesAreUpdated()
    {
        // Arrange
        var sampleStorage = ServiceProvider.GetRequiredService<IInstrumentSampleStorage>();
        var instrumentNumber = "SEED_03";
        var trayId = "TR03";
        var sampleId = "S003";

        sampleStorage.UpdateDrawer(instrumentNumber, new SampleDrawer
        {
            DrawerType = DrawerType.Sample1,
            Trays = new List<SampleTray>
            {
                new() { Id = trayId, TrayPosition = 1 }
            }
        });

        sampleStorage.AddOrUpdateTube(new SampleTube
        {
            TubeId = sampleId + "_T1",
            SampleId = sampleId,
            Area = SampleArea.SampleDrawer1,
            InstrumentNumber = instrumentNumber,
            TrayPosition = 1,
            TrayId = trayId,
            TubePosition = 1,
            Status = SampleTubeStatus.InProgress,
            InternalError = SampleTubeError.MissingTestOrders
        }, _ => new SampleTube());

        sampleStorage.AddOrUpdateTube(new SampleTube
        {
            TubeId = sampleId + "_T2",
            SampleId = sampleId,
            Area = SampleArea.SampleDrawer1,
            InstrumentNumber = instrumentNumber,
            TrayPosition = 1,
            TrayId = trayId,
            TubePosition = 2,
            Status = SampleTubeStatus.InProgress,
            InternalError = SampleTubeError.MissingTestOrders
        }, _ => new SampleTube());

        sampleStorage.UpdateTraysSummary(instrumentNumber);

        var testOrderStates = new List<TestOrder>
        {
            new(Guid.NewGuid(), TestOrderState.Pipetted, sampleId + "_T1", false),
            new(Guid.NewGuid(), TestOrderState.Pipetted, sampleId + "_T2", false)
        };

        var requestUid = new RequestUid
        {
            RequestCreatedDate = DateTimeOffset.Now,
            SampleId = sampleId
        };

        // Act
        await using (var asyncScope = Factory.Services.CreateAsyncScope())
        {
            var notification = new RequestUpdatedEvent(1, requestUid, testOrderStates);
            var publisher = asyncScope.ServiceProvider.GetRequiredService<IPublisher>();

            await publisher.Publish(notification, TestContext.Current.CancellationToken);
        }

        await WaitForNotificationsAsync();

        // Assert
        var tray = sampleStorage.GetTray(trayId);
        tray.Should().NotBeNull();
        tray!.TubeErrorCount.Should().Be(0);
        tray.FreePositions.Should().Be(48);
    }
}
