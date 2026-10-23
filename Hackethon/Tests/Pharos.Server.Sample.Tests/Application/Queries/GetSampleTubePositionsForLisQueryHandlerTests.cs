using Microsoft.Extensions.DependencyInjection;
using Pharos.Server.Application.Sample;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Sample.Application.Interfaces;
using Pharos.Server.Sample.Application.Queries.GetSampleTubePositionsForLis;
using Pharos.Server.Sample.Application.Services;
using Pharos.Server.Sample.Domain;

namespace Pharos.Server.Sample.Tests.Application.Queries;

[Collection(nameof(SampleTestsCollection))]
public class GetSampleTubePositionsForLisQueryHandlerTests(SampleTestsWaf factory)
    : DatabaseTestBase<SampleTestsWaf>(factory)
{
    private ISampleDbContext DbContext => ServiceProvider.GetRequiredService<ISampleDbContext>();

    private static (IInstrumentSampleStorage Storage, GetSampleTubePositionsForLisQueryHandler Handler)
        CreateHandlerWithMock(ISampleDbContext dbContext)
    {
        var storage = new InstrumentSampleStorage();
        var handler = new GetSampleTubePositionsForLisQueryHandler(storage, dbContext);
        return (storage, handler);
    }

    [Fact]
    public async Task WhenTubeFoundInInstrumentStorage_ReturnsTubeLocationFromStorage()
    {
        // Arrange
        var tubeId = "TUBE001";
        var expectedTrayId = "0001";
        var expectedPosition = 5;

        var tubeInInstrument = new SampleTube
        {
            InstrumentNumber = "INSTR001",
            TubeId = tubeId,
            TrayId = expectedTrayId,
            TubePosition = expectedPosition
        };

        var (storage, handler) = CreateHandlerWithMock(DbContext);
        storage.AddOrUpdateTube(tubeInInstrument, _ => tubeInInstrument);

        var query = new GetSampleTubePositionsForLisQuery([tubeId]);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().HaveCount(1);
        result[0].TubeId.Should().Be(tubeId);
        result[0].TrayId.Should().Be(expectedTrayId);
        result[0].TubePosition.Should().Be(expectedPosition);
    }

    [Fact]
    public async Task WhenTubeNotInStorageButInDatabase_ReturnsTubeLocationFromDatabase()
    {
        // Arrange
        var tubeId = "TUBE002";
        var expectedTrayId = "0002";
        var expectedPosition = 3;

        var dbContext = DbContext;

        // Create the tray first to satisfy foreign key constraint
        await dbContext.SampleTrays.AddAsync(new SampleTray
        {
            Id = expectedTrayId
        }, TestContext.Current.CancellationToken);

        await dbContext.SampleTubes.AddAsync(new SampleTube
        {
            TubeId = tubeId,
            TrayId = expectedTrayId,
            TubePosition = expectedPosition
        }, TestContext.Current.CancellationToken);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var (_, handler) = CreateHandlerWithMock(dbContext);

        var query = new GetSampleTubePositionsForLisQuery([tubeId]);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().HaveCount(1);
        result[0].TubeId.Should().Be(tubeId);
        result[0].TrayId.Should().Be(expectedTrayId);
        result[0].TubePosition.Should().Be(expectedPosition);
    }

    [Fact]
    public async Task WhenMultipleRecordsInDatabase_ReturnsLatestRecord()
    {
        // Arrange
        var tubeId = "TUBE003";

        var dbContext = DbContext;

        // Create the trays first to satisfy foreign key constraint
        await dbContext.SampleTrays.AddRangeAsync(
            new SampleTray { Id = "0003" },
            new SampleTray { Id = "0004" });

        await dbContext.SampleTubes.AddRangeAsync(
            new SampleTube { TubeId = tubeId, TrayId = "0003", TubePosition = 1 },
            new SampleTube { TubeId = tubeId, TrayId = "0004", TubePosition = 7 });
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var (_, handler) = CreateHandlerWithMock(dbContext);

        var query = new GetSampleTubePositionsForLisQuery([tubeId]);

        // Act
        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().HaveCount(1);
        result[0].TubeId.Should().Be(tubeId);
        result[0].TrayId.Should().Be("0004");
        result[0].TubePosition.Should().Be(7);
    }
}
