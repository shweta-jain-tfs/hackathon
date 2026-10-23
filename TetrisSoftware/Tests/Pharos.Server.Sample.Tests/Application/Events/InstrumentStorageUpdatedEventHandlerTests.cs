using MediatR;
using Pharos.Contracts.Notifications;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Request;
using Pharos.Server.Application.Sample;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Sample.Application.Events;
using Pharos.Server.Sample.Application.Interfaces;
using Pharos.Server.Sample.Application.Services;
using Tetris.Protocol.Common;
using Tetris.Protocol.Pharos.Storage;


namespace Pharos.Server.Sample.Tests.Application.Events;

public class InstrumentStorageUpdatedEventHandlerTests
{
    private const string _instrumentNumber = "I1";
    private readonly InstrumentSampleStorage _sampleStorage = new();
    private readonly Mock<IInstrumentManager> _instrumentService = new();
    private readonly Mock<IInstrument> _instrument = new();
    private readonly Mock<ICurrentInstrumentServerHub> _currentInstrumentHub = new();
    private readonly Mock<ISender> _sender = new();
    private readonly Mock<IPublisher> _publisher = new();
    private readonly Mock<ISampleTrayHub> _sampleTrayHub = new();
    private readonly Mock<ISampleTubeHub> _sampleTubeHub = new();
    private readonly Mock<ISampleQcIdProvider> _sampleQcIdProvider = new();

