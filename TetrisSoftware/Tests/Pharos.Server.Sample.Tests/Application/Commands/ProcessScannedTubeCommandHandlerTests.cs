using MediatR;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Request;
using Pharos.Server.Application.Sample;
using Pharos.Server.Application.Settings.Sample;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Sample.Application.Commands.ProcessScannedTube;
using Pharos.Server.Sample.Application.Commands.UpdateSampleTrayPosition;
using Pharos.Server.Sample.Application.Interfaces;
using Pharos.Server.Sample.Application.Services;
using Pharos.Server.Sample.Domain;
using Tetris.Protocol.Common;
using Tetris.Protocol.Pharos;

namespace Pharos.Server.Sample.Tests.Application.Commands;

[Collection(nameof(SampleTestsCollection))]
public class ProcessScannedTubeCommandHandlerTests(SampleTestsWaf factory) : DatabaseTestBase<SampleTestsWaf>(factory)
{
    private const string _instrumentNumber = "Instrument1";
    private const SampleArea _area = SampleArea.SampleDrawer1;
    private const int _tubePosition = 10;
    private const int _trayPosition = 1;
    private const string _trayBarcode = "1234";
    private ProcessScannedTubeCommandHandler _handler = default!;
    private InstrumentSampleStorage _sampleStorage = default!;
    private Mock<ISampleQcIdProvider> _sampleIdProvider = default!;
    private Mock<IPublisher> _publisherMock = default!;
    private Mock<IInstrumentManager> _instrumentServiceMock = default!;
    private Mock<IMediator> _mediator = default!;
    private Mock<IInstrument> _instrumentMock = default!;

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();

        _publisherMock = new Mock<IPublisher>();

        _publisherMock
               .Setup(x => x.Publish(It.IsAny<TubesIdentifiedEvent>(), It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);

        _instrumentServiceMock = new Mock<IInstrumentManager>();
        _instrumentMock = new Mock<IInstrument>();
        _sampleStorage = new InstrumentSampleStorage();
        _sampleIdProvider = new Mock<ISampleQcIdProvider>();
        _mediator = new Mock<IMediator>();

        _instrumentServiceMock
            .Setup(x => x.GetInstrument(_instrumentNumber))
            .Returns(_instrumentMock.Object);

        var samplesSettings = new SamplesSettings { Drawer = new Drawer() };
        var samplesSettingsMock = new Mock<IOptionsSnapshot<SamplesSettings>>();
        samplesSettingsMock.Setup(x => x.Value).Returns(samplesSettings);

        _handler = new ProcessScannedTubeCommandHandler(
            _sampleStorage,
            _sampleIdProvider.Object,
            _publisherMock.Object,
            _instrumentServiceMock.Object,
            samplesSettingsMock.Object,
            _mediator.Object);
    }

    public override ValueTask DisposeAsync()
    {
        _handler = null!;
        _sampleStorage = null!;
        _sampleIdProvider = null!;
        _publisherMock = null!;
        _instrumentServiceMock = null!;
        _instrumentMock = null!;
        return base.DisposeAsync();
    }

