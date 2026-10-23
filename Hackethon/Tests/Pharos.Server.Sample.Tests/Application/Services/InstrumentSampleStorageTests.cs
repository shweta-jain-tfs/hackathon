using Microsoft.Extensions.Options;
using Pharos.Server.Application.Settings.Sample;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Sample.Application.Services;
using Pharos.Server.Sample.Domain;

namespace Pharos.Server.Sample.Tests.Application.Services;

public class InstrumentSampleStorageTests
{
    private const string _instrumentNumber = "INSTR1";
    private const string _tubeId = "TUBE1";
    private const string _sampleId = "SAMPLE1";
    private const int _trayPosition = 1;

    [Fact]
    public void AddOrUpdateTube_AddsTube_WhenNew()
    {
        var storage = new InstrumentSampleStorage();
        var tube = new SampleTube
        {
            TubeId = _tubeId,
            InstrumentNumber = _instrumentNumber,
            TubePosition = 1,
            Area = SampleArea.SampleDrawer1,
            TrayPosition = _trayPosition
        };

        storage.AddOrUpdateTube(tube, _ => tube);

        var result = storage.GetTube(_tubeId);
        result.Should().NotBeNull();
        result.TubeId.Should().Be(_tubeId);
    }

    [Fact]
    public void AddOrUpdateTube_UpdatesTube_WhenExists()
    {
        var storage = new InstrumentSampleStorage();

        var initialTube = new SampleTube
        {
            TubeId = _tubeId,
            InstrumentNumber = _instrumentNumber,
            TubePosition = 1,
            Area = SampleArea.SampleDrawer1,
            TrayPosition = _trayPosition,
            Status = SampleTubeStatus.InProgress
        };
        storage.AddOrUpdateTube(initialTube, _ => initialTube);

        var updatedTube = new SampleTube
        {
            TubeId = _tubeId,
            InstrumentNumber = _instrumentNumber,
            TubePosition = 1,
            Area = SampleArea.SampleDrawer1,
            TrayPosition = _trayPosition,
            Status = SampleTubeStatus.Completed
        };
        storage.AddOrUpdateTube(updatedTube, _ => updatedTube);

        var result = storage.GetTube(_tubeId);
        result.Should().NotBeNull();
        result.Status.Should().Be(SampleTubeStatus.Completed);
    }

    [Fact]
    public void RemoveTube()
    {
        var storage = new InstrumentSampleStorage();

        var tubeToAdd = new SampleTube
        {
            TubeId = _tubeId,
            InstrumentNumber = _instrumentNumber,
            TubePosition = 1,
            Area = SampleArea.SampleDrawer1,
            TrayPosition = _trayPosition
        };
        storage.AddOrUpdateTube(tubeToAdd, _ => tubeToAdd);

        var tubeToRemove = new SampleTube
        {
            TubeId = _tubeId,
            InstrumentNumber = _instrumentNumber,
            TubePosition = 1,
            Area = SampleArea.SampleDrawer1,
            TrayPosition = _trayPosition
        };
        storage.RemoveTube(tubeToRemove);

        var result = storage.GetTube(_tubeId);
        result.Should().BeNull();
    }

    [Fact]
    public void RemoveTube_UnIdentifiedTube()
    {
        var storage = new InstrumentSampleStorage();

        var tubeToAdd = new SampleTube
        {
            TubeId = "",
            InstrumentNumber = _instrumentNumber,
            TubePosition = 1,
            Area = SampleArea.SampleDrawer1,
            TrayPosition = _trayPosition
        };
        storage.AddOrUpdateTube(tubeToAdd, _ => tubeToAdd);

        var tubeToRemove = new SampleTube
        {
            TubeId = "",
            InstrumentNumber = _instrumentNumber,
            TubePosition = 1,
            Area = SampleArea.SampleDrawer1,
            TrayPosition = _trayPosition
        };
        storage.RemoveTube(tubeToRemove);

        var allTubes = storage.TubesAsQueryable(_instrumentNumber).ToList();
        allTubes.Count.Should().Be(0);
    }

