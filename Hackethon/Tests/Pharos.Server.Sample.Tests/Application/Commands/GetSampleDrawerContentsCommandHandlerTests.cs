using MediatR;
using Microsoft.Extensions.Options;
using Moq.EntityFrameworkCore;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Request;
using Pharos.Server.Application.Sample;
using Pharos.Server.Application.Settings.Sample;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Infrastructure;
using Pharos.Server.Sample.Application.Commands.GetSampleDrawerContent;
using Pharos.Server.Sample.Application.Interfaces;
using Pharos.Server.Sample.Application.Services;
using Pharos.Server.Sample.Domain;
using Tetris.Protocol.Common;
using Tetris.Protocol.Pharos.Tray;

namespace Pharos.Server.Sample.Tests.Application.Commands;

public class GetSampleDrawerContentsCommandHandlerTests
{
    private readonly Mock<ISampleDbContext> _dbContextMock;
    private readonly InstrumentSampleStorage _sampleStorage = new();
    private readonly Mock<IOptionsSnapshot<SamplesSettings>> _samplesSettingsMock;
    private readonly Mock<ISender> _senderMock;
    private readonly GetSampleDrawerContentsCommandHandler _sut;

    public GetSampleDrawerContentsCommandHandlerTests()
    {
        _dbContextMock = new Mock<ISampleDbContext>();
        Mock<IDateTime> dateTimeMock = new();
        Mock<IInstrumentManager> instrumentServiceMock = new();
        Mock<ICurrentInstrumentServerHub> currentInstrumentServerHubMock = new();
        _senderMock = new Mock<ISender>();
        var publisherMock = new Mock<IPublisher>();
        _samplesSettingsMock = new Mock<IOptionsSnapshot<SamplesSettings>>();
        _sut = new GetSampleDrawerContentsCommandHandler(
            _sampleStorage,
            _dbContextMock.Object,
            dateTimeMock.Object,
            instrumentServiceMock.Object,
            currentInstrumentServerHubMock.Object,
            _samplesSettingsMock.Object,
             _senderMock.Object,
            publisherMock.Object
        );
    }

