using MediatR;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Sample;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Settings.Sample;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Sample.Application.Queries.GetSampleQcDestination;
using Pharos.Server.Sample.Application.Services;
using Pharos.Server.Sample.Domain;
using Tetris.Protocol.Common;
using Tetris.Protocol.Pharos;

namespace Pharos.Server.Sample.Tests.Application.Queries;

public class GetSampleQcDestinationQueryHandlerTests
{
    private const string _instrumentNumber = "SEED_01";
    private const string _tubeId = "TUBE123";
    private const string _externalTubeId = "EXT_TUBE123";
    private readonly InstrumentSampleStorage _sampleStorage = new();
    private readonly Mock<IOptionsSnapshot<SamplesSettings>> _samplesSettingsMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<IInstrumentManager> _instrumentServiceMock;
    private readonly Mock<IInstrument> _instrumentMock;
    private readonly GetSampleQcDestinationQueryHandler _sut;

    public GetSampleQcDestinationQueryHandlerTests()
    {
        _samplesSettingsMock = new Mock<IOptionsSnapshot<SamplesSettings>>();
        _mediatorMock = new Mock<IMediator>();
        _instrumentServiceMock = new Mock<IInstrumentManager>();
        _instrumentMock = new Mock<IInstrument>();
        _instrumentServiceMock.Setup(service => service.GetInstrument(_instrumentNumber)).Returns(_instrumentMock.Object);
        _instrumentMock.Setup(instrument => instrument.State)
            .Returns(Pharos.Server.Domain.Enums.InstrumentState.Standby);
        _sut = new GetSampleQcDestinationQueryHandler(_sampleStorage, _samplesSettingsMock.Object, _instrumentServiceMock.Object, _mediatorMock.Object);
    }

    [Fact]
    public async Task Handle_SampleNotInInternalStorage_ShouldReturnDefaultResponse()
    {
        var query = CreateQuery(EnumSampleArea.SampleDrawer1);

        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        result.WantedDestination.Should().NotBeNull();
        result.WantedDestination.Area.Should().Be(EnumSampleMoveArea.NoMove);
    }

    [Fact]
    public async Task Handle_SampleInInternalStorage_TubeNotFound_ShouldReturnDefaultResponse()
    {
        var query = CreateQuery(EnumSampleArea.InternalStorage);

        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        result.WantedDestination.Should().NotBeNull();
        result.WantedDestination.Area.Should().Be(EnumSampleMoveArea.NoMove);
    }

    [Fact]
    public async Task Handle_SampleInInternalStorage_InProgressTube_HasFreeInternalStorage_ShouldReturnInternalStorageResponse()
    {
        SetupInProgressTubeInShuttle();
        SetupInstrumentInAssayMode(isInAssay: true);
        var query = CreateQueryWithTubeId(EnumSampleArea.Shuttle1);

        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        result.WantedDestination.Should().NotBeNull();
        result.WantedDestination.Area.Should().Be(EnumSampleMoveArea.MoveInternalStorage);
    }

    [Fact]
    public async Task Handle_SampleInInternalStorage_InProgressTube_NoFreeInternalStorage_ShouldMoveToDrawer()
    {
        SetupInternalStorageAtCapacity();
        SetupInProgressTubeInShuttle();
        SetupDrawerWithFreePosition(DrawerType.Sample1);
        SetupDrawerSettings(leftAsInput: false);

        var query = CreateQueryWithTubeId(EnumSampleArea.Shuttle1);

        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        result.WantedDestination.Should().NotBeNull();
        result.WantedDestination.Area.Should().Be(EnumSampleMoveArea.MoveDrawer1);
    }

    [Trait("ProductRequirement", "PR_304")]
    [Trait("ModuleRequirement", "MR_5374")]
    [Fact]
    public async Task Handle_LeftDrawerAsInputRightAsOutput_InProgressTube_ShouldMoveToDrawer1FirstPosition()
    {
        SetupInternalStorageAtCapacity();
        SetupInProgressTubeInShuttle();
        SetupDrawerWithFreePosition(DrawerType.Sample1);
        SetupDrawerSettings(leftAsInput: true);

        var query = CreateQueryWithTubeId(EnumSampleArea.Shuttle1);

        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        result.WantedDestination.Should().NotBeNull();
        result.WantedDestination.Area.Should().Be(EnumSampleMoveArea.MoveDrawer1);
        result.WantedDestination.TubePosition.Should().Be(1);
    }