    [Fact]
    public void RemoveTube_OnlyIfThePositionIsMatched()
    {
        var storage = new InstrumentSampleStorage();
        var tubeId = "TUBE_ID";

        // Add first tube with shared TubeId in Drawer1
        var tube1 = new SampleTube
        {
            TubeId = tubeId,
            InstrumentNumber = _instrumentNumber,
            TubePosition = 1,
            Area = SampleArea.SampleDrawer1,
            TrayPosition = 1
        };
        storage.AddOrUpdateTube(tube1, _ => tube1);

        var allTubes = storage.TubesAsQueryable(_instrumentNumber).ToList();
        allTubes.Should().Contain(t => t.TubePosition == 1 && t.Area == SampleArea.SampleDrawer1);

        // Remove the specific tube from Drawer2, Tray Position 2, Tube Position 2
        var tubeToRemove = new SampleTube
        {
            TubeId = tubeId,
            InstrumentNumber = _instrumentNumber,
            TubePosition = 2,
            Area = SampleArea.SampleDrawer2,
            TrayPosition = 2
        };
        storage.RemoveTube(tubeToRemove);

        // Verify SampleDrawer1 tube is not removed as position does not match
        var allTubeAfterRemove = storage.TubesAsQueryable(_instrumentNumber).ToList();
        allTubeAfterRemove.Should().Contain(t => t.TubePosition == 1 && t.Area == SampleArea.SampleDrawer1);
    }

    [Fact]
    public void GetDrawer_ReturnsDrawer_WhenExists()
    {
        var storage = new InstrumentSampleStorage();
        var drawer = new SampleDrawer
        {
            DrawerType = DrawerType.Sample1
        };
        storage.UpdateDrawer(_instrumentNumber, drawer);

        var result = storage.GetDrawer(_instrumentNumber, DrawerType.Sample1);
        result.Should().NotBeNull();
        result.DrawerType.Should().Be(DrawerType.Sample1);
    }

    [Fact]
    public void GetDrawer_ReturnsNewDrawer_WhenNotExists()
    {
        var storage = new InstrumentSampleStorage();
        var result = storage.GetDrawer(_instrumentNumber, DrawerType.Sample2);
        result.Should().NotBeNull();
        result.DrawerType.Should().Be(DrawerType.Sample2);
    }

    [Fact]
    public void GetTray_ReturnsTray_WhenExists()
    {
        var storage = new InstrumentSampleStorage();
        var tray = new SampleTray
        {
            Id = "TRAY1",
            TrayPosition = _trayPosition
        };
        var drawer = new SampleDrawer
        {
            DrawerType = DrawerType.Sample1,
            Trays = [tray]
        };
        storage.UpdateDrawer(_instrumentNumber, drawer);

        var result = storage.GetTray("TRAY1");
        result.Should().NotBeNull();
        result.Id.Should().Be("TRAY1");
    }

    [Fact]
    public void GetTray_ReturnsNull_WhenNotExists()
    {
        var storage = new InstrumentSampleStorage();
        var result = storage.GetTray("NON_EXISTENT");
        result.Should().BeNull();
    }

    [Fact]
    public void GetTrays_ReturnsAllTrays()
    {
        var storage = new InstrumentSampleStorage();
        var tray1 = new SampleTray { Id = "TRAY1", TrayPosition = 1 };
        var tray2 = new SampleTray { Id = "TRAY2", TrayPosition = 2 };
        var drawer = new SampleDrawer
        {
            DrawerType = DrawerType.Sample1,
            Trays = [tray1, tray2]
        };
        storage.UpdateDrawer(_instrumentNumber, drawer);

        var trays = storage.GetTrays(_instrumentNumber, DrawerType.Sample1);
        trays.Should().Contain(t => t.Id == "TRAY1");
        trays.Should().Contain(t => t.Id == "TRAY2");
    }