    [Fact]
    public async Task HandleLoadEmptyTrayTaskCard_ShouldRemoveTaskCard_WhenTraysHaveFreePositions()
    {
        var instrumentNumber = "Instrument1";
        var trayBarcode = "1234";

        var validTrays = new List<DetectedSampleTray>
        {
            new()
            {
                TrayBarcode = trayBarcode,
                TrayPosition = 1,
                DetectedTubes = {
                    new DetectedSampleTube { SamplePosition = 1 },
                    new DetectedSampleTube { SamplePosition = 2 }
                }
            }
        };

        _dbContextMock.Setup(x => x.SampleTrays).ReturnsDbSet(new List<SampleTray>());
        _samplesSettingsMock.Setup(x => x.Value).Returns(new SamplesSettings());

        var message = new SampleDrawerContentsRequest
        {
            Drawer = EnumSampleDrawer.Drawer1,
            SampleTrays = { validTrays }
        };

        var request = new GetSampleDrawerContentsCommand(instrumentNumber, message);

        await _sut.Handle(request, TestContext.Current.CancellationToken);

        _senderMock.Verify(m => m.Send(
            It.Is<RemoveTaskCardCommand>(cmd =>
                cmd.CardType == TaskCardType.LoadEmptySampleTray &&
                cmd.InstrumentNumber == instrumentNumber),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleLoadEmptyTrayTaskCard_ShouldNotRemoveTaskCard_WhenTraysAreFull()
    {
        var instrumentNumber = "Instrument1";
        var trayBarcode = "1234";

        var detectedTubes = new List<DetectedSampleTube>();
        for (var i = 1; i <= Constants.MaxSampleTubesInTray; i++)
        {
            detectedTubes.Add(new DetectedSampleTube { SamplePosition = i });
        }

        var validTrays = new List<DetectedSampleTray>
        {
            new()
            {
                TrayBarcode = trayBarcode,
                TrayPosition = 1,
                DetectedTubes = { detectedTubes }
            }
        };

        _dbContextMock.Setup(x => x.SampleTrays).ReturnsDbSet(new List<SampleTray>());
        _samplesSettingsMock.Setup(x => x.Value).Returns(new SamplesSettings());

        var message = new SampleDrawerContentsRequest
        {
            Drawer = EnumSampleDrawer.Drawer1,
            SampleTrays = { validTrays }
        };

        var request = new GetSampleDrawerContentsCommand(instrumentNumber, message);

        await _sut.Handle(request, TestContext.Current.CancellationToken);

        _senderMock.Verify(m => m.Send(
            It.Is<RemoveTaskCardCommand>(cmd =>
                cmd.CardType == TaskCardType.LoadEmptySampleTray &&
                cmd.InstrumentNumber == instrumentNumber),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Trait("ProductRequirement", "PR_304")]
    [Trait("ModuleRequirement", "MR_5374")]
    [Fact]
    public async Task HandleLoadEmptyTrayTaskCard_ShouldRemoveTaskCard_WhenCurrentDrawerIsFullButOtherDrawerHasFreePositions()
    {
        var instrumentNumber = "Instrument1";
        var trayBarcode = "1234";

        var detectedTubes = new List<DetectedSampleTube>();
        for (var i = 1; i <= Constants.MaxSampleTubesInTray; i++)
        {
            detectedTubes.Add(new DetectedSampleTube { SamplePosition = i });
        }

        var validTrays = new List<DetectedSampleTray>
        {
            new()
            {
                TrayBarcode = trayBarcode,
                TrayPosition = 1,
                DetectedTubes = { detectedTubes }
            }
        };

        _sampleStorage.UpdateDrawer(instrumentNumber, new SampleDrawer
        {
            DrawerType = DrawerType.Sample2,
            Trays = [new SampleTray
            {
                Id = "2222",
                Drawer = DrawerType.Sample2,
                Error = SampleTrayError.None,
                SampleTubes = new List<SampleTube>
                {
                    new() { TubePosition = 1 },
                    new() { TubePosition = 2 }
                }
            }]
        });


        _dbContextMock.Setup(x => x.SampleTrays).ReturnsDbSet(new List<SampleTray>());
        _samplesSettingsMock.Setup(x => x.Value).Returns(new SamplesSettings());

        var message = new SampleDrawerContentsRequest
        {
            Drawer = EnumSampleDrawer.Drawer1,
            SampleTrays = { validTrays }
        };

        var request = new GetSampleDrawerContentsCommand(instrumentNumber, message);

        await _sut.Handle(request, TestContext.Current.CancellationToken);

        _senderMock.Verify(m => m.Send(
            It.Is<RemoveTaskCardCommand>(cmd =>
                cmd.CardType == TaskCardType.LoadEmptySampleTray &&
                cmd.InstrumentNumber == instrumentNumber),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Trait("ProductRequirement", "PR_304")]
    [Trait("ModuleRequirement", "MR_5374")]
    [Fact]
    public async Task HandleLoadEmptyTrayTaskCard_ShouldConsiderReservedPositions_WhenCalculatingFreePositions()
    {
        var instrumentNumber = "Instrument1";
        var trayBarcode = "1234";

        var validTrays = new List<DetectedSampleTray>
        {
            new()
            {
                TrayBarcode = trayBarcode,
                TrayPosition = 1,
                DetectedTubes = {
                    new DetectedSampleTube { SamplePosition = 1 },
                    new DetectedSampleTube { SamplePosition = 2 }
                }
            }
        };

        for (var i = 3; i <= Constants.MaxSampleTubesInTray; i++)
        {
            var tube = new SampleTube
            {
                InstrumentNumber = instrumentNumber,
                TubeId = $"EXT_TUBE_{i}",
                IdentitySource = SampleTubeIdentitySource.External,
                Area = SampleArea.InternalStorage,
                TubePosition = 1,
                OriginalPosition = new OriginalPosition
                {
                    TrayId = trayBarcode,
                    TrayPosition = 1,
                    TubePosition = i
                }
            };
            _sampleStorage.AddOrUpdateTube(tube, _ => tube);
        }

        _dbContextMock.Setup(x => x.SampleTrays).ReturnsDbSet(new List<SampleTray>());
        _samplesSettingsMock.Setup(x => x.Value).Returns(new SamplesSettings());

        var message = new SampleDrawerContentsRequest
        {
            Drawer = EnumSampleDrawer.Drawer1,
            SampleTrays = { validTrays }
        };

        var request = new GetSampleDrawerContentsCommand(instrumentNumber, message);

        await _sut.Handle(request, TestContext.Current.CancellationToken);

        _senderMock.Verify(m => m.Send(
            It.Is<RemoveTaskCardCommand>(cmd =>
                cmd.CardType == TaskCardType.LoadEmptySampleTray &&
                cmd.InstrumentNumber == instrumentNumber),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Trait("ProductRequirement", "PR_304")]
    [Trait("ModuleRequirement", "MR_4398")]
    [Fact]
    public async Task GetSampleDrawerContents_ForInvalidTray_Should_SetSampleTrayErrorInvalidBarcode()
    {
        var instrumentNumber = "Instrument1";
        var message = new SampleDrawerContentsRequest
        {
            Drawer = EnumSampleDrawer.Drawer1,
            SampleTrays = {
                new DetectedSampleTray {
                    SampleTrayError = EnumSampleTrayError.SampleTrayErrorUnreadableBarcode,
                    TrayPosition = 1
                }
            }
        };

        _dbContextMock.Setup(x => x.SampleTrays).ReturnsDbSet(new List<SampleTray>());
        _samplesSettingsMock.Setup(x => x.Value).Returns(new SamplesSettings());

        var request = new GetSampleDrawerContentsCommand(instrumentNumber, message);

        var result = await _sut.Handle(request, TestContext.Current.CancellationToken);

        result.SampleTrays.Count.Should().Be(1);
        result.SampleTrays[0].SampleTrayError.Should().Be(EnumSampleTrayError.SampleTrayErrorUnreadableBarcode);
    }

    [Fact]
    public async Task GetSampleDrawerContents_ForRemovedTray_should_UpdateDrawerAndInstrument()
    {
        var instrumentNumber = "Instrument1";
        var message = new SampleDrawerContentsRequest
        {
            Drawer = EnumSampleDrawer.Drawer1
        };

        var dbTrays = GetSampleTrays();
        _dbContextMock.Setup(x => x.SampleTrays).ReturnsDbSet(dbTrays);

        _samplesSettingsMock.Setup(x => x.Value).Returns(new SamplesSettings());

        var request = new GetSampleDrawerContentsCommand(instrumentNumber, message);

        var result = await _sut.Handle(request, TestContext.Current.CancellationToken);

        result.Should().NotBeNull().And.BeOfType<SampleDrawerContentsResponse>();
        Assert.True(dbTrays.All(x => x.InstrumentNumber == null && x.Drawer == null));
    }

    [Fact]
    public async Task UpdateStorage_WhenPredefinedPosition_Should_OnlyHaveOneTubeEntry()
    {
        // Arrange
        var instrumentNumber = "Instrument1";
        var trayBarcode = "9999";
        var trayPosition = 3;
        var tubePosition = 4;
        var area = SampleArea.SampleDrawer2;
        var manualTubeId = "MANUAL_TUBE_ID_2";
        var manualSampleId = "MANUAL_SAMPLE_ID_2";

        // Simulate manual input tube (predefined)
        var manualInputTube = new SampleTube
        {
            InstrumentNumber = instrumentNumber,
            Area = area,
            TrayPosition = trayPosition,
            TubePosition = tubePosition,
            TubeId = manualTubeId,
            SampleId = manualSampleId,
            IdentitySource = SampleTubeIdentitySource.ManualInput
        };

        // Setup tray and context
        var tray = new SampleTray
        {
            Id = trayBarcode,
            TrayPosition = trayPosition,
            Drawer = DrawerType.Sample2,
            SampleTubes = new List<SampleTube> { manualInputTube }
        };
        _dbContextMock.Setup(x => x.SampleTrays).ReturnsDbSet(new List<SampleTray> { tray });
        _samplesSettingsMock.Setup(x => x.Value).Returns(new SamplesSettings());

        // Setup detected tray in request
        var detectedTray = new DetectedSampleTray
        {
            TrayBarcode = trayBarcode,
            TrayPosition = trayPosition,
            DetectedTubes = { new DetectedSampleTube { SamplePosition = tubePosition } }
        };
        var message = new SampleDrawerContentsRequest
        {
            Drawer = EnumSampleDrawer.Drawer2,
            SampleTrays = { detectedTray }
        };
        var request = new GetSampleDrawerContentsCommand(instrumentNumber, message);

        // Act
        await _sut.Handle(request, TestContext.Current.CancellationToken);

        // Assert
        // Verify that the storage was added with the manual tube
        _sampleStorage.GetTube(manualTubeId).Should().NotBeNull();
    }

    [Trait("ProductRequirement", "PR_304")]
    [Trait("ModuleRequirement", "MR_5374")]
    [Fact]
    public async Task Handle_LeftDrawerAsInputRightAsOutput_LeftDrawerHasFreePositions_ShouldRemoveLeftDrawerTaskCard()
    {
        var instrumentNumber = "Instrument1";
        var trayBarcode = "1234";

        var settings = new SamplesSettings
        {
            Drawer = new Drawer
            {
                DrawerMode = DrawerMode.LeftDrawerAsInputAndRightDrawerAsOutput
            }
        };

        var externalTube = new SampleTube
        {
            InstrumentNumber = instrumentNumber,
            IdentitySource = SampleTubeIdentitySource.External,
            Area = SampleArea.InternalStorage,
            TubePosition = 1,
            OriginalPosition = new OriginalPosition
            {
                TrayId = trayBarcode,
                TrayPosition = 1,
                TubePosition = 2
            }
        };
        _sampleStorage.AddOrUpdateTube(externalTube, _ => externalTube);

        _dbContextMock.Setup(x => x.SampleTrays).ReturnsDbSet(new List<SampleTray>());
        _samplesSettingsMock.Setup(x => x.Value).Returns(settings);

        var trayWithFreePositions = new DetectedSampleTray
        {
            TrayBarcode = trayBarcode,
            DetectedTubes = { new DetectedSampleTube { SamplePosition = 1 } }
        };

        var command = new GetSampleDrawerContentsCommand(instrumentNumber, new SampleDrawerContentsRequest
        {
            Drawer = EnumSampleDrawer.Drawer1,
            SampleTrays = { trayWithFreePositions }
        });

        await _sut.Handle(command, TestContext.Current.CancellationToken);

        _senderMock.Verify(m => m.Send(
            It.Is<RemoveTaskCardCommand>(cmd =>
                cmd.CardType == TaskCardType.LoadEmptyLeftSampleTray &&
                cmd.InstrumentNumber == instrumentNumber),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateStorage_WhenPredefinedPositionForAlreadyDetectedTubePosition_Should_OnlyHaveOneTubeEntry()
    {
        // Arrange
        var instrumentNumber = "Instrument1";
        var trayBarcode = "5432";
        var trayPosition = 1;
        var tubePosition = 2;
        var area = SampleArea.SampleDrawer2;
        var manualTubeId = "MANUAL_TUBE_ID";
        var manualSampleId = "MANUAL_SAMPLE_ID";

        // Simulate detected tube already in storage
        var tube = new SampleTube
        {
            InstrumentNumber = instrumentNumber,
            Area = area,
            TrayPosition = trayPosition,
            TubePosition = tubePosition,
        };
        _sampleStorage.AddOrUpdateTube(tube, _ => tube);

        // Simulate manual input tube (predefined)
        var manualInputTube = new SampleTube
        {
            InstrumentNumber = instrumentNumber,
            Area = area,
            TrayPosition = trayPosition,
            TubePosition = tubePosition,
            TubeId = manualTubeId,
            SampleId = manualSampleId,
            IdentitySource = SampleTubeIdentitySource.ManualInput
        };

        // Setup tray and context
        var tray = new SampleTray
        {
            Id = trayBarcode,
            TrayPosition = trayPosition,
            SampleTubes = new List<SampleTube> { manualInputTube }
        };
        _dbContextMock.Setup(x => x.SampleTrays).ReturnsDbSet(new List<SampleTray> { tray });
        _sampleStorage.UpdateDrawer(instrumentNumber, new SampleDrawer
        {
            DrawerType = DrawerType.Sample2,
            Trays = [tray]
        });
        _samplesSettingsMock.Setup(x => x.Value).Returns(new SamplesSettings());

        // Setup detected tray in request
        var detectedTray = new DetectedSampleTray
        {
            TrayBarcode = trayBarcode,
            TrayPosition = trayPosition,
            DetectedTubes = { new DetectedSampleTube { SamplePosition = tubePosition } }
        };
        var message = new SampleDrawerContentsRequest
        {
            Drawer = EnumSampleDrawer.Drawer2,
            SampleTrays = { detectedTray }
        };
        var request = new GetSampleDrawerContentsCommand(instrumentNumber, message);

        // Act
        await _sut.Handle(request, TestContext.Current.CancellationToken);

        // Assert
        // Verify that the storage was updated manual tube in place of the detected tube
        var tubes = _sampleStorage.TubesAsQueryable(instrumentNumber).ToList();
        tubes.Should().HaveCount(1);
        tubes[0].TubeId.Should().Be(manualTubeId);
    }

    [Fact]
    public async Task Handle_ShouldUnmapConfirmManualInputTube_WhenRequestExists()
    {
        var instrumentNumber = "Instrument1";
        var trayBarcode = "0001";
        var trayPosition = 1;
        var requestCreatedDate = new DateTimeOffset(new DateTime(2024, 1, 1), TimeSpan.Zero);

        var confirmManualTube = new SampleTube
        {
            TubeId = "Tube-1",
            SampleId = "Sample-1",
            IdentitySource = SampleTubeIdentitySource.ConfirmManualInput,
            TrayId = trayBarcode,
            TubePosition = 5,
            InstrumentNumber = instrumentNumber,
            RequestCreatedDate = requestCreatedDate
        };

        var trayEntity = new SampleTray
        {
            Id = trayBarcode,
            TrayPosition = trayPosition,
            Drawer = DrawerType.Sample1,
            InstrumentNumber = instrumentNumber,
            SampleTubes = new List<SampleTube> { confirmManualTube }
        };

        var sampleTrays = new List<SampleTray> { trayEntity };
        var sampleTubes = new List<SampleTube> { confirmManualTube };

        _dbContextMock.Setup(x => x.SampleTrays).ReturnsDbSet(sampleTrays);
        _dbContextMock.Setup(x => x.SampleTubes).ReturnsDbSet(sampleTubes);
        _dbContextMock.Setup(x => x.HasChanges()).Returns(false);

        _samplesSettingsMock.Setup(x => x.Value).Returns(new SamplesSettings());

        _senderMock
            .Setup(x => x.Send(
                It.Is<GetRequestIdQuery>(query => query.SampleId == confirmManualTube.SampleId && query.RequestCreatedDate == requestCreatedDate),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RequestIdResponseDto(1, requestCreatedDate, true));

        var detectedTray = new DetectedSampleTray
        {
            TrayBarcode = trayBarcode,
            TrayPosition = trayPosition
        };

        var message = new SampleDrawerContentsRequest
        {
            Drawer = EnumSampleDrawer.Drawer1,
            SampleTrays = { detectedTray }
        };

        var request = new GetSampleDrawerContentsCommand(instrumentNumber, message);

        await _sut.Handle(request, TestContext.Current.CancellationToken);

        confirmManualTube.TrayId.Should().BeNull();
        confirmManualTube.TubePosition.Should().BeNull();
        confirmManualTube.InstrumentNumber.Should().BeNull();
        trayEntity.SampleTubes.Should().Contain(confirmManualTube);
        _dbContextMock.Verify(x => x.SampleTubes.Remove(It.IsAny<SampleTube>()), Times.Never);
        _senderMock.Verify(x => x.Send(It.IsAny<GetRequestIdQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldRemoveConfirmManualInputTube_WhenRequestDoesNotExist()
    {
        var instrumentNumber = "Instrument1";
        var trayBarcode = "0002";
        var trayPosition = 2;
        var requestCreatedDate = new DateTimeOffset(new DateTime(2024, 2, 1), TimeSpan.Zero);

        var confirmManualTube = new SampleTube
        {
            TubeId = "Tube-2",
            SampleId = "Sample-2",
            IdentitySource = SampleTubeIdentitySource.ConfirmManualInput,
            TrayId = trayBarcode,
            TubePosition = 6,
            InstrumentNumber = instrumentNumber,
            RequestCreatedDate = requestCreatedDate
        };

        var trayEntity = new SampleTray
        {
            Id = trayBarcode,
            TrayPosition = trayPosition,
            Drawer = DrawerType.Sample1,
            InstrumentNumber = instrumentNumber,
            SampleTubes = new List<SampleTube> { confirmManualTube }
        };

        var sampleTrays = new List<SampleTray> { trayEntity };
        var sampleTubes = new List<SampleTube> { confirmManualTube };

        _dbContextMock.Setup(x => x.SampleTrays).ReturnsDbSet(sampleTrays);
        _dbContextMock.Setup(x => x.SampleTubes).ReturnsDbSet(sampleTubes);
        _dbContextMock.Setup(x => x.HasChanges()).Returns(false);

        _samplesSettingsMock.Setup(x => x.Value).Returns(new SamplesSettings());

        _senderMock
            .Setup(x => x.Send(
                It.Is<GetRequestIdQuery>(query => query.SampleId == confirmManualTube.SampleId && query.RequestCreatedDate == requestCreatedDate),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RequestIdResponseDto(-1, requestCreatedDate, false));

        var detectedTray = new DetectedSampleTray
        {
            TrayBarcode = trayBarcode,
            TrayPosition = trayPosition
        };

        var message = new SampleDrawerContentsRequest
        {
            Drawer = EnumSampleDrawer.Drawer1,
            SampleTrays = { detectedTray }
        };

        var request = new GetSampleDrawerContentsCommand(instrumentNumber, message);

        await _sut.Handle(request, TestContext.Current.CancellationToken);

        trayEntity.SampleTubes.Should().BeEmpty();
        _dbContextMock.Verify(x => x.SampleTubes.Remove(confirmManualTube), Times.Once);
        _senderMock.Verify(x => x.Send(It.IsAny<GetRequestIdQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static List<SampleTray> GetSampleTrays()
    {
        return
        [
            new SampleTray
            {
                Id = "7565",
                Drawer = DrawerType.Sample1,
                InstrumentNumber = "Instrument1"
            }
        ];
    }
}