    [Fact]
    public async Task Handle_LeftDrawerAsInputRightAsOutput_ErrorTube_ShouldMoveToDrawer2FirstPosition()
    {
        SetupInternalStorageAtCapacity();
        SetupErrorTubeInShuttle();
        SetupDrawerWithFreePosition(DrawerType.Sample2);
        SetupDrawerSettings(leftAsInput: true);

        var query = CreateQueryWithTubeId(EnumSampleArea.Shuttle1);

        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        result.WantedDestination.Should().NotBeNull();
        result.WantedDestination.Area.Should().Be(EnumSampleMoveArea.MoveDrawer2);
        result.WantedDestination.TubePosition.Should().Be(1);
    }

    [Fact]
    public async Task Handle_LeftDrawerAsInputRightAsOutput_CompletedTube_ShouldMoveToDrawer2LastPosition()
    {
        SetupInternalStorageAtCapacity();
        SetupCompletedTubeInShuttle();
        SetupDrawerWithFreePosition(DrawerType.Sample2);
        SetupDrawerSettings(leftAsInput: true);

        var query = CreateQueryWithTubeId(EnumSampleArea.Shuttle1);

        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        result.WantedDestination.Should().NotBeNull();
        result.WantedDestination.Area.Should().Be(EnumSampleMoveArea.MoveDrawer2);
        result.WantedDestination.TubePosition.Should().Be(Constants.MaxSampleTubesInTray);
    }

