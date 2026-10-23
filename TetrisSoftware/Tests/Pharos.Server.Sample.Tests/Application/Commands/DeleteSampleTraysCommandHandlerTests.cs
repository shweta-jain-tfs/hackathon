using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Server.Application.Request;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Sample.Application.Commands.DeleteSampleTray;
using Pharos.Server.Sample.Application.Interfaces;
using Pharos.Server.Sample.Application.Services;
using Pharos.Server.Sample.Domain;

namespace Pharos.Server.Sample.Tests.Application.Commands;

[Collection(nameof(SampleTestsCollection))]
public class DeleteSampleTraysCommandHandlerTests(SampleTestsWaf factory) : DatabaseTestBase<SampleTestsWaf>(factory)
{
    private readonly int _tubeEntityId = 1;
    private readonly string _leftDrawerTrayId = "2345";

    [Fact]
    public async Task Handle_ShouldDeleteSampleTrayAndSampleTubesWithoutRequests_WhenTrayNotInSampleStorage()
    {
        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();
        var sampleStorage = new InstrumentSampleStorage(); // Use parameterless constructor
        var requestCreatedDate = new DateTimeOffset(new DateTime(2025, 1, 1), TimeSpan.Zero);
        dbContext.SampleTrays.Add(new SampleTray
        {
            Id = _leftDrawerTrayId,
            Drawer = DrawerType.Sample1,
            InstrumentNumber = null,
            SampleTubes = new List<SampleTube>
            {
                new()
                {
                    TubeId = "1",
                    SampleId = "S1",
                    Status = SampleTubeStatus.Completed,
                    InstrumentNumber = null,
                    TubePosition = 1,
                    RequestCreatedDate = requestCreatedDate
                }
            }
        });

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Note: sampleStorage is empty by default, so tray is not in storage

        var senderMock = new Mock<ISender>();
        senderMock
            .Setup(x => x.Send(It.Is<GetRequestIdQuery>(q => q.SampleId == "S1" && q.RequestCreatedDate == requestCreatedDate), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RequestIdResponseDto(-1, requestCreatedDate, false));

        var handler = new DeleteSampleTrayCommandHandler(dbContext, sampleStorage, senderMock.Object);
        var result = await handler.Handle(new DeleteSampleTrayCommand(new[] { _leftDrawerTrayId }), TestContext.Current.CancellationToken);
        result.Should().NotBeEmpty();
        senderMock.Verify(x => x.Send(It.IsAny<GetRequestIdQuery>(), It.IsAny<CancellationToken>()), Times.Once);

        // use a new scope to check the database (to avoid hitting cached items)
        await using var scope = ServiceProvider.CreateAsyncScope();
        dbContext = scope.ServiceProvider.GetRequiredService<ISampleDbContext>();

        var sampleTray = await dbContext.SampleTrays
            .Where(x => x.Id == _leftDrawerTrayId)
            .Include(t => t.SampleTubes)
            .FirstOrDefaultAsync(TestContext.Current.CancellationToken);

        sampleTray.Should().BeNull();

        var sampleTube = await dbContext.SampleTubes.FirstOrDefaultAsync(t => t.SampleId == "S1", TestContext.Current.CancellationToken);
        sampleTube.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldDetachSampleTube_WhenTrayNotInSampleStorageAndRequestExists()
    {
        var dbContext = ServiceProvider.GetRequiredService<ISampleDbContext>();
        var sampleStorage = new InstrumentSampleStorage();
        var requestCreatedDate = new DateTimeOffset(new DateTime(2025, 1, 1), TimeSpan.Zero);
        dbContext.SampleTrays.Add(new SampleTray
        {
            Id = _leftDrawerTrayId,
            Drawer = DrawerType.Sample1,
            InstrumentNumber = null,
            SampleTubes = new List<SampleTube>
            {
                new()
                {
                    TubeId = "1",
                    SampleId = "S1",
                    Status = SampleTubeStatus.Completed,
                    InstrumentNumber = null,
                    TubePosition = 1,
                    RequestCreatedDate = requestCreatedDate
                }
            }
        });

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Note: sampleStorage is empty by default, so tray is not in storage

        var senderMock = new Mock<ISender>();
        senderMock
            .Setup(x => x.Send(It.Is<GetRequestIdQuery>(q => q.SampleId == "S1" && q.RequestCreatedDate == requestCreatedDate), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RequestIdResponseDto(_tubeEntityId, requestCreatedDate, true));

        var handler = new DeleteSampleTrayCommandHandler(dbContext, sampleStorage, senderMock.Object);
        var result = await handler.Handle(new DeleteSampleTrayCommand(new[] { _leftDrawerTrayId }), TestContext.Current.CancellationToken);
        result.Should().NotBeEmpty();
        senderMock.Verify(x => x.Send(It.IsAny<GetRequestIdQuery>(), It.IsAny<CancellationToken>()), Times.Once);

        await using var scope = ServiceProvider.CreateAsyncScope();
        dbContext = scope.ServiceProvider.GetRequiredService<ISampleDbContext>();

        var sampleTray = await dbContext.SampleTrays
            .Where(x => x.Id == _leftDrawerTrayId)
            .Include(t => t.SampleTubes)
            .FirstOrDefaultAsync(TestContext.Current.CancellationToken);

        sampleTray.Should().BeNull();

        var sampleTube = await dbContext.SampleTubes.FirstOrDefaultAsync(t => t.SampleId == "S1", TestContext.Current.CancellationToken);
        sampleTube.Should().NotBeNull();
        sampleTube!.TrayId.Should().BeNull();
        sampleTube.TubePosition.Should().BeNull();
        sampleTube.InstrumentNumber.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyList_WhenNoTraysFound()
    {
        var sender = ServiceProvider.GetRequiredService<ISender>();
        var result = await sender.Send(new DeleteSampleTrayCommand(["99"]), TestContext.Current.CancellationToken);
        result.Count.Should().Be(0);
    }
}