    [Fact]
    public async Task Handle_ShouldReturnErrorResponse_WhenBarcodeCannotBeRead()
    {
        // Arrange
        var pharosGenerateTubeId = $"{DateTime.Now.Ticks:X}";
        var tempTubeId = $"{_instrumentNumber}_{_area}_{_tubePosition}_{_trayPosition}";
        var position = new SampleTubePosition { Area = EnumSampleArea.SampleDrawer1, TrayBarcode = "1234", TrayPosition = 1, TubePosition = 10 };
        var detectedTube = new SampleTube
        {
            InstrumentNumber = _instrumentNumber,
            Area = _area,
            TubePosition = _tubePosition,
            TrayId = _trayBarcode,
            TrayPosition = _trayPosition
        };

        _sampleStorage.AddOrUpdateTube(detectedTube, _ => detectedTube);

        _sampleIdProvider
            .Setup(x => x.GetSampleQcTubeKey(string.Empty))
            .ReturnsAsync(new SampleQcTubeKey("#$123#$", SampleType.Sample, string.Empty, pharosGenerateTubeId, true, true, string.Empty, -1));

        var command = new ProcessScannedTubeCommand(
            _instrumentNumber,
            new ScannedSampleQcRequest
            {
                CannotReadBarcode = true,
                CurrentPosition = position
            });

        // Act
        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.TubeId.Should().NotBeNullOrEmpty();
        result.SampleType.Should().Be(EnumSampleType.UnspecifiedSampleType);
        result.WantedDestination.Area.Should().Be(EnumSampleMoveArea.NoMove);
        result.Error.Should().Be(EnumBarcodeReadingError.UnreadableTubeBarcode);

        // Verify that the tube temporary tubeId is updated
        _sampleStorage.TubesAsQueryable(_instrumentNumber).Count().Should().Be(1);
        var tube = _sampleStorage.GetTube(pharosGenerateTubeId);
        tube.Should().NotBeNull();
        var tempTube = _sampleStorage.GetTube(tempTubeId);
        tempTube.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldReturnErrorResponse_WhenBarcodeIsInvalid()
    {
        // Arrange
        var pharosGenerateTubeId = $"{DateTime.Now.Ticks:X}";
        var tempTubeId = $"{_instrumentNumber}_{_area}_{_tubePosition}_{_trayPosition}";
        var position = new SampleTubePosition { Area = EnumSampleArea.SampleDrawer1, TrayBarcode = "1234", TrayPosition = 1, TubePosition = 10 };
        var detectedTube = new SampleTube
        {
            InstrumentNumber = _instrumentNumber,
            Area = SampleArea.SampleDrawer1,
            TubePosition = 10,
            TrayId = _trayBarcode,
            TrayPosition = _trayPosition
        };

        _sampleStorage.AddOrUpdateTube(detectedTube, _ => detectedTube);
        _sampleIdProvider
            .Setup(x => x.GetSampleQcTubeKey("InvalidBarcode", It.IsAny<string>()))
            .ReturnsAsync(new SampleQcTubeKey("#$123#$", SampleType.Sample, string.Empty, pharosGenerateTubeId, true, true, string.Empty, -1));


        var command = new ProcessScannedTubeCommand(
            _instrumentNumber,
            new ScannedSampleQcRequest
            {
                TubeBarcode = "InvalidBarcode",
                CurrentPosition = position
            });

        // Act
        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.TubeId.Should().NotBe("InvalidBarcode");
        result.SampleType.Should().Be(EnumSampleType.UnspecifiedSampleType);
        result.WantedDestination.Area.Should().Be(EnumSampleMoveArea.NoMove);
        result.Error.Should().Be(EnumBarcodeReadingError.InvalidTubeBarcode);

        // Verify that the tube temporary tubeId is updated
        _sampleStorage.TubesAsQueryable(_instrumentNumber).Count().Should().Be(1);
        var tube = _sampleStorage.GetTube(pharosGenerateTubeId);
        tube.Should().NotBeNull();
        var tempTube = _sampleStorage.GetTube(tempTubeId);
        tempTube.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldReturnErrorResponse_WhenTubeIsDuplicate()
    {
        // Arrange
        var pharosGenerateTubeId = $"{DateTime.Now.Ticks:X}";
        var tubeID = "TUBE_ID_1";
        var position = new SampleTubePosition { Area = EnumSampleArea.SampleDrawer1, TrayBarcode = _trayBarcode, TrayPosition = 1, TubePosition = 11 };
        var identifiedTube10 = new SampleTube
        {
            TubeId = tubeID,
            InstrumentNumber = _instrumentNumber,
            Area = SampleArea.SampleDrawer1,
            TubePosition = _tubePosition,
            TrayId = _trayBarcode,
            TrayPosition = _trayPosition
        };
        var identifiedTube11 = new SampleTube
        {
            TubeId = $"{_instrumentNumber}_{_area}_11_{_trayPosition}",
            InstrumentNumber = _instrumentNumber,
            Area = SampleArea.SampleDrawer1,
            TubePosition = 11,
            TrayId = _trayBarcode,
            TrayPosition = 1
        };

        _sampleStorage.AddOrUpdateTube(identifiedTube10, _ => identifiedTube10);
        _sampleStorage.AddOrUpdateTube(identifiedTube11, _ => identifiedTube11);
        _sampleIdProvider
            .Setup(x => x.GetSampleQcTubeKey("DuplicateBarcode", It.IsAny<string>()))
            .ReturnsAsync(new SampleQcTubeKey("DuplicateBarcode", SampleType.Sample, "TUBE_ID", "1", false, true, string.Empty, -1));
        _sampleIdProvider
            .Setup(x => x.GetSampleQcTubeKey(string.Empty))
            .ReturnsAsync(new SampleQcTubeKey("DuplicateBarcode", SampleType.Sample, string.Empty, pharosGenerateTubeId, true, true, string.Empty, -1));
        _instrumentMock
            .Setup(x => x.DrawersLockState[DrawerType.Sample1])
            .Returns(new DrawerLockState(DrawerType.Sample1, LockState.ClosedUnlocked, false));

        var command = new ProcessScannedTubeCommand(
          _instrumentNumber,
          new ScannedSampleQcRequest { TubeBarcode = "DuplicateBarcode", CurrentPosition = position });

        // Act
        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.TubeId.Should().NotBe(tubeID);
        result.SampleType.Should().Be(EnumSampleType.Sample);
        result.WantedDestination.Area.Should().Be(EnumSampleMoveArea.NoMove);
        result.Error.Should().Be(EnumBarcodeReadingError.DuplicateTubeBarcode);

        // Verify that the tube with duplicate tubeId is added to storage with pharos generated TubeID
        _sampleStorage.TubesAsQueryable(_instrumentNumber).Count().Should().Be(2);
        var scannedTube = _sampleStorage.GetTube(pharosGenerateTubeId);
        scannedTube.Should().NotBeNull();
        var existingTube = _sampleStorage.GetTube(tubeID);
        existingTube.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ShouldReturnValidResponse_WhenTubeIsValid()
    {
        // Arrange
        var position = new SampleTubePosition
        {
            Area = EnumSampleArea.SampleDrawer1,
            TrayBarcode = _trayBarcode,
            TrayPosition = _trayPosition,
            TubePosition = _tubePosition,
        };
        var detectedTube = new SampleTube
        {
            InstrumentNumber = _instrumentNumber,
            Area = SampleArea.SampleDrawer1,
            TubePosition = _tubePosition,
            TrayId = _trayBarcode,
            TrayPosition = _trayPosition
        };

        _sampleStorage.AddOrUpdateTube(detectedTube, _ => detectedTube);

        _sampleIdProvider
            .Setup(x => x.GetSampleQcTubeKey("ValidBarcode", It.IsAny<string>()))
            .ReturnsAsync(new SampleQcTubeKey("DuplicateBarcode", SampleType.Sample, "TUBE_ID", "1", false, true, string.Empty, -1));

        var command = new ProcessScannedTubeCommand(
         _instrumentNumber,
         new ScannedSampleQcRequest
         {
             TubeBarcode = "ValidBarcode",
             CurrentPosition = position
         });

        _instrumentMock
            .Setup(x => x.IsInAssayMode)
            .Returns(true);

        _mediator
            .Setup(x => x.Send(It.IsAny<GetTestOrderStatesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, GetTestOrderStatesQueryResultItem>());

        // Act
        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.TubeId.Should().Be("TUBE_ID_1");
        result.SampleId.Should().NotBeNullOrEmpty();
        result.SampleType.Should().Be(EnumSampleType.Sample);
        result.WantedDestination.Area.Should().Be(EnumSampleMoveArea.MoveInternalStorage);

        _sampleStorage.TubesAsQueryable(_instrumentNumber).Count().Should().Be(1);
        var scannedTube = _sampleStorage.GetTube("TUBE_ID_1");
        scannedTube.Should().NotBeNull();

        _publisherMock.Verify(x => x.Publish(It.IsAny<TubesIdentifiedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnValidResponse_WhenTubeIsValid_InstrumentNonInAssayMode()
    {
        // Arrange
        var position = new SampleTubePosition
        {
            Area = EnumSampleArea.SampleDrawer1,
            TrayBarcode = _trayBarcode,
            TrayPosition = _trayPosition,
            TubePosition = _tubePosition,
        };
        var detectedTube = new SampleTube
        {
            InstrumentNumber = _instrumentNumber,
            Area = SampleArea.SampleDrawer1,
            TubePosition = _tubePosition,
            TrayId = _trayBarcode,
            TrayPosition = _trayPosition
        };

        _sampleStorage.AddOrUpdateTube(detectedTube, _ => detectedTube);
        _sampleIdProvider
            .Setup(x => x.GetSampleQcTubeKey("ValidBarcode", It.IsAny<string>()))
            .ReturnsAsync(new SampleQcTubeKey("DuplicateBarcode", SampleType.Sample, "TUBE_ID", "1", false, true, string.Empty, -1));

        var command = new ProcessScannedTubeCommand(
         _instrumentNumber,
         new ScannedSampleQcRequest
         {
             TubeBarcode = "ValidBarcode",
             CurrentPosition = position
         });

        _instrumentMock
            .Setup(x => x.IsInAssayMode)
            .Returns(false);

        _mediator
            .Setup(x => x.Send(It.IsAny<GetTestOrderStatesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, GetTestOrderStatesQueryResultItem>());

        // Act
        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.TubeId.Should().Be("TUBE_ID_1");
        result.SampleId.Should().NotBeNullOrEmpty();
        result.SampleType.Should().Be(EnumSampleType.Sample);
        result.WantedDestination.Area.Should().Be(EnumSampleMoveArea.NoMove);

        _sampleStorage.TubesAsQueryable(_instrumentNumber).Count().Should().Be(1);
        var scannedTube = _sampleStorage.GetTube("TUBE_ID_1");
        scannedTube.Should().NotBeNull();

        _publisherMock.Verify(x => x.Publish(It.IsAny<TubesIdentifiedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldUpdateSampleTrayPosition_WhenDrawerIsOpen()
    {
        // Arrange
        var tubeId = "TUBE_ID_1";
        var trayBarcode = _trayBarcode;
        var trayPosition = 1;
        // Arrange
        var pharosGenerateTubeId = $"{DateTime.Now.Ticks:X}";
        var tubeID = "TUBE_ID_1";
        var position = new SampleTubePosition { Area = EnumSampleArea.SampleDrawer1, TrayBarcode = _trayBarcode, TrayPosition = 1, TubePosition = 11 };
        var identifiedTube10 = new SampleTube
        {
            TubeId = tubeID,
            InstrumentNumber = _instrumentNumber,
            Area = SampleArea.SampleDrawer1,
            TubePosition = _tubePosition,
            TrayId = _trayBarcode,
            TrayPosition = _trayPosition
        };
        var identifiedTube11 = new SampleTube
        {
            TubeId = $"{_instrumentNumber}_{_area}_11_{_trayPosition}",
            InstrumentNumber = _instrumentNumber,
            Area = SampleArea.SampleDrawer1,
            TubePosition = 11,
            TrayId = _trayBarcode,
            TrayPosition = 1
        };

        _sampleStorage.AddOrUpdateTube(identifiedTube10, _ => identifiedTube10);
        _sampleStorage.AddOrUpdateTube(identifiedTube11, _ => identifiedTube11);
        _sampleIdProvider
            .Setup(x => x.GetSampleQcTubeKey("DuplicateBarcode", It.IsAny<string>()))
            .ReturnsAsync(new SampleQcTubeKey("DuplicateBarcode", SampleType.Sample, "TUBE_ID", "1", false, true, string.Empty, -1));
        _sampleIdProvider
            .Setup(x => x.GetSampleQcTubeKey(string.Empty, It.IsAny<string>()))
            .ReturnsAsync(new SampleQcTubeKey("DuplicateBarcode", SampleType.Sample, string.Empty, pharosGenerateTubeId, true, true, string.Empty, -1));

        // Add a tube with the same TubeId to storage
        var existingTube = new SampleTube
        {
            TubeId = tubeId,
            InstrumentNumber = _instrumentNumber,
            Area = SampleArea.SampleDrawer1,
            TubePosition = 5, // any position
            TrayId = trayBarcode,
            TrayPosition = trayPosition
        };
        _sampleStorage.AddOrUpdateTube(existingTube, _ => existingTube);

        // Simulate drawer is open
        _instrumentMock
            .Setup(x => x.DrawersLockState[DrawerType.Sample1])
            .Returns(new DrawerLockState(DrawerType.Sample1, LockState.Open, false));

        var updateCommandSent = false;
        _mediator
            .Setup(x => x.Send(It.IsAny<UpdateSampleTrayPositionCommand>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((cmd, _) =>
            {
                if (cmd is UpdateSampleTrayPositionCommand updateCmd)
                {
                    updateCmd.TrayId.Should().Be(trayBarcode);
                    updateCmd.TubePosition.Should().Be(existingTube.TubePosition.Value);
                    updateCmd.TubeBarcode.Should().BeEmpty();
                    updateCommandSent = true;
                }
            })
            .Returns(Task.FromResult<object>(null!));

        var command = new ProcessScannedTubeCommand(
            _instrumentNumber,
            new ScannedSampleQcRequest { TubeBarcode = "DuplicateBarcode", CurrentPosition = position });

        _mediator
            .Setup(x => x.Send(It.IsAny<GetTestOrderStatesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, GetTestOrderStatesQueryResultItem>());

        // Act
        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        updateCommandSent.Should().BeTrue();
        result.TubeId.Should().Be(tubeId);
        _mediator.Verify(x => x.Send(It.IsAny<UpdateSampleTrayPositionCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidTube_AssayMode_InternalStorageAtCapacityExcludingReservedQcPositions_ShouldMoveToDrawer()
    {
        // Arrange
        var position = new SampleTubePosition
        {
            Area = EnumSampleArea.SampleDrawer1,
            TrayBarcode = _trayBarcode,
            TrayPosition = _trayPosition,
            TubePosition = _tubePosition,
        };
        var detectedTube = new SampleTube
        {
            InstrumentNumber = _instrumentNumber,
            Area = SampleArea.SampleDrawer1,
            TubePosition = _tubePosition,
            TrayId = _trayBarcode,
            TrayPosition = _trayPosition
        };

        _sampleStorage.AddOrUpdateTube(detectedTube, _ => detectedTube);
        SetupInternalStorageAtCapacity();
        SetupDrawerWithFreePosition(DrawerType.Sample1);
        SetupDrawerSettings(DrawerMode.BothDrawersAsInputAndOutput);

        _sampleIdProvider
            .Setup(x => x.GetSampleQcTubeKey("ValidBarcode", It.IsAny<string>()))
            .ReturnsAsync(new SampleQcTubeKey("ValidBarcode", SampleType.Sample, "TUBE_ID", "1", false, true, string.Empty, -1));

        var command = new ProcessScannedTubeCommand(
            _instrumentNumber,
            new ScannedSampleQcRequest
            {
                TubeBarcode = "ValidBarcode",
                CurrentPosition = position
            });

        _instrumentMock
            .Setup(x => x.IsInAssayMode)
            .Returns(true);

        _mediator
            .Setup(x => x.Send(It.IsAny<GetTestOrderStatesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, GetTestOrderStatesQueryResultItem>());

        // Act
        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.TubeId.Should().Be("TUBE_ID_1");
        result.SampleId.Should().NotBeNullOrEmpty();
        result.WantedDestination.Area.Should().Be(EnumSampleMoveArea.MoveDrawer1);
        result.WantedDestination.TrayPosition.Should().Be(1);
        result.WantedDestination.TubePosition.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ValidTube_BothDrawersMode_ShouldFindFirstAvailablePosition()
    {
        // Arrange
        var position = new SampleTubePosition
        {
            Area = EnumSampleArea.SampleDrawer1,
            TrayBarcode = _trayBarcode,
            TrayPosition = _trayPosition,
            TubePosition = _tubePosition,
        };
        var detectedTube = new SampleTube
        {
            InstrumentNumber = _instrumentNumber,
            Area = SampleArea.SampleDrawer1,
            TubePosition = _tubePosition,
            TrayId = _trayBarcode,
            TrayPosition = _trayPosition
        };

        _sampleStorage.AddOrUpdateTube(detectedTube, _ => detectedTube);
        SetupDrawerWithFreePosition(DrawerType.Sample1);
        SetupDrawerSettings(DrawerMode.BothDrawersAsInputAndOutput);

        _sampleIdProvider
            .Setup(x => x.GetSampleQcTubeKey("ValidBarcode", It.IsAny<string>()))
            .ReturnsAsync(new SampleQcTubeKey("ValidBarcode", SampleType.Sample, "TUBE_ID", "1", false, true, string.Empty, -1));

        var command = new ProcessScannedTubeCommand(
            _instrumentNumber,
            new ScannedSampleQcRequest
            {
                TubeBarcode = "ValidBarcode",
                CurrentPosition = position
            });

        _instrumentMock
            .Setup(x => x.IsInAssayMode)
            .Returns(false);

        _mediator
            .Setup(x => x.Send(It.IsAny<GetTestOrderStatesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, GetTestOrderStatesQueryResultItem>());

        // Act
        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.WantedDestination.Area.Should().Be(EnumSampleMoveArea.MoveDrawer1);
        result.WantedDestination.TrayPosition.Should().Be(1);
        result.WantedDestination.TubePosition.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ValidTube_LeftAsInputDrawerMode_InProgressTube_ShouldMoveToLeftDrawer()
    {
        // Arrange
        var position = new SampleTubePosition
        {
            Area = EnumSampleArea.SampleDrawer1,
            TrayBarcode = _trayBarcode,
            TrayPosition = _trayPosition,
            TubePosition = _tubePosition,
        };
        var detectedTube = new SampleTube
        {
            InstrumentNumber = _instrumentNumber,
            Area = SampleArea.SampleDrawer1,
            TubePosition = _tubePosition,
            TrayId = _trayBarcode,
            TrayPosition = _trayPosition
        };

        _sampleStorage.AddOrUpdateTube(detectedTube, _ => detectedTube);
        SetupDrawerWithFreePosition(DrawerType.Sample1);
        SetupDrawerSettings(DrawerMode.LeftDrawerAsInputAndRightDrawerAsOutput);

        _sampleIdProvider
            .Setup(x => x.GetSampleQcTubeKey("ValidBarcode", It.IsAny<string>()))
            .ReturnsAsync(new SampleQcTubeKey("ValidBarcode", SampleType.Sample, "TUBE_ID", "1", false, true, string.Empty, -1));

        var command = new ProcessScannedTubeCommand(
            _instrumentNumber,
            new ScannedSampleQcRequest
            {
                TubeBarcode = "ValidBarcode",
                CurrentPosition = position
            });

        _instrumentMock
            .Setup(x => x.IsInAssayMode)
            .Returns(false);

        _mediator
            .Setup(x => x.Send(It.IsAny<GetTestOrderStatesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, GetTestOrderStatesQueryResultItem>());

        // Act
        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.WantedDestination.Area.Should().Be(EnumSampleMoveArea.MoveDrawer1);
        result.WantedDestination.TrayPosition.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ValidTube_NoAvailableDrawerSpace_ShouldReturnNoMove()
    {
        // Arrange
        var position = new SampleTubePosition
        {
            Area = EnumSampleArea.SampleDrawer1,
            TrayBarcode = _trayBarcode,
            TrayPosition = _trayPosition,
            TubePosition = _tubePosition,
        };
        var detectedTube = new SampleTube
        {
            InstrumentNumber = _instrumentNumber,
            Area = SampleArea.SampleDrawer1,
            TubePosition = _tubePosition,
            TrayId = _trayBarcode,
            TrayPosition = _trayPosition
        };

        _sampleStorage.AddOrUpdateTube(detectedTube, _ => detectedTube);
        SetupDrawerSettings(DrawerMode.BothDrawersAsInputAndOutput);

        _sampleIdProvider
            .Setup(x => x.GetSampleQcTubeKey("ValidBarcode", It.IsAny<string>()))
            .ReturnsAsync(new SampleQcTubeKey("ValidBarcode", SampleType.Sample, "TUBE_ID", "1", false, true, string.Empty, -1));

        var command = new ProcessScannedTubeCommand(
            _instrumentNumber,
            new ScannedSampleQcRequest
            {
                TubeBarcode = "ValidBarcode",
                CurrentPosition = position
            });

        _instrumentMock
            .Setup(x => x.IsInAssayMode)
            .Returns(false);

        _mediator
            .Setup(x => x.Send(It.IsAny<GetTestOrderStatesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, GetTestOrderStatesQueryResultItem>());

        // Act
        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.WantedDestination.Area.Should().Be(EnumSampleMoveArea.NoMove);
    }

    [Fact]
    public async Task Handle_ShouldReturnErrorResponse_WithDrawerDestination_WhenBarcodeCannotBeReadAndLeftAsInputDrawerMode()
    {
        // Arrange
        var pharosGenerateTubeId = $"{DateTime.Now.Ticks:X}";
        var position = new SampleTubePosition { Area = EnumSampleArea.SampleDrawer1, TrayBarcode = _trayBarcode, TrayPosition = 1, TubePosition = 1 };
        var detectedTube = new SampleTube
        {
            InstrumentNumber = _instrumentNumber,
            Area = SampleArea.SampleDrawer1,
            TubePosition = _tubePosition,
            TrayId = _trayBarcode,
            TrayPosition = _trayPosition
        };

        _sampleStorage.AddOrUpdateTube(detectedTube, _ => detectedTube);
        SetupDrawerWithFreePosition(DrawerType.Sample2);
        SetupDrawerSettings(DrawerMode.LeftDrawerAsInputAndRightDrawerAsOutput);

        _sampleIdProvider
            .Setup(x => x.GetSampleQcTubeKey(string.Empty))
            .ReturnsAsync(new SampleQcTubeKey("", SampleType.Sample, string.Empty, pharosGenerateTubeId, true, true, string.Empty, -1));

        var command = new ProcessScannedTubeCommand(
            _instrumentNumber,
            new ScannedSampleQcRequest
            {
                CannotReadBarcode = true,
                CurrentPosition = position
            });

        // Act
        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.TubeId.Should().NotBeNullOrEmpty();
        result.Error.Should().Be(EnumBarcodeReadingError.UnreadableTubeBarcode);
        result.WantedDestination.Area.Should().Be(EnumSampleMoveArea.MoveDrawer2);
        result.WantedDestination.TrayPosition.Should().Be(1);
        result.WantedDestination.TubePosition.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ValidQcTube_AssayMode_ShouldMoveToInternalStorageWhenSpaceAvailable()
    {
        // Arrange
        var position = new SampleTubePosition
        {
            Area = EnumSampleArea.SampleDrawer1,
            TrayBarcode = _trayBarcode,
            TrayPosition = _trayPosition,
            TubePosition = _tubePosition,
        };
        var detectedTube = new SampleTube
        {
            InstrumentNumber = _instrumentNumber,
            Area = SampleArea.SampleDrawer1,
            TubePosition = _tubePosition,
            TrayId = _trayBarcode,
            TrayPosition = _trayPosition
        };

        _sampleStorage.AddOrUpdateTube(detectedTube, _ => detectedTube);

        _sampleIdProvider
            .Setup(x => x.GetSampleQcTubeKey("QcBarcode", It.IsAny<string>()))
            .ReturnsAsync(new SampleQcTubeKey("QcBarcode", SampleType.QualityControl, "QC_ID", "1", false, true, string.Empty, 1));

        var command = new ProcessScannedTubeCommand(
            _instrumentNumber,
            new ScannedSampleQcRequest
            {
                TubeBarcode = "QcBarcode",
                CurrentPosition = position
            });

        _instrumentMock
            .Setup(x => x.IsInAssayMode)
            .Returns(true);

        _mediator
            .Setup(x => x.Send(It.IsAny<GetTestOrderStatesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, GetTestOrderStatesQueryResultItem>());

        // Act
        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.SampleType.Should().Be(EnumSampleType.QualityControl);
        result.WantedDestination.Area.Should().Be(EnumSampleMoveArea.MoveInternalStorage);

    }

    [Fact]
    public async Task Handle_ValidQcTube_AssayMode_InternalStorageAtCapacity_ShouldMoveToDrawer()
    {
        // Arrange
        var position = new SampleTubePosition
        {
            Area = EnumSampleArea.SampleDrawer1,
            TrayBarcode = _trayBarcode,
            TrayPosition = _trayPosition,
            TubePosition = _tubePosition,
        };
        var detectedTube = new SampleTube
        {
            InstrumentNumber = _instrumentNumber,
            Area = SampleArea.SampleDrawer1,
            TubePosition = _tubePosition,
            TrayId = _trayBarcode,
            TrayPosition = _trayPosition
        };

        _sampleStorage.AddOrUpdateTube(detectedTube, _ => detectedTube);

        for (var i = 0; i < Constants.SamplesInternalStorageMaxCap; i++)
        {
            var tube = new SampleTube
            {
                TubeId = $"INTERNAL_TUBE_{i}",
                InstrumentNumber = _instrumentNumber,
                Area = SampleArea.InternalStorage,
                TubePosition = i + 1,
                SampleType = i < Constants.InternalStoragePositionsReservedForQc ? SampleType.QualityControl : SampleType.Sample
            };
            _sampleStorage.AddOrUpdateTube(tube, _ => tube);
        }

        SetupDrawerWithFreePosition(DrawerType.Sample1);
        SetupDrawerSettings(DrawerMode.BothDrawersAsInputAndOutput);

        _sampleIdProvider
            .Setup(x => x.GetSampleQcTubeKey("QcBarcode", It.IsAny<string>()))
            .ReturnsAsync(new SampleQcTubeKey("QcBarcode", SampleType.QualityControl, "QC_ID", "1", false, true, string.Empty, 1));

        var command = new ProcessScannedTubeCommand(
            _instrumentNumber,
            new ScannedSampleQcRequest
            {
                TubeBarcode = "QcBarcode",
                CurrentPosition = position
            });

        _instrumentMock
            .Setup(x => x.IsInAssayMode)
            .Returns(true);

        _mediator
            .Setup(x => x.Send(It.IsAny<GetTestOrderStatesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, GetTestOrderStatesQueryResultItem>());

        // Act
        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.SampleType.Should().Be(EnumSampleType.QualityControl);
        result.WantedDestination.Area.Should().Be(EnumSampleMoveArea.MoveDrawer1);
        result.WantedDestination.TrayPosition.Should().Be(1);
        result.WantedDestination.TubePosition.Should().Be(1);
    }

    private void SetupDrawerSettings(DrawerMode drawerMode)
    {
        var drawerSettings = new Drawer
        {
            DrawerMode = drawerMode
        };

        var samplesSettings = new SamplesSettings { Drawer = drawerSettings };
        var samplesSettingsMock = new Mock<IOptionsSnapshot<SamplesSettings>>();
        samplesSettingsMock.Setup(x => x.Value).Returns(samplesSettings);

        _handler = new ProcessScannedTubeCommandHandler(
            _sampleStorage,
            _sampleIdProvider.Object,
            _publisherMock.Object,
            _instrumentServiceMock.Object,
            samplesSettingsMock.Object,
            _mediator.Object);
    }

    private void SetupDrawerWithFreePosition(DrawerType drawerType)
    {
        var trays = new List<SampleTray>
        {
            new() { Id = "TRAY_1", TrayPosition = 1, Drawer = drawerType },
            new() { Id = "TRAY_2", TrayPosition = 2, Drawer = drawerType }
        };

        _sampleStorage.UpdateDrawer(_instrumentNumber, new SampleDrawer
        {
            DrawerType = drawerType,
            Trays = trays
        });
    }

    private void SetupInternalStorageAtCapacity()
    {
        var availablePositions = Constants.SamplesInternalStorageMaxCap - Constants.InternalStoragePositionsReservedForQc;

        for (var i = 0; i < availablePositions; i++)
        {
            var tube = new SampleTube
            {
                TubeId = $"INTERNAL_TUBE_{i}",
                InstrumentNumber = _instrumentNumber,
                Area = SampleArea.InternalStorage,
                SampleType = SampleType.Sample,
                TubePosition = i + 1
            };
            _sampleStorage.AddOrUpdateTube(tube, _ => tube);
        }
    }
}