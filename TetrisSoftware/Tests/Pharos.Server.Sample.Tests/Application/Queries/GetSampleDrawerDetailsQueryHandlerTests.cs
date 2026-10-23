using Microsoft.Extensions.DependencyInjection;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Request;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Sample.Application.Interfaces;
using Pharos.Server.Sample.Application.Queries.GetSampleDrawerDetails;
using Pharos.Server.Sample.Domain;

namespace Pharos.Server.Sample.Tests.Application.Queries;

[Collection(nameof(SampleTestsCollection))]
public class GetSampleDrawerDetailsQueryHandlerTests(SampleTestsWaf factory) : DatabaseTestBase<SampleTestsWaf>(factory)
{
    private readonly string _instrumentNumber = "SEED_01";
    private readonly string _trayId = "1234";

    [Fact]
    [Trait("ProductRequirement", "PR_312")]
    [Trait("ModuleRequirement", "MR_4143")]
    public async Task Handle_ShouldPopulateFromStorage_WhenDrawerIsClosed()
    {
        // Arrange
        var sampleStorage = ServiceProvider.GetRequiredService<IInstrumentSampleStorage>();
        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();
        var sampleIdFormatter = ServiceProvider.GetRequiredService<ISampleIdFormatter>();

        sampleStorage.UpdateDrawer(_instrumentNumber, new SampleDrawer
        {
            DrawerType = DrawerType.Sample1,
            Trays =
            [
                new SampleTray
                {
                    Id = _trayId,
                    Drawer = DrawerType.Sample1,
                    TrayPosition = 1,
                }
            ]
        });

        var tb = new SampleTube
        {
            TubeId = "1",
            SampleId = "S1",
            Status = SampleTubeStatus.Completed,
            InstrumentNumber = _instrumentNumber,
            TrayId = _trayId,
            Area = SampleArea.SampleDrawer1,
            TrayPosition = 1,
            TubePosition = 1,
            IdentitySource = SampleTubeIdentitySource.ManualInput
        };
        sampleStorage.AddOrUpdateTube(tb, _ => tb);


        var instrumentServiceMock = new Mock<IInstrumentManager>();
        var instrumentMock = CreateInstrumentMock(instrumentServiceMock);
        instrumentMock.Setup(x => x.DrawersLockState[DrawerType.Sample1])
            .Returns(new DrawerLockState(DrawerType.Sample1, LockState.ClosedLocked, false));

        var handler = new GetSampleDrawerDetailsQueryHandler(sampleStorage, instrumentServiceMock.Object, dbContext, sampleIdFormatter);

        var query = new GetSampleDrawerDetailsQuery(_instrumentNumber, DrawerType.Sample1);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Drawer.Should().Be(DrawerType.Sample1);
        result.LockState.Should().Be(LockState.ClosedLocked);
        result.SampleTrays.Should().NotBeEmpty();
        result.SampleTrays.Should().HaveCount(Constants.MaxTraysInSampleDrawer);

        var tray = result.SampleTrays.First(t => t.Id == _trayId);
        tray.Should().NotBeNull();
        tray.SamplePositions.Should().NotBeEmpty();

        var tube = tray.SamplePositions.First(p => p.Position == 1);
        tube.SampleId.Should().Be("S1");
        tube.SampleTubeStatus.Should().Be(SampleTubeStatus.Completed);
        tube.Error.Should().Be(SampleTubeError.None);
        tube.SamplePositionSource.Should().Be(SampleTubeIdentitySource.ManualInput);

        // Verify empty tray positions are filled
        tray.SamplePositions
            .Where(p => p.Position != 1)
            .Should()
            .AllSatisfy(p => p.SampleTubeStatus.Should().Be(SampleTubeStatus.None));
    }

