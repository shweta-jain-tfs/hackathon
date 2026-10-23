using Microsoft.Extensions.DependencyInjection;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Localization;
using Pharos.Server.Sample.Application.Interfaces;
using Pharos.Server.Sample.Application.Queries.GetSampleTrayList;
using Pharos.Server.Sample.Application.Services;
using Pharos.Server.Sample.Domain;

namespace Pharos.Server.Sample.Tests.Application.Queries;

[Collection(nameof(SampleTestsCollection))]
public class GetSampleTrayListQueryHandlerTests(SampleTestsWaf factory) : DatabaseTestBase<SampleTestsWaf>(factory)
{
    private readonly InstrumentSampleStorage _sampleStorage = new();
    private readonly string _instrumentNumber = "S001";
    private readonly string _trayId1 = "TR01";
    private readonly string _trayId2 = "TR02";

    [Fact]
    public async Task Handle_ShouldReturnEmptyList_WhenNoSampleTraysExist()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();
        var handler = new GetSampleTrayListQueryHandler(_sampleStorage, dbContext, ServiceProvider.GetRequiredService<IInstrumentManager>());

        var query = new GetSampleTrayListQuery
        {
            StartIndex = 0,
            Count = 10
        };

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldReturnTraysFromDatabaseAndStorage_WhenConditionsAreMet()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();
        var handler = new GetSampleTrayListQueryHandler(_sampleStorage, dbContext, ServiceProvider.GetRequiredService<IInstrumentManager>());

        // Add multiple trays to the database
        dbContext.SampleTrays.AddRange(new List<SampleTray>
        {
            new()
            {
                Id = _trayId1,
                Drawer = DrawerType.Sample1,
                TrayPosition = 1,
                SampleTubes = new List<SampleTube>
                {
                    new()
                    {
                        TubeId = "T01",
                        InstrumentError = SampleTubeError.None,
                        InternalError = SampleTubeError.None
                    }
                }
            },
            new()
            {
                Id = _trayId2,
                Drawer = DrawerType.Sample2,
                TrayPosition = 2,
                SampleTubes = new List<SampleTube>
                {
                    new()
                    {
                        TubeId = "T02",
                        InstrumentError = SampleTubeError.TubeShapeError,
                        InternalError = SampleTubeError.None
                    }
                }
            }
        });
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Add one of the trays to the sample storage with a different state
        var sampleDrawer = new SampleDrawer
        {
            DrawerType = DrawerType.Sample1,
            Trays =
            [
                new SampleTray
                {
                    Id = _trayId1,
                    TrayPosition = 1,
                    FreePositions = 5, // Different from the database
                    TubeErrorCount = 1 // Different from the database
                }
            ]
        };
        _sampleStorage.UpdateDrawer(_instrumentNumber, sampleDrawer);

        var query = new GetSampleTrayListQuery
        {
            StartIndex = 0,
            Count = 10
        };

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2); // Both trays should be included
        var tray1 = result.Items.FirstOrDefault(x => x.Id == _trayId1);
        tray1.Should().NotBeNull();
        tray1!.FreePositions.Should().Be(5); // From sample storage
        tray1.ErrorCount.Should().Be(1); // From sample storage

        var tray2 = result.Items.FirstOrDefault(x => x.Id == _trayId2);
        tray2.Should().NotBeNull();
        tray2!.FreePositions.Should().Be(Constants.MaxSampleTubesInTray - 1); // Calculated from the database
        tray2.ErrorCount.Should().Be(1); // Calculated from the database
    }

    [Fact]
    public async Task Handle_ShouldReturnNotLoadedLocation_WhenTrayInstrumentNumberIsMissing()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();
        var handler = new GetSampleTrayListQueryHandler(_sampleStorage, dbContext, ServiceProvider.GetRequiredService<IInstrumentManager>());

        dbContext.SampleTrays.Add(new SampleTray
        {
            Id = _trayId1,
            Drawer = DrawerType.Sample1,
            TrayPosition = 1,
            InstrumentNumber = null
        });
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var query = new GetSampleTrayListQuery
        {
            StartIndex = 0,
            Count = 10
        };

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Items.Should().ContainSingle();
        result.Items[0].InstrumentName.Should().Be(Translations.TrayLocation_NotLoaded);
    }

    [Fact]
    public async Task Handle_ShouldFilterTraysByError_WhenErrorFilterIsApplied()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();
        var handler = new GetSampleTrayListQueryHandler(_sampleStorage, dbContext, ServiceProvider.GetRequiredService<IInstrumentManager>());

        dbContext.SampleTrays.Add(new SampleTray
        {
            Id = _trayId1,
            Drawer = DrawerType.Sample1,
            TrayPosition = 1,
            SampleTubes = new List<SampleTube>
            {
                new()
                {
                    TubeId = "T01",
                    InstrumentError = SampleTubeError.TubeShapeError
                }
            }
        });
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var query = new GetSampleTrayListQuery
        {
            StartIndex = 0,
            Count = 10
        };

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Items.First().Id.Should().Be(_trayId1);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyResult_WhenNoMatchingTraysFound()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();
        var sampleStorage = ServiceProvider.GetRequiredService<IInstrumentSampleStorage>();
        var handler = new GetSampleTrayListQueryHandler(sampleStorage, dbContext, ServiceProvider.GetRequiredService<IInstrumentManager>());

        var query = new GetSampleTrayListQuery
        {
            TrayId = "NonExistentTray",
            StartIndex = 0,
            Count = 10
        };

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
    }
}