    [Fact]
    public async Task Handle_LeftDrawerAsInputRightAsOutput_NoAvailableSpace_ShouldSendTaskCard()
    {
        SetupInternalStorageAtCapacity();
        SetupInProgressTubeInShuttle();
        SetupDrawerWithNoFreePositions(DrawerType.Sample1);
        SetupDrawerWithNoFreePositions(DrawerType.Sample2);
        SetupDrawerSettings(leftAsInput: true);

        var query = CreateQueryWithTubeId(EnumSampleArea.Shuttle1);

        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        result.WantedDestination.Should().NotBeNull();
        result.WantedDestination.Area.Should().Be(EnumSampleMoveArea.NoMove);
        _mediatorMock.Verify(x => x.Send(It.Is<TaskCardCommand>(cmd =>
            cmd.CardType == TaskCardType.LoadEmptyLeftSampleTray &&
            cmd.InstrumentNumber == _instrumentNumber),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_BothDrawersAsInputOutput_InProgressTube_ShouldUseFirstPosition()
    {
        SetupInternalStorageAtCapacity();
        SetupInProgressTubeInShuttle();
        SetupDrawerWithFreePosition(DrawerType.Sample1);
        SetupDrawerSettings(leftAsInput: false);

        var query = CreateQueryWithTubeId(EnumSampleArea.Shuttle1);

        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        result.WantedDestination.Should().NotBeNull();
        result.WantedDestination.Area.Should().Be(EnumSampleMoveArea.MoveDrawer1);
        result.WantedDestination.TubePosition.Should().Be(1);
    }

    [Fact]
    public async Task Handle_BothDrawersAsInputOutput_ErrorTube_ShouldUseFirstPosition()
    {
        SetupInternalStorageAtCapacity();
        SetupInProgressTubeInShuttle();
        SetupDrawerWithFreePosition(DrawerType.Sample1);
        SetupDrawerSettings(leftAsInput: false);

        var query = CreateQueryWithTubeId(EnumSampleArea.Shuttle1);

        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        result.WantedDestination.Should().NotBeNull();
        result.WantedDestination.Area.Should().Be(EnumSampleMoveArea.MoveDrawer1);
        result.WantedDestination.TubePosition.Should().Be(1);
    }

    [Fact]
    public async Task Handle_BothDrawersAsInputOutput_Drawer1Full_ShouldUseDrawer2()
    {
        SetupInternalStorageAtCapacity();
        SetupInProgressTubeInShuttle();
        SetupDrawerWithNoFreePositions(DrawerType.Sample1);
        SetupDrawerWithFreePosition(DrawerType.Sample2);
        SetupDrawerSettings(leftAsInput: false);

        var query = CreateQueryWithTubeId(EnumSampleArea.Shuttle1);

        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        result.WantedDestination.Should().NotBeNull();
        result.WantedDestination.Area.Should().Be(EnumSampleMoveArea.MoveDrawer2);
    }

    [Fact]
    public async Task Handle_ShouldSkipTraysWithErrors()
    {
        SetupInternalStorageAtCapacity();
        SetupInProgressTubeInShuttle();
        SetupDrawerSettings(leftAsInput: false);

        var trays = new List<SampleTray>
        {
            new() { Id = "333", TrayPosition = 1, Error = SampleTrayError.InvalidBarcode },
            new() { Id = "4444", TrayPosition = 2 }
        };

        _sampleStorage.UpdateDrawer(_instrumentNumber, new SampleDrawer
        {
            DrawerType = DrawerType.Sample1,
            Trays = trays
        });

        var query = CreateQueryWithTubeId(EnumSampleArea.Shuttle1);

        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        result.WantedDestination.Should().NotBeNull();
        result.WantedDestination.TrayPosition.Should().Be(2);
        result.WantedDestination.TubePosition.Should().Be(1);
        result.WantedDestination.Area.Should().Be(EnumSampleMoveArea.MoveDrawer1);
    }

    [Fact]
    public async Task Handle_ShouldReturnDefaultResponse_WhenDrawersAreFull_ShouldSendTaskCard()
    {
        SetupInternalStorageAtCapacity();
        SetupInProgressTubeInShuttle();
        SetupDrawerWithNoFreePositions(DrawerType.Sample1);
        SetupDrawerWithNoFreePositions(DrawerType.Sample2);
        SetupDrawerSettings(leftAsInput: false);

        var query = CreateQueryWithTubeId(EnumSampleArea.Shuttle1);

        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        result.WantedDestination.Should().NotBeNull();
        result.WantedDestination.Area.Should().Be(EnumSampleMoveArea.NoMove);
        _mediatorMock.Verify(x => x.Send(It.Is<TaskCardCommand>(cmd =>
            cmd.CardType == TaskCardType.LoadEmptySampleTray &&
            cmd.InstrumentNumber == _instrumentNumber),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExternalSourceTube_WithAvailableOriginalPosition_ShouldReturnOriginalPosition()
    {
        SetupInternalStorageAtCapacity();
        SetupExternalSourceTubeInShuttle();
        SetupDrawerWithFreePosition(DrawerType.Sample1);

        var query = CreateQueryWithExternalTubeId(EnumSampleArea.Shuttle1);

        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        result.WantedDestination.Should().NotBeNull();
        result.WantedDestination.Area.Should().Be(EnumSampleMoveArea.MoveDrawer1);
        result.WantedDestination.TrayPosition.Should().Be(1);
        result.WantedDestination.TubePosition.Should().Be(3);
    }

    [Fact]
    public async Task Handle_ExternalSourceTube_WithNoAvailableOriginalPosition_ShouldReturnDefaultResponse()
    {
        SetupInternalStorageAtCapacity();
        SetupExternalSourceTubeInShuttle();
        SetupDrawerWithNoFreePositions(DrawerType.Sample1);

        var query = CreateQueryWithExternalTubeId(EnumSampleArea.Shuttle1);

        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        result.WantedDestination.Should().NotBeNull();
        result.WantedDestination.Area.Should().Be(EnumSampleMoveArea.NoMove);
    }

    [Fact]
    public async Task Handle_ShouldExcludeReservedPositions_WhenFindingAvailablePosition()
    {
        SetupInternalStorageAtCapacity();
        SetupInProgressTubeInShuttle();
        SetupDrawerWithFreePosition(DrawerType.Sample1);
        SetupExternalSourceTubeReservingPosition(1, 1);
        SetupDrawerSettings(leftAsInput: false);

        var query = CreateQueryWithTubeId(EnumSampleArea.Shuttle1);

        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        result.WantedDestination.Should().NotBeNull();
        result.WantedDestination.Area.Should().Be(EnumSampleMoveArea.MoveDrawer1);
        result.WantedDestination.TrayPosition.Should().Be(1);
        result.WantedDestination.TubePosition.Should().Be(2);
    }

    [Fact]
    public async Task Handle_ShouldFindNextAvailableTray_WhenCurrentTrayFullWithReservedPositions()
    {
        SetupInternalStorageAtCapacity();
        SetupInProgressTubeInShuttle();
        SetupDrawerWithFreePosition(DrawerType.Sample1);
        SetupFullTrayWithReservedPositions(DrawerType.Sample1, 1);
        SetupDrawerSettings(leftAsInput: false);

        var query = CreateQueryWithTubeId(EnumSampleArea.Shuttle1);

        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        result.WantedDestination.Should().NotBeNull();
        result.WantedDestination.Area.Should().Be(EnumSampleMoveArea.MoveDrawer1);
        result.WantedDestination.TrayPosition.Should().Be(2);
        result.WantedDestination.TubePosition.Should().Be(1);
    }

    [Fact]
    public async Task Handle_LeftDrawerAsInputRightAsOutput_ErrorTube_NoSpaceInRightDrawer_ShouldSendRightDrawerTaskCard()
    {
        SetupInternalStorageAtCapacity();
        SetupErrorTubeInShuttle();
        SetupDrawerWithNoFreePositions(DrawerType.Sample2);
        SetupDrawerSettings(leftAsInput: true);

        var query = CreateQueryWithTubeId(EnumSampleArea.Shuttle1);

        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        result.WantedDestination.Should().NotBeNull();
        result.WantedDestination.Area.Should().Be(EnumSampleMoveArea.NoMove);
        _mediatorMock.Verify(x => x.Send(It.Is<TaskCardCommand>(cmd =>
            cmd.CardType == TaskCardType.LoadEmptyRightSampleTray &&
            cmd.InstrumentNumber == _instrumentNumber),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_LeftDrawerAsInputRightAsOutput_CompletedTube_NoSpaceInRightDrawer_ShouldSendRightDrawerTaskCard()
    {
        SetupInternalStorageAtCapacity();
        SetupCompletedTubeInShuttle();
        SetupDrawerWithNoFreePositions(DrawerType.Sample2);
        SetupDrawerSettings(leftAsInput: true);

        var query = CreateQueryWithTubeId(EnumSampleArea.Shuttle1);

        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        result.WantedDestination.Should().NotBeNull();
        result.WantedDestination.Area.Should().Be(EnumSampleMoveArea.NoMove);
        _mediatorMock.Verify(x => x.Send(It.Is<TaskCardCommand>(cmd =>
            cmd.CardType == TaskCardType.LoadEmptyRightSampleTray &&
            cmd.InstrumentNumber == _instrumentNumber),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_LeftDrawerAsInputRightAsOutput_InProgressTube_NoSpaceInLeftDrawer_ShouldSendLeftDrawerTaskCard()
    {
        SetupInternalStorageAtCapacity();
        SetupInProgressTubeInShuttle();
        SetupDrawerWithNoFreePositions(DrawerType.Sample1);
        SetupDrawerSettings(leftAsInput: true);

        var query = CreateQueryWithTubeId(EnumSampleArea.Shuttle1);

        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        result.WantedDestination.Should().NotBeNull();
        result.WantedDestination.Area.Should().Be(EnumSampleMoveArea.NoMove);
        _mediatorMock.Verify(x => x.Send(It.Is<TaskCardCommand>(cmd =>
            cmd.CardType == TaskCardType.LoadEmptyLeftSampleTray &&
            cmd.InstrumentNumber == _instrumentNumber),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ManualEntryTubeInDrawer_DuringAssay_HasFreeInternalStorage_ShouldMoveToInternalStorage()
    {
        SetupManualEntryTubeInDrawer();
        SetupInstrumentInAssayMode(isInAssay: true);
        SetupDrawerSettings(leftAsInput: false);

        var query = CreateQueryWithTubeId(EnumSampleArea.SampleDrawer1);

        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        result.WantedDestination.Should().NotBeNull();
        result.WantedDestination.Area.Should().Be(EnumSampleMoveArea.MoveInternalStorage);
    }

    [Fact]
    public async Task Handle_ManualEntryTubeInDrawer_DuringAssay_NoFreeInternalStorage_ShouldMoveToDrawer()
    {
        SetupInternalStorageAtCapacity();
        SetupManualEntryTubeInDrawer();
        SetupInstrumentInAssayMode(isInAssay: true);
        SetupDrawerWithFreePosition(DrawerType.Sample1);
        SetupDrawerSettings(leftAsInput: false);

        var query = CreateQueryWithTubeId(EnumSampleArea.SampleDrawer1);

        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        result.WantedDestination.Should().NotBeNull();
        result.WantedDestination.Area.Should().Be(EnumSampleMoveArea.MoveDrawer1);
        result.WantedDestination.TubePosition.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ManualEntryTubeInDrawer_NotDuringAssay_ShouldMoveToDrawerFirstPosition()
    {
        SetupManualEntryTubeInDrawer();
        SetupInstrumentInAssayMode(isInAssay: false);
        SetupDrawerWithFreePosition(DrawerType.Sample1);
        SetupDrawerSettings(leftAsInput: false);

        var query = CreateQueryWithTubeId(EnumSampleArea.SampleDrawer1);

        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        result.WantedDestination.Should().NotBeNull();
        result.WantedDestination.Area.Should().Be(EnumSampleMoveArea.MoveDrawer1);
        result.WantedDestination.TubePosition.Should().Be(1);
    }

    [Fact]
    public async Task Handle_35ManualEntryTubesInInternalStorage_36thSampleTube_ShouldMoveToDrawer()
    {
        Setup35ManualEntryTubesInInternalStorage();
        SetupManualEntryTubeInDrawer();
        SetupInstrumentInAssayMode(isInAssay: true);
        SetupDrawerWithFreePosition(DrawerType.Sample1);
        SetupDrawerSettings(leftAsInput: false);

        var query = CreateQueryWithTubeId(EnumSampleArea.SampleDrawer1);

        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        result.WantedDestination.Should().NotBeNull();
        result.WantedDestination.Area.Should().Be(EnumSampleMoveArea.MoveDrawer1);
        result.WantedDestination.TubePosition.Should().Be(1);
    }

    [Fact]
    public async Task Handle_35ManualEntryTubesInInternalStorage_36thQcTube_ShouldMoveToInternalStorage()
    {
        Setup35ManualEntryTubesInInternalStorage();
        SetupManualEntryQcTubeInDrawer();
        SetupInstrumentInAssayMode(isInAssay: true);
        SetupDrawerSettings(leftAsInput: false);

        var query = CreateQueryWithTubeId(EnumSampleArea.SampleDrawer1);

        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        result.WantedDestination.Should().NotBeNull();
        result.WantedDestination.Area.Should().Be(EnumSampleMoveArea.MoveInternalStorage);
    }

    [Fact]
    public async Task Handle_LeftDrawerAsInputRightAsOutput_UnloadRequestedInProgressTube_ShouldMoveToDrawer2LastPosition()
    {
        SetupInProgressTubeInShuttle();
        _sampleStorage.GetTube(_tubeId)!.UnloadRequested = true;
        SetupInstrumentInAssayMode(isInAssay: true);
        SetupDrawerWithFreePosition(DrawerType.Sample1);
        SetupDrawerWithFreePosition(DrawerType.Sample2);
        SetupDrawerSettings(leftAsInput: true);

        var query = CreateQueryWithTubeId(EnumSampleArea.Shuttle1);

        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        result.WantedDestination.Should().NotBeNull();
        result.WantedDestination.Area.Should().Be(EnumSampleMoveArea.MoveDrawer2);
        result.WantedDestination.TrayPosition.Should().Be(2);
        result.WantedDestination.TubePosition.Should().Be(Constants.MaxSampleTubesInTray);
        _mediatorMock.Verify(x => x.Send(It.IsAny<TaskCardCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_BothDrawersAsInputOutput_UnloadRequestedInProgressTube_ShouldUseLastPosition()
    {
        SetupInProgressTubeInShuttle();
        _sampleStorage.GetTube(_tubeId)!.UnloadRequested = true;
        SetupInstrumentInAssayMode(isInAssay: true);
        SetupDrawerWithFreePosition(DrawerType.Sample1);
        SetupDrawerWithFreePosition(DrawerType.Sample2);
        SetupDrawerSettings(leftAsInput: false);

        var query = CreateQueryWithTubeId(EnumSampleArea.Shuttle1);

        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        result.WantedDestination.Should().NotBeNull();
        result.WantedDestination.Area.Should().Be(EnumSampleMoveArea.MoveDrawer2);
        result.WantedDestination.TrayPosition.Should().Be(2);
        result.WantedDestination.TubePosition.Should().Be(Constants.MaxSampleTubesInTray);
        _mediatorMock.Verify(x => x.Send(It.IsAny<TaskCardCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_LeftDrawerAsInputRightAsOutput_CompletedManualEntryTube_ShouldMoveToDrawer2LastPosition()
    {
        SetupCompletedTubeInShuttle();
        _sampleStorage.GetTube(_tubeId)!.IdentitySource = SampleTubeIdentitySource.ManualInput;
        SetupInstrumentInAssayMode(isInAssay: true);
        SetupDrawerWithFreePosition(DrawerType.Sample1);
        SetupDrawerWithFreePosition(DrawerType.Sample2);
        SetupDrawerSettings(leftAsInput: true);

        var query = CreateQueryWithTubeId(EnumSampleArea.Shuttle1);

        var result = await _sut.Handle(query, TestContext.Current.CancellationToken);

        result.WantedDestination.Should().NotBeNull();
        result.WantedDestination.Area.Should().Be(EnumSampleMoveArea.MoveDrawer2);
        result.WantedDestination.TrayPosition.Should().Be(2);
        result.WantedDestination.TubePosition.Should().Be(Constants.MaxSampleTubesInTray);
        _mediatorMock.Verify(x => x.Send(It.IsAny<TaskCardCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static GetSampleQcDestinationQuery CreateQuery(EnumSampleArea area)
    {
        return new GetSampleQcDestinationQuery(
            _instrumentNumber,
            new SampleQcDestinationRequest
            {
                TubeId = "",
                CurrentPosition = new SampleTubePosition { Area = area, TubePosition = 1 }
            }
        );
    }

    private static GetSampleQcDestinationQuery CreateQueryWithTubeId(EnumSampleArea area)
    {
        return new GetSampleQcDestinationQuery(
            _instrumentNumber,
            new SampleQcDestinationRequest
            {
                TubeId = _tubeId,
                CurrentPosition = new SampleTubePosition { Area = area, TubePosition = 1 }
            }
        );
    }

    private static GetSampleQcDestinationQuery CreateQueryWithExternalTubeId(EnumSampleArea area)
    {
        return new GetSampleQcDestinationQuery(
            _instrumentNumber,
            new SampleQcDestinationRequest
            {
                TubeId = _externalTubeId,
                CurrentPosition = new SampleTubePosition { Area = area, TubePosition = 1 }
            }
        );
    }

    private void SetupDrawerSettings(bool leftAsInput)
    {
        var usageMode = leftAsInput ? DrawerMode.LeftDrawerAsInputAndRightDrawerAsOutput : DrawerMode.BothDrawersAsInputAndOutput;

        var drawerSettings = new Drawer
        {
            DrawerMode = usageMode
        };

        var samplesSettings = new SamplesSettings { Drawer = drawerSettings };
        _samplesSettingsMock.Setup(x => x.Value).Returns(samplesSettings);
    }

    private void SetupDrawerWithFreePosition(DrawerType drawerType)
    {
        var trays = new List<SampleTray>
        {
            new() { Id = "3333", TrayPosition = 1 },
            new() { Id = "4444", TrayPosition = 2 }
        };

        _sampleStorage.UpdateDrawer(_instrumentNumber, new SampleDrawer
        {
            DrawerType = drawerType,
            Trays = trays
        });
    }

    private void SetupDrawerWithNoFreePositions(DrawerType drawerType)
    {
        var drawer = drawerType == DrawerType.Sample1 ? SampleArea.SampleDrawer1 : SampleArea.SampleDrawer2;
        var trayId = drawerType == DrawerType.Sample1 ? "1111" : "2222";

        var trays = new List<SampleTray>
        {
            new() { Id = trayId, TrayPosition = 1 }
        };

        _sampleStorage.UpdateDrawer(_instrumentNumber, new SampleDrawer
        {
            DrawerType = drawerType,
            Trays = trays
        });

        for (var i = 0; i < Constants.MaxSampleTubesInTray; i++)
        {
            var tube = new SampleTube
            {
                TubeId = $"TUBE_{trayId}_{i}",
                InstrumentNumber = _instrumentNumber,
                Area = drawer,
                TrayPosition = 1,
                TubePosition = i + 1
            };

            _sampleStorage.AddOrUpdateTube(tube, _ => tube);
        }
    }

    private void SetupInProgressTubeInShuttle()
    {
        var tube = new SampleTube
        {
            TubeId = _tubeId,
            InstrumentNumber = _instrumentNumber,
            Area = SampleArea.Shuttle1,
            Status = SampleTubeStatus.InProgress,
            InstrumentError = SampleTubeError.None,
            InternalError = SampleTubeError.None,
            TubePosition = 1,
            IdentitySource = SampleTubeIdentitySource.BarcodeScanning,
            SampleType = SampleType.Sample
        };
        _sampleStorage.AddOrUpdateTube(tube, _ => tube);
    }

    private void SetupExternalSourceTubeInShuttle()
    {
        var tube = new SampleTube
        {
            TubeId = _externalTubeId,
            InstrumentNumber = _instrumentNumber,
            Area = SampleArea.Shuttle1,
            Status = SampleTubeStatus.InProgress,
            InstrumentError = SampleTubeError.None,
            InternalError = SampleTubeError.None,
            TubePosition = 1,
            IdentitySource = SampleTubeIdentitySource.External,
            OriginalPosition = new OriginalPosition
            {
                Area = SampleArea.SampleDrawer1,
                TrayPosition = 1,
                TubePosition = 3
            }
        };
        _sampleStorage.AddOrUpdateTube(tube, _ => tube);
    }

    private void SetupExternalSourceTubeReservingPosition(int trayPosition, int tubePosition)
    {
        var tube = new SampleTube
        {
            TubeId = "RESERVED_TUBE",
            InstrumentNumber = _instrumentNumber,
            Area = SampleArea.InternalStorage,
            Status = SampleTubeStatus.InProgress,
            InstrumentError = SampleTubeError.None,
            InternalError = SampleTubeError.None,
            TubePosition = 1,
            IdentitySource = SampleTubeIdentitySource.External,
            OriginalPosition = new OriginalPosition
            {
                Area = SampleArea.SampleDrawer1,
                TrayPosition = trayPosition,
                TubePosition = tubePosition
            }
        };
        _sampleStorage.AddOrUpdateTube(tube, _ => tube);
    }

    private void SetupFullTrayWithReservedPositions(DrawerType drawerType, int trayPosition)
    {
        var area = drawerType == DrawerType.Sample1 ? SampleArea.SampleDrawer1 : SampleArea.SampleDrawer2;
        for (var i = 1; i <= Constants.MaxSampleTubesInTray; i++)
        {
            var tube = new SampleTube
            {
                TubeId = $"RESERVED_TUBE_{i}",
                InstrumentNumber = _instrumentNumber,
                Area = area,
                Status = SampleTubeStatus.InProgress,
                InstrumentError = SampleTubeError.None,
                InternalError = SampleTubeError.None,
                TubePosition = i,
                TrayPosition = trayPosition,
                IdentitySource = SampleTubeIdentitySource.External,
                OriginalPosition = new OriginalPosition
                {
                    Area = SampleArea.SampleDrawer1,
                    TrayPosition = trayPosition,
                    TubePosition = i
                }
            };
            _sampleStorage.AddOrUpdateTube(tube, _ => tube);
        }
    }

    private void SetupErrorTubeInShuttle()
    {
        var tube = new SampleTube
        {
            TubeId = _tubeId,
            InstrumentNumber = _instrumentNumber,
            Area = SampleArea.Shuttle1,
            Status = SampleTubeStatus.InProgress,
            InstrumentError = SampleTubeError.SampleShortage,
            InternalError = SampleTubeError.None,
            TubePosition = 1
        };
        _sampleStorage.AddOrUpdateTube(tube, _ => tube);
    }

    private void SetupCompletedTubeInShuttle()
    {
        var tube = new SampleTube
        {
            TubeId = _tubeId,
            InstrumentNumber = _instrumentNumber,
            Area = SampleArea.Shuttle1,
            Status = SampleTubeStatus.Completed,
            InstrumentError = SampleTubeError.None,
            InternalError = SampleTubeError.None,
            TubePosition = 1
        };
        _sampleStorage.AddOrUpdateTube(tube, _ => tube);
    }

    private void SetupInternalStorageAtCapacity()
    {
        for (var i = 0; i < Constants.SamplesInternalStorageMaxCap; i++)
        {
            var tube = new SampleTube
            {
                TubeId = $"TUBE_{i}",
                InstrumentNumber = _instrumentNumber,
                Area = SampleArea.InternalStorage,
                TubePosition = i + 1,
                SampleType = SampleType.Sample
            };
            _sampleStorage.AddOrUpdateTube(tube, _ => tube);
        }
    }

    private void SetupManualEntryTubeInDrawer()
    {
        var tube = new SampleTube
        {
            TubeId = _tubeId,
            InstrumentNumber = _instrumentNumber,
            Area = SampleArea.SampleDrawer1,
            Status = SampleTubeStatus.InProgress,
            InstrumentError = SampleTubeError.None,
            InternalError = SampleTubeError.None,
            TrayPosition = 1,
            TubePosition = 1,
            IdentitySource = SampleTubeIdentitySource.ManualInput,
            SampleType = SampleType.Sample
        };
        _sampleStorage.AddOrUpdateTube(tube, _ => tube);
    }

    private void SetupManualEntryQcTubeInDrawer()
    {
        var tube = new SampleTube
        {
            TubeId = _tubeId,
            InstrumentNumber = _instrumentNumber,
            Area = SampleArea.SampleDrawer1,
            Status = SampleTubeStatus.InProgress,
            InstrumentError = SampleTubeError.None,
            InternalError = SampleTubeError.None,
            TrayPosition = 1,
            TubePosition = 1,
            IdentitySource = SampleTubeIdentitySource.ManualInput,
            SampleType = SampleType.QualityControl
        };
        _sampleStorage.AddOrUpdateTube(tube, _ => tube);
    }

    private void Setup35ManualEntryTubesInInternalStorage()
    {
        for (var i = 0; i < 35; i++)
        {
            var tube = new SampleTube
            {
                TubeId = $"MANUAL_TUBE_{i}",
                InstrumentNumber = _instrumentNumber,
                Area = SampleArea.InternalStorage,
                TubePosition = i + 1,
                IdentitySource = SampleTubeIdentitySource.ManualInput,
                SampleType = SampleType.Sample,
                Status = SampleTubeStatus.InProgress,
                InstrumentError = SampleTubeError.None,
                InternalError = SampleTubeError.None
            };
            _sampleStorage.AddOrUpdateTube(tube, _ => tube);
        }
    }

    private void SetupInstrumentInAssayMode(bool isInAssay)
    {
        _instrumentMock.Setup(instrument => instrument.State)
            .Returns(isInAssay
                ? Pharos.Server.Domain.Enums.InstrumentState.Assay
                : Pharos.Server.Domain.Enums.InstrumentState.Standby);
    }
}