    [Fact]
    [Trait("ProductRequirement", "PR_312")]
    [Trait("ModuleRequirement", "MR_4724")]
    public async Task Handle_ShouldPopulateFromDatabase_WhenDrawerIsOpen()
    {
        // Arrange
        var sampleStorage = ServiceProvider.GetRequiredService<IInstrumentSampleStorage>();
        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();
        var sampleIdFormatter = ServiceProvider.GetRequiredService<ISampleIdFormatter>();

        dbContext.SampleTrays.Add(new SampleTray
        {
            Id = _trayId,
            Drawer = DrawerType.Sample1,
            InstrumentNumber = _instrumentNumber,
            TrayPosition = 1,
            SampleTubes = new List<SampleTube>
            {
                new()
                {
                    TubeId = "1",
                    SampleId = "S1",
                    Status = SampleTubeStatus.Completed,
                    InstrumentNumber = _instrumentNumber,
                    TubePosition = 1,
                    IdentitySource = SampleTubeIdentitySource.BarcodeScanning
                }
            }
        });

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var instrumentServiceMock = new Mock<IInstrumentManager>();
        var instrumentMock = CreateInstrumentMock(instrumentServiceMock);
        instrumentMock.Setup(x => x.DrawersLockState[DrawerType.Sample1])
            .Returns(new DrawerLockState(DrawerType.Sample1, LockState.Open, false));

        var handler = new GetSampleDrawerDetailsQueryHandler(sampleStorage, instrumentServiceMock.Object, dbContext, sampleIdFormatter);

        var query = new GetSampleDrawerDetailsQuery(_instrumentNumber, DrawerType.Sample1);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Drawer.Should().Be(DrawerType.Sample1);
        result.LockState.Should().Be(LockState.Open);
        result.SampleTrays.Should().NotBeEmpty();
        result.SampleTrays.Should().HaveCount(Constants.MaxTraysInSampleDrawer);

        var tray = result.SampleTrays.First(t => t.Id == _trayId);
        tray.Should().NotBeNull();
        tray.SamplePositions.Should().NotBeEmpty();

        var tube = tray.SamplePositions.First(p => p.Position == 1);
        tube.SampleId.Should().Be("S1");
        tube.SampleTubeStatus.Should().Be(SampleTubeStatus.Completed);
        tube.Error.Should().Be(SampleTubeError.None);
        tube.SamplePositionSource.Should().Be(SampleTubeIdentitySource.BarcodeScanning);

        // Verify empty tray positions are filled
        tray.SamplePositions
            .Where(p => p.Position != 1)
            .Should()
            .AllSatisfy(p => p.SampleTubeStatus.Should().Be(SampleTubeStatus.None));
    }

    [Fact]
    [Trait("ProductRequirement", "PR_312")]
    [Trait("ModuleRequirement", "MR_4724")]
    public async Task Handle_ShouldPopulateFromDatabaseAndStorage_WhenDrawerIsOpenAndContainsTraysWithErrors()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();
        var sampleStorage = ServiceProvider.GetRequiredService<IInstrumentSampleStorage>();
        var sampleIdFormatter = ServiceProvider.GetRequiredService<ISampleIdFormatter>();

        dbContext.SampleTrays.Add(new SampleTray
        {
            Id = _trayId,
            Drawer = DrawerType.Sample1,
            InstrumentNumber = _instrumentNumber,
            TrayPosition = 1,
            SampleTubes = new List<SampleTube>
        {
            new()
            {
                TubeId = "1",
                SampleId = "S1",
                Status = SampleTubeStatus.Completed,
                InstrumentNumber = _instrumentNumber,
                TubePosition = 1,
                IdentitySource = SampleTubeIdentitySource.BarcodeScanning
            }
        }
        });

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        sampleStorage.UpdateDrawer(_instrumentNumber, new SampleDrawer
        {
            DrawerType = DrawerType.Sample1,
            Trays = new List<SampleTray>
        {
            new()
            {
                Id = "ErrorTray",
                Drawer = DrawerType.Sample1,
                TrayPosition = 2,
                Error = SampleTrayError.InvalidBarcode
            }
        }
        });

        var instrumentServiceMock = new Mock<IInstrumentManager>();
        var instrumentMock = CreateInstrumentMock(instrumentServiceMock);
        instrumentMock.Setup(x => x.DrawersLockState[DrawerType.Sample1])
            .Returns(new DrawerLockState(DrawerType.Sample1, LockState.Open, false));

        var handler = new GetSampleDrawerDetailsQueryHandler(sampleStorage, instrumentServiceMock.Object, dbContext, sampleIdFormatter);

        var query = new GetSampleDrawerDetailsQuery(_instrumentNumber, DrawerType.Sample1);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Drawer.Should().Be(DrawerType.Sample1);
        result.LockState.Should().Be(LockState.Open);
        result.SampleTrays.Should().NotBeEmpty();
        result.SampleTrays.Should().HaveCount(Constants.MaxTraysInSampleDrawer);

        var tray = result.SampleTrays.First(t => t.Id == _trayId);
        tray.Should().NotBeNull();
        tray.SamplePositions.Should().NotBeEmpty();

        var tube = tray.SamplePositions.First(p => p.Position == 1);
        tube.SampleId.Should().Be("S1");
        tube.SampleTubeStatus.Should().Be(SampleTubeStatus.Completed);
        tube.Error.Should().Be(SampleTubeError.None);
        tube.SamplePositionSource.Should().Be(SampleTubeIdentitySource.BarcodeScanning);

        var errorTray = result.SampleTrays.First(t => t.Id == "ErrorTray");
        errorTray.Should().NotBeNull();
        errorTray.TrayError.Should().Be(SampleTrayError.InvalidBarcode);

        // Verify empty tray positions are filled
        tray.SamplePositions
            .Where(p => p.Position != 1)
            .Should()
            .AllSatisfy(p => p.SampleTubeStatus.Should().Be(SampleTubeStatus.None));
    }

    private Mock<IInstrument> CreateInstrumentMock(Mock<IInstrumentManager> instrumentServiceMock)
    {
        var instrumentMock = new Mock<IInstrument>();

        instrumentServiceMock
            .Setup(x => x.GetInstrument(_instrumentNumber))
            .Returns(instrumentMock.Object);

        return instrumentMock;
    }
}