    public InstrumentStorageUpdatedEventHandlerTests()
    {
        _instrumentService.Setup(service => service.GetInstrument(_instrumentNumber)).Returns(_instrument.Object);
        _instrument.Setup(instrument => instrument.Id).Returns(1);
        _currentInstrumentHub.Setup(x => x.LeftSampleDrawerContentsChanged(It.IsAny<int>())).Returns(Task.CompletedTask);
        _currentInstrumentHub.Setup(x => x.RightSampleDrawerContentsChanged(It.IsAny<int>())).Returns(Task.CompletedTask);
        _sampleTrayHub.Setup(x => x.FreePositionAndErrorCountUpdated()).Returns(Task.CompletedTask);
        _sampleTrayHub.Setup(x => x.PositionStatusUpdated(It.IsAny<List<string>>())).Returns(Task.CompletedTask);
        _sampleTubeHub.Setup(x => x.SampleTubeUpdated(It.IsAny<int>())).Returns(Task.CompletedTask);
        _sender.Setup(x => x.Send(It.IsAny<GetRequestIdQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new RequestIdResponseDto(-1, DateTimeOffset.MinValue, false));
        _sender.Setup(x => x.Send(It.IsAny<GetTestOrderStatesQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<string, GetTestOrderStatesQueryResultItem>());
        _sampleQcIdProvider.Setup(x => x.GetSampleQcTubeKey(It.IsAny<string>(), It.IsAny<string>()))
           .ReturnsAsync(new SampleQcTubeKey(null, SampleType.Sample, string.Empty, string.Empty, false, true, string.Empty, 0));
    }

    private InstrumentStorageUpdatedEventHandler CreateHandler()
    {
        return new(
            _sampleStorage,
            _instrumentService.Object,
            _currentInstrumentHub.Object,
            _sender.Object,
            _publisher.Object,
            _sampleTrayHub.Object,
            _sampleTubeHub.Object,
            _sampleQcIdProvider.Object
        );
    }

    [Fact]
    [Trait("ProductRequirement", "PR_312")]
    [Trait("ModuleRequirement", "MR_4140")]
    public async Task StorageUpdatesForScanningDrawerWithTrays()
    {
        // Arrange
        var storage = new InstrumentStorage
        {
            IsFullMessage = true
        };

        var drawer = new SampleDrawer
        {
            Id = EnumSampleDrawer.Drawer1
        };
        storage.SampleDrawers.Add(drawer);

        var tray = new SampleTray
        {
            TrayBarcode = "0001",
            TrayPosition = 1,
            Error = EnumSampleTrayError.UnspecifiedSampleTrayError,
        };
        drawer.Trays.Add(tray);

        // Act
        var notification = new InstrumentStorageUpdatedEvent(_instrumentNumber, storage);
        var handler = CreateHandler();
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        var updatedStorage = new InstrumentStorage
        {
            IsFullMessage = false,
        };

        drawer = new SampleDrawer
        {
            Id = EnumSampleDrawer.Drawer1,
            ScanningOngoing = true,
        };
        drawer.Trays.Add(tray);
        updatedStorage.SampleDrawers.Add(drawer);

        notification = new InstrumentStorageUpdatedEvent(_instrumentNumber, updatedStorage);
        handler = CreateHandler();
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        var result = _sampleStorage.GetDrawer(_instrumentNumber, DrawerType.Sample1);
        result.ScanningOngoing.Should().BeTrue();
        result.Trays.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_FullMessage_ClearsStorageAndAddsTube()
    {
        // Arrange
        var handler = CreateHandler();
        var storage = new InstrumentStorage
        {
            IsFullMessage = true
        };
        var drawer = new SampleDrawer { Id = EnumSampleDrawer.Drawer1 };
        storage.SampleDrawers.Add(drawer);
        var tray = new SampleTray { TrayBarcode = "T1", TrayPosition = 1 };
        drawer.Trays.Add(tray);
        var tube = new SampleTube
        {
            TubeId = "TUBE1",
            Position = new SampleTubePosition
            {
                Area = EnumSampleArea.SampleDrawer1,
                TrayBarcode = "T1",
                TrayPosition = 1,
                TubePosition = 1
            }
        };
        storage.Samples.Add(tube);

        var notification = new InstrumentStorageUpdatedEvent(_instrumentNumber, storage);
        _instrument.Setup(x => x.Id).Returns(1);

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        var tubes = _sampleStorage.TubesAsQueryable(_instrumentNumber).ToList();
        tubes.Should().ContainSingle(t => t.TubeId == "TUBE1");
        _sampleTubeHub.Verify(x => x.SampleTubeUpdated(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Handle_TraySummaryChanged_InvokesSampleTrayHub()
    {
        // Arrange
        var handler = CreateHandler();
        var storage = new InstrumentStorage { IsFullMessage = false };
        var drawer = new SampleDrawer { Id = EnumSampleDrawer.Drawer1 };
        var tray = new SampleTray { TrayBarcode = "T1", TrayPosition = 1 };
        drawer.Trays.Add(tray);
        storage.SampleDrawers.Add(drawer);
        _instrument.Setup(x => x.Id).Returns(1);
        _sampleTrayHub.Setup(x => x.FreePositionAndErrorCountUpdated()).Returns(Task.CompletedTask);

        var notification = new InstrumentStorageUpdatedEvent(_instrumentNumber, storage);

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        _sampleTrayHub.Verify(x => x.FreePositionAndErrorCountUpdated(), Times.AtLeastOnce());
        _sampleTrayHub.Verify(x => x.PositionStatusUpdated(It.Is<List<string>>(ids => ids.Contains("T1"))), Times.AtLeastOnce());
    }

    [Fact]
    public async Task Handle_ErrorCount_SendsTaskCardCommand()
    {
        // Arrange
        var handler = CreateHandler();
        var storage = new InstrumentStorage { IsFullMessage = false };
        var drawer = new SampleDrawer { Id = EnumSampleDrawer.Drawer1 };
        var tray = new SampleTray { TrayBarcode = "T1", TrayPosition = 1 };


        drawer.Trays.Add(tray);
        storage.SampleDrawers.Add(drawer);

        var tube = new SampleTube
        {
            TubeId = "TUBE1",
            TubeBarcode = "TUBE1",
            ChangeType = EnumChangeType.AddedObject,
            Position = new SampleTubePosition
            {
                TrayBarcode = "T1",
                TrayPosition = 1,
                TubePosition = 1,
                Area = EnumSampleArea.SampleDrawer1
            },
            Error = EnumSampleTubeError.TubeGripError
        };
        storage.Samples.Add(tube);
        _instrument.Setup(x => x.Id).Returns(1);
        var notification = new InstrumentStorageUpdatedEvent(_instrumentNumber, storage);

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        _sender.Verify(x => x.Send(It.Is<TaskCardCommand>(c => c.CardType == TaskCardType.SampleTubeError), It.IsAny<CancellationToken>()), Times.AtLeastOnce());
        _sampleTubeHub.Verify(x => x.SampleTubeUpdated(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NoErrorCount_SendsRemoveTaskCardCommand()
    {
        // Arrange
        var handler = CreateHandler();
        var storage = new InstrumentStorage { IsFullMessage = false };
        var drawer = new SampleDrawer { Id = EnumSampleDrawer.Drawer1 };
        storage.SampleDrawers.Add(drawer);
        _instrument.Setup(x => x.Id).Returns(1);
        _sampleStorage.UpdateDrawer(_instrumentNumber, new Pharos.Server.Sample.Domain.SampleDrawer
        {
            DrawerType = DrawerType.Sample1,
            Trays = new List<Pharos.Server.Sample.Domain.SampleTray>
            {
                new() { Id = "T1", TrayPosition = 1, TubeErrorCount = 0 }
            }
        });

        var notification = new InstrumentStorageUpdatedEvent(_instrumentNumber, storage);

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        _sender.Verify(x => x.Send(It.Is<RemoveTaskCardCommand>(c => c.CardType == TaskCardType.SampleTubeError), It.IsAny<CancellationToken>()), Times.AtLeastOnce());
    }

    [Fact]
    public async Task Handle_PartialUpdate_UpdatesTube_WhenChangeTypeIsUpdatedObject()
    {
        // Arrange
        var handler = CreateHandler();
        var tubeId = "TUBE_UPDATE";
        var trayBarcode = "T1";
        // Add initial tube
        _sampleStorage.AddOrUpdateTube(new Pharos.Server.Sample.Domain.SampleTube
        {
            TubeId = tubeId,
            InstrumentNumber = _instrumentNumber,
            TubePosition = 1,
            Area = SampleArea.SampleDrawer1,
            TrayPosition = 1,
            Status = SampleTubeStatus.InProgress
        }, t => t);

        var storage = new InstrumentStorage
        {
            IsFullMessage = false
        };
        var tube = new SampleTube
        {
            TubeId = tubeId,
            ChangeType = EnumChangeType.UpdatedObject,
            SampleQcStatus = EnumSampleQcTubeStatus.SampleQcTubeStatusInProgress,
            Position = new SampleTubePosition
            {
                Area = EnumSampleArea.SampleDrawer1,
                TrayBarcode = trayBarcode,
                TrayPosition = 1,
                TubePosition = 2
            }
        };
        storage.Samples.Add(tube);

        // Add the tray to the storage so the handler can associate the tube
        var drawer = new SampleDrawer { Id = EnumSampleDrawer.Drawer1 };
        var tray = new SampleTray { TrayBarcode = trayBarcode, TrayPosition = 1 };
        drawer.Trays.Add(tray);
        storage.SampleDrawers.Add(drawer);

        var notification = new InstrumentStorageUpdatedEvent(_instrumentNumber, storage);

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        var result = _sampleStorage.GetTube(tubeId);
        result.Should().NotBeNull();
        result!.TubeId.Should().Be(tubeId);
        result.TubePosition.Should().Be(2);
        result.Status.Should().Be(SampleTubeStatus.InProgress);
        _sampleTubeHub.Verify(x => x.SampleTubeUpdated(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Handle_PartialUpdate_RemovesTube_WhenChangeTypeIsDeletedObject()
    {
        // Arrange
        var handler = CreateHandler();
        var tubeId = "TUBE_DELETE";
        var sampleId = "SAMPLE_DELETE";
        var requestCreatedDate = DateTimeOffset.UtcNow;
        var trayBarcode = "T1";
        // Add initial tube
        _sampleStorage.AddOrUpdateTube(new Pharos.Server.Sample.Domain.SampleTube
        {
            TubeId = tubeId,
            SampleId = sampleId,
            RequestCreatedDate = requestCreatedDate,
            InstrumentNumber = _instrumentNumber,
            TubePosition = 1,
            Area = SampleArea.SampleDrawer1,
            TrayPosition = 1,
            InternalError = SampleTubeError.MissingTestOrders
        }, t => t);

        var storage = new InstrumentStorage
        {
            IsFullMessage = false
        };
        var tube = new SampleTube
        {
            TubeId = tubeId,
            SampleId = sampleId,
            ChangeType = EnumChangeType.DeletedObject,
            Position = new SampleTubePosition
            {
                Area = EnumSampleArea.SampleDrawer1,
                TrayBarcode = trayBarcode,
                TrayPosition = 1,
                TubePosition = 1
            }
        };
        storage.Samples.Add(tube);

        // Add the tray to the storage so the handler can associate the tube
        var drawer = new SampleDrawer { Id = EnumSampleDrawer.Drawer1 };
        var tray = new SampleTray { TrayBarcode = trayBarcode, TrayPosition = 1 };
        drawer.Trays.Add(tray);
        storage.SampleDrawers.Add(drawer);

        var notification = new InstrumentStorageUpdatedEvent(_instrumentNumber, storage);

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        var result = _sampleStorage.GetTube(tubeId);
        result.Should().BeNull();
        _sampleTubeHub.Verify(x => x.SampleTubeUpdated(It.IsAny<int>()), Times.Never);
        _publisher.Verify(x => x.Publish(
            It.Is<TubesUnloadedEvent>(e =>
                e.Tubes.Count == 1
                && e.Tubes[0].TubeId == tubeId
                && e.Tubes[0].SampleId == sampleId
                && e.Tubes[0].HasErrors),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PartialUpdate_UpdatesTube_AfterTubeIdSetUsingBarcode()
    {
        // Arrange
        var handler = CreateHandler();
        var tubeBarcode = "TUBE_BARCODE";
        var tubeId = "TUBE_UPDATE";
        var sampleId = "SAMPLEID_UPDATE";
        var trayBarcode = "T1";
        // tube added before scan 
        _sampleStorage.AddOrUpdateTube(new Pharos.Server.Sample.Domain.SampleTube
        {
            InstrumentNumber = _instrumentNumber,
            Area = SampleArea.SampleDrawer1,
            TrayId = trayBarcode,
            TubePosition = 1,
            TrayPosition = 1
        }, t => t);

        // tube added after scan
        var tube = new Pharos.Server.Sample.Domain.SampleTube
        {
            InstrumentNumber = _instrumentNumber,
            TubeId = tubeId,
            TrayId = trayBarcode,
            TubePosition = 1,
            Area = SampleArea.SampleDrawer1,
            TrayPosition = 1,
        };

        _sampleStorage.AddOrUpdateTube(tube, oldTube =>
        {
            oldTube.SampleId = sampleId;
            oldTube.SampleIdSuffix = "1";
            oldTube.Status = SampleTubeStatus.InProgress;
            return oldTube;
        });

        // Storage updates after scan with identity source from ISW
        var storage = new InstrumentStorage
        {
            IsFullMessage = false
        };
        var identifiedTube = new SampleTube
        {
            TubeBarcode = tubeBarcode,
            TubeId = tubeId,
            SampleId = sampleId,
            IdentitySource = EnumIdentitySource.BarcodeScanning,
            ChangeType = EnumChangeType.UpdatedObject,
            SampleQcStatus = EnumSampleQcTubeStatus.SampleQcTubeStatusInProgress,
            Position = new SampleTubePosition
            {
                Area = EnumSampleArea.SampleDrawer1,
                TrayBarcode = trayBarcode,
                TrayPosition = 1,
                TubePosition = 1,
            }
        };
        storage.Samples.Add(identifiedTube);

        // Add the tray to the storage so the handler can associate the tube
        var drawer = new SampleDrawer { Id = EnumSampleDrawer.Drawer1 };
        var tray = new SampleTray { TrayBarcode = trayBarcode, TrayPosition = 1 };
        drawer.Trays.Add(tray);
        storage.SampleDrawers.Add(drawer);

        var notification = new InstrumentStorageUpdatedEvent(_instrumentNumber, storage);
        _sender.Setup(x => x.Send(It.Is<GetRequestIdQuery>(q => q.SampleId == sampleId), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new RequestIdResponseDto(1, DateTimeOffset.UtcNow, true));

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        var result = _sampleStorage.GetTube(tubeId);
        result.Should().NotBeNull();
        result!.TubeBarcode.Should().Be(tubeBarcode);
        result!.TubeId.Should().Be(tubeId);
        result!.SampleId.Should().Be(sampleId);
        result!.SampleIdSuffix.Should().Be("1");
        result.Status.Should().Be(SampleTubeStatus.InProgress);
        _sampleTubeHub.Verify(x => x.SampleTubeUpdated(1), Times.Once);
    }

    [Fact]
    public async Task Handle_PartialUpdate_UpdatesTube_AfterTubeIdSetUsingManualEntry()
    {
        // Arrange
        var handler = CreateHandler();
        var tubeBarcode = "TUBE_BARCODE";
        var tubeId = "TUBE_UPDATE";
        var sampleId = "SAMPLEID_UPDATE";
        var trayBarcode = "T1";
        // On closing drawer add the predefined tube to storage
        _sampleStorage.AddOrUpdateTube(new Pharos.Server.Sample.Domain.SampleTube
        {
            InstrumentNumber = _instrumentNumber,
            TrayPosition = 1,
            TrayId = "9999",
            TubePosition = 1,
            TubeId = tubeId,
            SampleId = sampleId,
            SampleIdSuffix = "01",
            Status = SampleTubeStatus.InProgress,
            IdentitySource = SampleTubeIdentitySource.External
        }, t => t);

        // Storage updates after scan with identity source from ISW
        var storage = new InstrumentStorage
        {
            IsFullMessage = false
        };
        var identifiedTube = new SampleTube
        {
            TubeBarcode = tubeBarcode,
            TubeId = tubeId,
            SampleId = sampleId,
            IdentitySource = EnumIdentitySource.ManualInput,
            ChangeType = EnumChangeType.UpdatedObject,
            SampleQcStatus = EnumSampleQcTubeStatus.SampleQcTubeStatusInProgress,
            Position = new SampleTubePosition
            {
                Area = EnumSampleArea.SampleDrawer1,
                TrayBarcode = trayBarcode,
                TrayPosition = 1,
                TubePosition = 1,
            }
        };
        storage.Samples.Add(identifiedTube);

        // Add the tray to the storage so the handler can associate the tube
        var drawer = new SampleDrawer { Id = EnumSampleDrawer.Drawer1 };
        var tray = new SampleTray { TrayBarcode = trayBarcode, TrayPosition = 1 };
        drawer.Trays.Add(tray);
        storage.SampleDrawers.Add(drawer);

        var notification = new InstrumentStorageUpdatedEvent(_instrumentNumber, storage);
        _sender.Setup(x => x.Send(It.Is<GetRequestIdQuery>(q => q.SampleId == sampleId), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new RequestIdResponseDto(2, DateTimeOffset.UtcNow, true));

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        _sampleStorage.TubesAsQueryable(_instrumentNumber).Where(t => t.Area == SampleArea.SampleDrawer1 && t.TubePosition == 1 && t.TrayPosition == 1).Count().Should().Be(1);
        var result = _sampleStorage.GetTube(tubeId);
        result.Should().NotBeNull();
        result!.TubeBarcode.Should().Be(tubeBarcode);
        result!.TubeId.Should().Be(tubeId);
        result!.SampleId.Should().Be(sampleId);
        result!.SampleIdSuffix.Should().Be("01");
        result.Status.Should().Be(SampleTubeStatus.InProgress);
        _sampleTubeHub.Verify(x => x.SampleTubeUpdated(2), Times.Once);
    }

    [Fact]
    public async Task Handle_PartialUpdate_AddsTube_WhenChangeTypeIsAddedObject()
    {
        // Arrange
        var handler = CreateHandler();
        var tubeId = "TUBE_ADD";
        var trayBarcode = "T1";
        var storage = new InstrumentStorage
        {
            IsFullMessage = false
        };
        var tube = new SampleTube
        {
            TubeId = tubeId,
            ChangeType = EnumChangeType.AddedObject,
            Position = new SampleTubePosition
            {
                Area = EnumSampleArea.SampleDrawer1,
                TrayBarcode = trayBarcode,
                TrayPosition = 1,
                TubePosition = 1
            }
        };
        storage.Samples.Add(tube);

        // Add the tray to the storage so the handler can associate the tube
        var drawer = new SampleDrawer { Id = EnumSampleDrawer.Drawer1 };
        var tray = new SampleTray { TrayBarcode = trayBarcode, TrayPosition = 1 };
        drawer.Trays.Add(tray);
        storage.SampleDrawers.Add(drawer);

        var notification = new InstrumentStorageUpdatedEvent(_instrumentNumber, storage);

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        var result = _sampleStorage.GetTube(tubeId);
        result.Should().NotBeNull();
        result!.TubeId.Should().Be(tubeId);
        result.TrayId.Should().Be(trayBarcode);
        _sampleTubeHub.Verify(x => x.SampleTubeUpdated(It.IsAny<int>()), Times.Never);
    }
    [Fact]
    public async Task Handle_PartlyCompletedTube_SendsTaskCardCommand()
    {
        // Arrange
        var handler = CreateHandler();
        var storage = new InstrumentStorage();
        var drawer = new SampleDrawer { Id = EnumSampleDrawer.Drawer1 };
        var tray = new SampleTray { TrayPosition = 1 }; ;
        drawer.Trays.Add(tray);
        storage.SampleDrawers.Add(drawer);

        _sampleStorage.AddOrUpdateTube(new Domain.SampleTube
        {
            TubeId = "T01",
            InstrumentNumber = _instrumentNumber,
            Area = SampleArea.SampleDrawer1,
            TrayId = "T1",
            TrayPosition = 1,
            TubePosition = 1,
            Status = SampleTubeStatus.PartlyCompleted,
            InternalError = SampleTubeError.None
        }, t => t);

        _instrument.Setup(x => x.Id).Returns(1);
        _sender.Setup(x => x.Send(It.IsAny<DoesTaskCardExistQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(false);

        var notification = new InstrumentStorageUpdatedEvent(_instrumentNumber, storage);

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        _sender.Verify(x => x.Send(It.Is<TaskCardCommand>(c => c.CardType == TaskCardType.SamplePartlyCompleted), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NoPartlyCompletedTubes_SendsRemoveTaskCardCommand()
    {
        // Arrange
        var handler = CreateHandler();
        var storage = new InstrumentStorage();
        var drawer = new SampleDrawer { Id = EnumSampleDrawer.Drawer1 };
        var tray = new SampleTray { TrayPosition = 1 };
        drawer.Trays.Add(tray);
        storage.SampleDrawers.Add(drawer);

        // Add a completed tube to storage
        _sampleStorage.AddOrUpdateTube(new Pharos.Server.Sample.Domain.SampleTube
        {
            TubeId = "T01",
            InstrumentNumber = _instrumentNumber,
            Area = SampleArea.SampleDrawer1,
            TrayId = "T1",
            TrayPosition = 1,
            TubePosition = 1,
            Status = SampleTubeStatus.Completed,
            InternalError = SampleTubeError.None
        }, t => t);

        _instrument.Setup(x => x.Id).Returns(1);

        var notification = new InstrumentStorageUpdatedEvent(_instrumentNumber, storage);

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        _sender.Verify(x => x.Send(It.Is<RemoveTaskCardCommand>(c => c.CardType == TaskCardType.SamplePartlyCompleted), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PresenceOfUnloadRequestedTube_SendsTaskCardCommand()
    {
        // Arrange
        var handler = CreateHandler();
        var storage = new InstrumentStorage();
        var drawer = new SampleDrawer { Id = EnumSampleDrawer.Drawer1 };
        var tray = new SampleTray { TrayPosition = 1 }; ;
        drawer.Trays.Add(tray);
        storage.SampleDrawers.Add(drawer);

        _sampleStorage.AddOrUpdateTube(new Domain.SampleTube
        {
            TubeId = "T01",
            InstrumentNumber = _instrumentNumber,
            Area = SampleArea.SampleDrawer1,
            TrayId = "T1",
            TrayPosition = 1,
            TubePosition = 1,
            Status = SampleTubeStatus.Completed,
            UnloadRequested = true,
            InternalError = SampleTubeError.None
        }, t => t);

        _instrument.Setup(x => x.Id).Returns(1);
        _sender.Setup(x => x.Send(It.IsAny<DoesTaskCardExistQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(false);

        var notification = new InstrumentStorageUpdatedEvent(_instrumentNumber, storage);

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        _sender.Verify(x => x.Send(It.Is<TaskCardCommand>(c => c.CardType == TaskCardType.RemoveSamplesFromDrawer), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AbscenceOfNoUnloadRequestedTubes_SendsRemoveTaskCardCommand()
    {
        // Arrange
        var handler = CreateHandler();
        var storage = new InstrumentStorage();
        var drawer = new SampleDrawer { Id = EnumSampleDrawer.Drawer1 };
        var tray = new SampleTray { TrayPosition = 1 };
        drawer.Trays.Add(tray);
        storage.SampleDrawers.Add(drawer);

        // Add a completed tube to storage
        _sampleStorage.AddOrUpdateTube(new Pharos.Server.Sample.Domain.SampleTube
        {
            TubeId = "T01",
            InstrumentNumber = _instrumentNumber,
            Area = SampleArea.SampleDrawer1,
            TrayId = "T1",
            TrayPosition = 1,
            TubePosition = 1,
            Status = SampleTubeStatus.InProgress,
            InternalError = SampleTubeError.None
        }, t => t);

        _instrument.Setup(x => x.Id).Returns(1);

        var notification = new InstrumentStorageUpdatedEvent(_instrumentNumber, storage);

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        _sender.Verify(x => x.Send(It.Is<RemoveTaskCardCommand>(c => c.CardType == TaskCardType.RemoveSamplesFromDrawer), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_FullMessage_WithIdentifiedTubes_CallsSampleTubeHub()
    {
        // Arrange
        var handler = CreateHandler();
        var sampleId1 = "SAMPLE1";
        var sampleId2 = "SAMPLE2";
        var storage = new InstrumentStorage
        {
            IsFullMessage = true
        };
        var drawer = new SampleDrawer { Id = EnumSampleDrawer.Drawer1 };
        storage.SampleDrawers.Add(drawer);
        var tray = new SampleTray { TrayBarcode = "T1", TrayPosition = 1 };
        drawer.Trays.Add(tray);

        var tube1 = new SampleTube
        {
            TubeId = "TUBE1",
            SampleId = sampleId1,
            Position = new SampleTubePosition
            {
                Area = EnumSampleArea.SampleDrawer1,
                TrayBarcode = "T1",
                TrayPosition = 1,
                TubePosition = 1
            }
        };
        var tube2 = new SampleTube
        {
            TubeId = "TUBE2",
            SampleId = sampleId2,
            Position = new SampleTubePosition
            {
                Area = EnumSampleArea.SampleDrawer1,
                TrayBarcode = "T1",
                TrayPosition = 1,
                TubePosition = 2
            }
        };
        storage.Samples.Add(tube1);
        storage.Samples.Add(tube2);

        var notification = new InstrumentStorageUpdatedEvent(_instrumentNumber, storage);
        _instrument.Setup(x => x.Id).Returns(1);
        _sender.Setup(x => x.Send(It.Is<GetRequestIdQuery>(q => q.SampleId == sampleId1), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new RequestIdResponseDto(10, DateTimeOffset.UtcNow, true));
        _sender.Setup(x => x.Send(It.Is<GetRequestIdQuery>(q => q.SampleId == sampleId2), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new RequestIdResponseDto(20, DateTimeOffset.UtcNow, true));

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        _sampleTubeHub.Verify(x => x.SampleTubeUpdated(10), Times.Once);
        _sampleTubeHub.Verify(x => x.SampleTubeUpdated(20), Times.Once);
    }

    [Fact]
    public async Task Handle_PartialUpdate_UpdatesTube_WhenSampleMovesToAnotherTrayInSameDrawer()
    {
        // Arrange
        var handler = CreateHandler();
        var tubeId = "T001";
        var originalTrayBarcode = "T1";
        var newTrayBarcode = "T2";

        _sampleStorage.AddOrUpdateTube(new Pharos.Server.Sample.Domain.SampleTube
        {
            TubeId = tubeId,
            InstrumentNumber = _instrumentNumber,
            TubePosition = 1,
            Area = SampleArea.SampleDrawer1,
            TrayId = originalTrayBarcode,
            TrayPosition = 2
        }, t => t);

        var storage = new InstrumentStorage
        {
            IsFullMessage = false
        };

        var tube = new SampleTube
        {
            TubeId = tubeId,
            ChangeType = EnumChangeType.UpdatedObject,
            Position = new SampleTubePosition
            {
                Area = EnumSampleArea.SampleDrawer1,
                TrayBarcode = newTrayBarcode,
                TrayPosition = 1,
                TubePosition = 1
            }
        };
        storage.Samples.Add(tube);

        // Add both trays to the storage
        var drawer = new SampleDrawer { Id = EnumSampleDrawer.Drawer1 };
        drawer.Trays.Add(new SampleTray { TrayBarcode = originalTrayBarcode, TrayPosition = 2 });
        drawer.Trays.Add(new SampleTray { TrayBarcode = newTrayBarcode, TrayPosition = 1 });
        storage.SampleDrawers.Add(drawer);

        var notification = new InstrumentStorageUpdatedEvent(_instrumentNumber, storage);
        _instrument.Setup(x => x.Id).Returns(1);

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        var result = _sampleStorage.GetTube(tubeId);
        result!.TubeId.Should().Be(tubeId);
        result.TrayId.Should().Be(newTrayBarcode);
        result.TrayPosition.Should().Be(1);
        result.TubePosition.Should().Be(1);
        result.Area.Should().Be(SampleArea.SampleDrawer1);

        // Verify both trays were tracked as modified
        _sampleTrayHub.Verify(x => x.PositionStatusUpdated(
            It.Is<List<string>>(ids => ids.Contains(originalTrayBarcode) && ids.Contains(newTrayBarcode))),
            Times.AtLeastOnce());

        // Verify only left drawer changed notification was sent
        _currentInstrumentHub.Verify(x => x.LeftSampleDrawerContentsChanged(1), Times.Once);
        _currentInstrumentHub.Verify(x => x.RightSampleDrawerContentsChanged(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Handle_PartialUpdate_UpdatesTube_WhenSampleMovesToDifferentTrayInDifferentDrawer()
    {
        // Arrange
        var handler = CreateHandler();
        var tubeId = "TUBE_MOVE_DIFF_DRAWER";
        var originalTrayBarcode = "T1";
        var newTrayBarcode = "T3";

        // Add initial tube in Tray 1 of Drawer 1
        _sampleStorage.AddOrUpdateTube(new Pharos.Server.Sample.Domain.SampleTube
        {
            TubeId = tubeId,
            InstrumentNumber = _instrumentNumber,
            TubePosition = 2,
            Area = SampleArea.SampleDrawer1,
            TrayId = originalTrayBarcode,
            TrayPosition = 1
        }, t => t);

        var storage = new InstrumentStorage
        {
            IsFullMessage = false
        };

        var tube = new SampleTube
        {
            TubeId = tubeId,
            ChangeType = EnumChangeType.UpdatedObject,
            Position = new SampleTubePosition
            {
                Area = EnumSampleArea.SampleDrawer2,
                TrayBarcode = newTrayBarcode,
                TrayPosition = 2,
                TubePosition = 5
            }
        };
        storage.Samples.Add(tube);

        // Add both drawers with their respective trays
        var drawer1 = new SampleDrawer { Id = EnumSampleDrawer.Drawer1 };
        drawer1.Trays.Add(new SampleTray { TrayBarcode = originalTrayBarcode, TrayPosition = 1 });
        storage.SampleDrawers.Add(drawer1);

        var drawer2 = new SampleDrawer { Id = EnumSampleDrawer.Drawer2 };
        drawer2.Trays.Add(new SampleTray { TrayBarcode = newTrayBarcode, TrayPosition = 2 });
        storage.SampleDrawers.Add(drawer2);

        var notification = new InstrumentStorageUpdatedEvent(_instrumentNumber, storage);
        _instrument.Setup(x => x.Id).Returns(1);

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        var result = _sampleStorage.GetTube(tubeId);
        result!.TubeId.Should().Be(tubeId);
        result.TrayId.Should().Be(newTrayBarcode);
        result.TrayPosition.Should().Be(2);
        result.TubePosition.Should().Be(5);
        result.Area.Should().Be(SampleArea.SampleDrawer2);

        // Verify both trays were tracked as modified
        _sampleTrayHub.Verify(x => x.PositionStatusUpdated(
            It.Is<List<string>>(ids => ids.Contains(originalTrayBarcode) && ids.Contains(newTrayBarcode))),
            Times.AtLeastOnce());

        _currentInstrumentHub.Verify(x => x.LeftSampleDrawerContentsChanged(1), Times.Once);
        _currentInstrumentHub.Verify(x => x.RightSampleDrawerContentsChanged(1), Times.Once);
    }

    [Fact]
    public async Task Handle_FullMessage_WithQcTubes_BypassesTestOrderQueryAndMapsQcStatus()
    {
        // Arrange
        var handler = CreateHandler();
        var qcSampleId = "QC-12345";
        var sampleId = "SAMPLE1";
        var storage = new InstrumentStorage
        {
            IsFullMessage = true
        };
        var drawer = new SampleDrawer { Id = EnumSampleDrawer.Drawer1 };
        storage.SampleDrawers.Add(drawer);
        var tray = new SampleTray { TrayBarcode = "T1", TrayPosition = 1 };
        drawer.Trays.Add(tray);

        var qcTube = new SampleTube
        {
            TubeId = "QC001",
            SampleId = qcSampleId,
            SampleType = EnumSampleType.QualityControl,
            SampleQcStatus = EnumSampleQcTubeStatus.SampleQcTubeStatusCompleted,
            Position = new SampleTubePosition
            {
                Area = EnumSampleArea.SampleDrawer1,
                TrayBarcode = "T1",
                TrayPosition = 1,
                TubePosition = 1
            }
        };
        storage.Samples.Add(qcTube);

        // Regular sample tube
        var sampleTube = new SampleTube
        {
            TubeId = "TUBE1",
            SampleId = sampleId,
            Position = new SampleTubePosition
            {
                Area = EnumSampleArea.SampleDrawer1,
                TrayBarcode = "T1",
                TrayPosition = 1,
                TubePosition = 2
            }
        };
        storage.Samples.Add(sampleTube);

        var notification = new InstrumentStorageUpdatedEvent(_instrumentNumber, storage);
        _instrument.Setup(x => x.Id).Returns(1);
        _sender.Setup(x => x.Send(It.Is<GetRequestIdQuery>(q => q.SampleId == qcSampleId), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new RequestIdResponseDto(100, DateTimeOffset.UtcNow, true));
        _sender.Setup(x => x.Send(It.Is<GetRequestIdQuery>(q => q.SampleId == sampleId), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new RequestIdResponseDto(200, DateTimeOffset.UtcNow, true));

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        var qcResult = _sampleStorage.GetTube("QC001");
        qcResult.Should().NotBeNull();
        qcResult!.Status.Should().Be(SampleTubeStatus.PipettedQc);
        qcResult.SampleType.Should().Be(SampleType.QualityControl);

        _sender.Verify(x => x.Send(
            It.Is<GetTestOrderStatesQuery>(q => !q.SampleIds.Contains(qcSampleId)),
            It.IsAny<CancellationToken>()), Times.Once);

        _sampleTubeHub.Verify(x => x.SampleTubeUpdated(100), Times.Once);
        _sampleTubeHub.Verify(x => x.SampleTubeUpdated(200), Times.Once);
    }

    [Fact]
    public async Task Handle_PartialUpdate_AddsTube_WithQcNotNeededStatus()
    {
        // Arrange
        var handler = CreateHandler();
        var qcTubeId = "QC_NOT_NEEDED";
        var qcSampleId = "QC-88888";
        var trayBarcode = "T1";
        var storage = new InstrumentStorage
        {
            IsFullMessage = false
        };
        var qcTube = new SampleTube
        {
            TubeId = qcTubeId,
            SampleId = qcSampleId,
            SampleType = EnumSampleType.QualityControl,
            SampleQcStatus = EnumSampleQcTubeStatus.SampleQcTubeStatusNotNeeded,
            ChangeType = EnumChangeType.AddedObject,
            Position = new SampleTubePosition
            {
                Area = EnumSampleArea.SampleDrawer1,
                TrayBarcode = trayBarcode,
                TrayPosition = 1,
                TubePosition = 3
            }
        };
        storage.Samples.Add(qcTube);

        var drawer = new SampleDrawer { Id = EnumSampleDrawer.Drawer1 };
        var tray = new SampleTray { TrayBarcode = trayBarcode, TrayPosition = 1 };
        drawer.Trays.Add(tray);
        storage.SampleDrawers.Add(drawer);

        var notification = new InstrumentStorageUpdatedEvent(_instrumentNumber, storage);
        _sender.Setup(x => x.Send(It.Is<GetRequestIdQuery>(q => q.SampleId == qcSampleId), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new RequestIdResponseDto(301, DateTimeOffset.UtcNow, true));

        // Act
        await handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        var result = _sampleStorage.GetTube(qcTubeId);
        result.Should().NotBeNull();
        result!.TubeId.Should().Be(qcTubeId);
        result.InternalError.Should().Be(SampleTubeError.QcNotNeeded);
        result.Status.Should().Be(SampleTubeStatus.InProgress);
        _sampleTubeHub.Verify(x => x.SampleTubeUpdated(301), Times.Once);
    }
}