    [Fact]
    public void TubesAsQueryable_ReturnsTubes()
    {
        var storage = new InstrumentSampleStorage();
        var tube = new SampleTube
        {
            TubeId = _tubeId,
            InstrumentNumber = _instrumentNumber,
            TubePosition = 1,
            Area = SampleArea.SampleDrawer1,
            TrayPosition = _trayPosition
        };
        storage.AddOrUpdateTube(tube, _ => tube);

        var tubes = storage.TubesAsQueryable(_instrumentNumber).ToList();
        tubes.Should().ContainSingle()
            .Which.TubeId.Should().Be(_tubeId);
    }

    [Fact]
    public void GetTubesBySampleId_ReturnsTubes()
    {
        var storage = new InstrumentSampleStorage();
        var tube = new SampleTube
        {
            TubeId = _tubeId,
            SampleId = _sampleId,
            InstrumentNumber = _instrumentNumber,
            TubePosition = 1,
            Area = SampleArea.SampleDrawer1,
            TrayPosition = _trayPosition,
            InstrumentError = SampleTubeError.None
        };
        storage.AddOrUpdateTube(tube, _ => tube);

        var tubes = storage.GetTubesBySampleId(_sampleId);
        tubes.Should().ContainSingle()
            .Which.SampleId.Should().Be(_sampleId);
    }

    [Fact]
    public void GetTubesBySampleId_UsesCurrentSetting()
    {
        var currentSettings = new SamplesSettings();
        var settings = new Mock<IOptionsMonitor<SamplesSettings>>();
        settings.Setup(x => x.CurrentValue).Returns(() => currentSettings);

        var storage = new InstrumentSampleStorage(settings.Object);
        var tube = new SampleTube
        {
            TubeId = _tubeId,
            SampleId = "SAMPLE1",
            InstrumentNumber = _instrumentNumber,
            TubePosition = 1,
            Area = SampleArea.SampleDrawer1,
            TrayPosition = _trayPosition,
            InstrumentError = SampleTubeError.None
        };
        storage.AddOrUpdateTube(tube, _ => tube);

        storage.GetTubesBySampleId("sample1").Should().BeEmpty();
        storage.GetTubesBySampleIds(new HashSet<string> { "sample1" }).Should().BeEmpty();

        currentSettings.CaseInsensitiveSampleId = true;

        storage.GetTubesBySampleId("sample1").Should().ContainSingle();
        storage.GetTubesBySampleIds(new HashSet<string> { "sample1" }).Should().ContainSingle();
    }

    [Fact]
    public void Clear_RemovesAllTubesAndDrawers()
    {
        var storage = new InstrumentSampleStorage();
        var tube = new SampleTube
        {
            TubeId = _tubeId,
            InstrumentNumber = _instrumentNumber,
            TubePosition = 1,
            Area = SampleArea.SampleDrawer1,
            TrayPosition = _trayPosition
        };
        storage.AddOrUpdateTube(tube, _ => tube);

        var drawer = new SampleDrawer
        {
            DrawerType = DrawerType.Sample1
        };
        storage.UpdateDrawer(_instrumentNumber, drawer);

        storage.Clear(_instrumentNumber);

        storage.TubesAsQueryable(_instrumentNumber).AsEnumerable().Should().BeEmpty();
        storage.GetDrawer(_instrumentNumber, DrawerType.Sample1).DrawerType.Should().Be(DrawerType.Sample1);
    }

    [Fact]
    public void AddOrUpdateTube_Throws_WhenMissingInstrumentNumber()
    {
        var storage = new InstrumentSampleStorage();
        var tube = new SampleTube
        {
            TubeId = _tubeId,
            TubePosition = 1,
            Area = SampleArea.SampleDrawer1,
            TrayPosition = _trayPosition
        };
        var act = () => storage.AddOrUpdateTube(tube, _ => tube);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddOrUpdateTube_Throws_WhenMissingTubePosition()
    {
        var storage = new InstrumentSampleStorage();
        var tube = new SampleTube
        {
            TubeId = _tubeId,
            InstrumentNumber = _instrumentNumber,
            Area = SampleArea.SampleDrawer1,
            TrayPosition = _trayPosition
        };
        var act = () => storage.AddOrUpdateTube(tube, _ => tube);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddOrUpdateTube_Throws_WhenMissingTrayPositionForDrawer()
    {
        var storage = new InstrumentSampleStorage();
        var tube = new SampleTube
        {
            TubeId = _tubeId,
            InstrumentNumber = _instrumentNumber,
            TubePosition = 1,
            Area = SampleArea.SampleDrawer1
            // TrayPosition is missing
        };
        var act = () => storage.AddOrUpdateTube(tube, _ => tube);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddOrUpdateTube_AddsTube_WhenNoTubeId()
    {
        var storage = new InstrumentSampleStorage();
        var tube = new SampleTube
        {
            // TubeId is not set
            InstrumentNumber = _instrumentNumber,
            TubePosition = 2,
            Area = SampleArea.SampleDrawer1,
            TrayPosition = _trayPosition
        };

        storage.AddOrUpdateTube(tube, _ => tube);

        var result = storage.TubesAsQueryable(_instrumentNumber).SingleOrDefault();
        result.Should().NotBeNull();
        result.InstrumentNumber.Should().Be(_instrumentNumber);
        result.TubePosition.Should().Be(2);
        result.TrayPosition.Should().Be(_trayPosition);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("S001")]
    public void AddOrUpdateTube_UpdatesTube_WhenNoTubeId(string? scannedTubeId)
    {
        var storage = new InstrumentSampleStorage();

        var tubeToAdd = new SampleTube
        {
            InstrumentNumber = _instrumentNumber,
            TubePosition = 3,
            Area = SampleArea.SampleDrawer1,
            TrayPosition = _trayPosition,
            Status = SampleTubeStatus.InProgress
        };
        storage.AddOrUpdateTube(tubeToAdd, _ => tubeToAdd);

        var tubeToUpdate = new SampleTube
        {
            InstrumentNumber = _instrumentNumber,
            TubeId = scannedTubeId,
            TubePosition = 3,
            Area = SampleArea.SampleDrawer1,
            TrayPosition = _trayPosition,
            Status = SampleTubeStatus.Completed
        };
        storage.AddOrUpdateTube(tubeToUpdate, oldTube =>
        {
            oldTube.Status = tubeToUpdate.Status;
            return oldTube;
        });

        var result = storage.TubesAsQueryable(_instrumentNumber).SingleOrDefault();
        result.Should().NotBeNull();
        result.TubeId.Should().Be(scannedTubeId);
        result.Status.Should().Be(SampleTubeStatus.Completed);
    }

    [Fact]
    public void RemoveTube_RemovesTube_WhenNoTubeId()
    {
        var storage = new InstrumentSampleStorage();

        var tubeToAdd = new SampleTube
        {
            InstrumentNumber = _instrumentNumber,
            TubePosition = 4,
            Area = SampleArea.SampleDrawer1,
            TrayPosition = _trayPosition
        };
        storage.AddOrUpdateTube(tubeToAdd, _ => tubeToAdd);

        var tube = storage.TubesAsQueryable(_instrumentNumber).SingleOrDefault();
        tube.Should().NotBeNull();

        var tubeToRemove = new SampleTube
        {
            InstrumentNumber = _instrumentNumber,
            TubePosition = 4,
            Area = SampleArea.SampleDrawer1,
            TrayPosition = _trayPosition
        };
        storage.RemoveTube(tubeToRemove);

        tube = storage.TubesAsQueryable(_instrumentNumber).SingleOrDefault();
        tube.Should().BeNull();
    }

    [Fact]
    public void AddOrUpdateTube_UpdatesTubePosition_WhenTubeIdIsSet()
    {
        var storage = new InstrumentSampleStorage();

        var initialTube = new SampleTube
        {
            TubeId = _tubeId,
            InstrumentNumber = _instrumentNumber,
            TubePosition = 1,
            Area = SampleArea.SampleDrawer1,
            TrayPosition = _trayPosition
        };
        storage.AddOrUpdateTube(initialTube, _ => initialTube);

        var updatedTube = new SampleTube
        {
            TubeId = _tubeId,
            InstrumentNumber = _instrumentNumber,
            TubePosition = 2, // new position
            Area = SampleArea.SampleDrawer1,
            TrayPosition = _trayPosition
        };
        storage.AddOrUpdateTube(updatedTube, _ => updatedTube);

        var result = storage.GetTube(_tubeId);
        result.Should().NotBeNull();
        result.TubePosition.Should().Be(2);
    }

    [Fact]
    public void UpdateTraysSummary_UpdatesFreePositionsAndErrorCount_WhenTubesChange()
    {
        var storage = new InstrumentSampleStorage();
        var trayId = "TRAY1";
        var instrumentNumber = _instrumentNumber;

        // Setup: Add a drawer and tray
        var tray = new SampleTray
        {
            Id = trayId,
            TrayPosition = 1,
            FreePositions = 5,
            TubeErrorCount = 0
        };
        var drawer = new SampleDrawer
        {
            DrawerType = DrawerType.Sample1,
            Trays = [tray]
        };
        storage.UpdateDrawer(instrumentNumber, drawer);

        // Add a tube to the tray (should reduce FreePositions by 1)
        var tube = new SampleTube
        {
            TubeId = "TUBE1",
            InstrumentNumber = instrumentNumber,
            Area = SampleArea.SampleDrawer1,
            TrayPosition = 1,
            TubePosition = 1,
        };
        storage.AddOrUpdateTube(tube, _ => tube);

        // Act
        var changed = storage.UpdateTraysSummary(instrumentNumber);

        // Assert
        changed.Should().BeTrue();
        var updatedTray = storage.GetTray(trayId);
        updatedTray.Should().NotBeNull();
        updatedTray.FreePositions.Should().Be(Constants.MaxSampleTubesInTray - 1);
        updatedTray.TubeErrorCount.Should().Be(0);
    }

    [Trait("ProductRequirement", "PR_304")]
    [Trait("ModuleRequirement", "MR_4398")]
    [Fact]
    public void UpdateTraysSummary_UpdatesErrorCount_WhenTubeHasError()
    {
        var storage = new InstrumentSampleStorage();
        var trayId = "TRAY2";
        var instrumentNumber = _instrumentNumber;

        // Setup: Add a drawer and tray
        var tray = new SampleTray
        {
            Id = trayId,
            TrayPosition = 2,
            FreePositions = 5,
            TubeErrorCount = 0
        };
        var drawer = new SampleDrawer
        {
            DrawerType = DrawerType.Sample1,
            Trays = [tray]
        };
        storage.UpdateDrawer(instrumentNumber, drawer);

        // Add a tube with error to the tray
        var tube = new SampleTube
        {
            TubeId = "TUBE2",
            InstrumentNumber = instrumentNumber,
            Area = SampleArea.SampleDrawer1,
            TrayPosition = 2,
            TubePosition = 1,
            InstrumentError = SampleTubeError.TubeShapeError
        };
        storage.AddOrUpdateTube(tube, _ => tube);

        // Act
        var changed = storage.UpdateTraysSummary(instrumentNumber);

        // Assert
        changed.Should().BeTrue();
        var updatedTray = storage.GetTray(trayId);
        updatedTray.Should().NotBeNull();
        updatedTray.TubeErrorCount.Should().Be(1);
    }

    [Fact]
    public void UpdateTraysSummary_ReturnsFalse_WhenNoChange()
    {
        var storage = new InstrumentSampleStorage();
        var trayId = "TRAY3";
        var instrumentNumber = _instrumentNumber;

        // Setup: Add a drawer and tray
        var tray = new SampleTray
        {
            Id = trayId,
            TrayPosition = 3,
            FreePositions = Constants.MaxSampleTubesInTray,
            TubeErrorCount = 0
        };
        var drawer = new SampleDrawer
        {
            DrawerType = DrawerType.Sample1,
            Trays = [tray]
        };
        storage.UpdateDrawer(instrumentNumber, drawer);

        // No tubes added, so summary should not change
        var changed = storage.UpdateTraysSummary(instrumentNumber);

        changed.Should().BeFalse();
        var updatedTray = storage.GetTray(trayId);
        updatedTray.Should().NotBeNull();
        updatedTray.FreePositions.Should().Be(Constants.MaxSampleTubesInTray);
        updatedTray.TubeErrorCount.Should().Be(0);
    }

    [Fact]
    public void AddOrUpdateTube_UpdatesTube_WhenTubeIdIsSet()
    {
        var storage = new InstrumentSampleStorage();
        // ISW will initially Add the tube to storage once tube is detected in the drawer.
        var initialTube = new SampleTube
        {
            InstrumentNumber = _instrumentNumber,
            TrayId = "1111",
            TubePosition = 1,
            Area = SampleArea.SampleDrawer1,
            TrayPosition = _trayPosition
        };
        storage.AddOrUpdateTube(initialTube, _ => initialTube);

        // tubeId updates on barcode scan
        var tube = new SampleTube
        {
            InstrumentNumber = _instrumentNumber,
            TubeId = _tubeId,
            TrayId = "1111",
            TubePosition = 1,
            Area = SampleArea.SampleDrawer1,
            TrayPosition = _trayPosition
        };

        storage.AddOrUpdateTube(tube, oldTube =>
        {
            oldTube.TubeId = _tubeId;
            oldTube.SampleId = _tubeId;
            oldTube.SampleIdSuffix = "1";
            oldTube.Status = SampleTubeStatus.InProgress;
            return oldTube;
        });

        storage.TubesAsQueryable(_instrumentNumber).Count(t => t.Area == SampleArea.SampleDrawer1 && t.TubePosition == 1 && t.TrayPosition == _trayPosition).Should().Be(1);
        var result = storage.GetTube(_tubeId);
        result.Should().NotBeNull();
        result.TubeId.Should().Be(_tubeId);
        result.SampleId.Should().Be(_tubeId);
        result.SampleIdSuffix.Should().Be("1");
        result.Status.Should().Be(SampleTubeStatus.InProgress);
    }

    [Fact]
    public void AddOrUpdateTube_UpdatesPermanentTube_AfterAddingUnidentifiedTubeAtSamePosition()
    {
        var storage = new InstrumentSampleStorage();

        var permanentTube = new SampleTube
        {
            InstrumentNumber = _instrumentNumber,
            TubeId = _tubeId,
            Area = SampleArea.SampleDrawer1,
            TrayPosition = _trayPosition,
            TubePosition = 1,
            Status = SampleTubeStatus.InProgress
        };
        storage.AddOrUpdateTube(permanentTube, _ => permanentTube);

        var unidentifiedTubeAtSamePosition = new SampleTube
        {
            InstrumentNumber = _instrumentNumber,
            Area = SampleArea.SampleDrawer1,
            TrayPosition = _trayPosition,
            TubePosition = 1,
            Status = SampleTubeStatus.NotIdentified
        };
        storage.AddOrUpdateTube(unidentifiedTubeAtSamePosition, _ => unidentifiedTubeAtSamePosition);

        var tubes = storage.TubesAsQueryable(_instrumentNumber).ToList();
        tubes.Count.Should().Be(2);

        var permanentTubeUpdate = new SampleTube
        {
            InstrumentNumber = _instrumentNumber,
            TubeId = _tubeId,
            Area = SampleArea.SampleDrawer1,
            TrayPosition = _trayPosition,
            TubePosition = 1,
            Status = SampleTubeStatus.Completed
        };
        storage.AddOrUpdateTube(permanentTubeUpdate, oldTube =>
        {
            oldTube.Status = permanentTubeUpdate.Status;
            return oldTube;
        });

        tubes = storage.TubesAsQueryable(_instrumentNumber).ToList();
        tubes.Count.Should().Be(2);

        var tube = storage.GetTube(_tubeId);
        tube.Should().NotBeNull();
        tube.Status.Should().Be(SampleTubeStatus.Completed);
    }

    [Fact]
    public void UpdateTraysSummary_ReturnsTrue_WhenLastTubeIsMovedFromTray()
    {
        var storage = new InstrumentSampleStorage();
        var trayId = "TRAY4";
        var instrumentNumber = _instrumentNumber;

        // Add a drawer and tray
        var tray = new SampleTray
        {
            Id = trayId,
            TrayPosition = 4,
            FreePositions = 49,
            TubeErrorCount = 0
        };

        var drawer = new SampleDrawer
        {
            DrawerType = DrawerType.Sample1,
            Trays = [tray]
        };

        storage.UpdateDrawer(instrumentNumber, drawer);

        // Add a tube to the tray
        var tube = new SampleTube
        {
            TubeId = "TUBE50",
            InstrumentNumber = instrumentNumber,
            Area = SampleArea.SampleDrawer1,
            TrayPosition = 4,
            TubePosition = 1,
        };

        storage.AddOrUpdateTube(tube, _ => tube);

        // Update to reflect current state
        storage.UpdateTraysSummary(instrumentNumber);
        var currentTray = storage.GetTray(trayId);
        currentTray!.FreePositions.Should().Be(49);

        // Move the tube to internal storage
        var tube1 = new SampleTube
        {
            TubeId = "TUBE50",
            InstrumentNumber = instrumentNumber,
            Area = SampleArea.InternalStorage,
            TubePosition = 10,
        };

        storage.AddOrUpdateTube(tube1, _ => tube1);

        // Act
        var changed = storage.UpdateTraysSummary(instrumentNumber);

        // Assert
        changed.Should().BeTrue();
        var updatedTray = storage.GetTray(trayId);
        updatedTray.Should().NotBeNull();
        updatedTray.FreePositions.Should().Be(Constants.MaxSampleTubesInTray);
        updatedTray.TubeErrorCount.Should().Be(0);
    }

    [Fact]
    public void UpdateTrayAsUnknownObject_RemovesTubesFromTrayAndSetsTrayError_WhenTrayIsNotUsable()
    {
        var storage = new InstrumentSampleStorage();
        var trayId = "TRAY5";
        var otherTrayId = "TRAY6";

        storage.UpdateDrawer(_instrumentNumber, new SampleDrawer
        {
            DrawerType = DrawerType.Sample1,
            Trays =
            [
                new SampleTray
                {
                    Id = trayId,
                    TrayPosition = 1,
                    FreePositions = 48,
                    TubeErrorCount = 2
                },
                new SampleTray
                {
                    Id = otherTrayId,
                    TrayPosition = 2,
                    FreePositions = 49,
                    TubeErrorCount = 1
                }
            ]
        });

        var tubeInTray = new SampleTube
        {
            TubeId = "TUBE5",
            InstrumentNumber = _instrumentNumber,
            Area = SampleArea.SampleDrawer1,
            TrayPosition = 1,
            TubePosition = 1
        };
        storage.AddOrUpdateTube(tubeInTray, _ => tubeInTray);

        var tubeInOtherTray = new SampleTube
        {
            TubeId = "TUBE6",
            InstrumentNumber = _instrumentNumber,
            Area = SampleArea.SampleDrawer1,
            TrayPosition = 2,
            TubePosition = 1
        };
        storage.AddOrUpdateTube(tubeInOtherTray, _ => tubeInOtherTray);

        // Act
        var result = storage.UpdateTrayAsUnknownObject(trayId);

        // Assert
        result.Should().BeTrue();

        var unknownTray = storage.GetTrays(_instrumentNumber, DrawerType.Sample1).Single(x => x.Error == SampleTrayError.UnknownObject);
        unknownTray.Id.Should().BeEmpty();
        unknownTray.FreePositions.Should().Be(Constants.MaxSampleTubesInTray);
        unknownTray.TubeErrorCount.Should().Be(0);

        storage.GetTube("TUBE5").Should().BeNull();
        storage.GetTube("TUBE6").Should().NotBeNull();
    }
}
