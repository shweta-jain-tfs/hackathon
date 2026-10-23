using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Lis.Application.Commands.LisStatisticsCommands;
using Pharos.Server.Lis.Application.Events;
using Pharos.Server.Lis.Application.Interfaces;
using Pharos.Server.Lis.Domain;

namespace Pharos.Server.Lis.Tests.Application.Commands.LisStatisticsCommands;

[Collection(nameof(LisTestsCollection))]
[Trait("ProductRequirement", "PR_565")]
[Trait("ModuleRequirement", "MR_5377")]
public class DeleteLisStatisticsCommandHandlerTests(LisTestsWaf factory) : DatabaseTestBase<LisTestsWaf>(factory)
{
    private ILisDbContext DbContext => ServiceProvider.GetRequiredService<ILisDbContext>();

    private static (Mock<IPublisher> PublisherMock, DeleteLisStatisticsCommandHandler Handler)
        CreateHandler(ILisDbContext dbContext)
    {
        var publisherMock = new Mock<IPublisher>();
        var handler = new DeleteLisStatisticsCommandHandler(dbContext, publisherMock.Object);
        return (publisherMock, handler);
    }

    [Fact]
    public async Task Handle_Should_DeleteAllStatisticsRows_ForChannel()
    {
        const int targetChannelId = 1;
        const int otherChannelId = 2;
        var (_, handler) = CreateHandler(DbContext);

        DbContext.LisStatistics.AddRange(
            new LisStatistics
            {
                ChannelId = targetChannelId,
                LisStatisticsCategory = LisStatisticsCategory.Queries,
                SamplesCount = 10
            },
            new LisStatistics
            {
                ChannelId = targetChannelId,
                LisStatisticsCategory = LisStatisticsCategory.Imported,
                SamplesCount = 20,
                TestsCount = 30
            },
            new LisStatistics
            {
                ChannelId = targetChannelId,
                LisStatisticsCategory = LisStatisticsCategory.Exported,
                SamplesCount = 40,
                TestsCount = 50
            },
            new LisStatistics
            {
                ChannelId = otherChannelId,
                LisStatisticsCategory = LisStatisticsCategory.Exported,
                SamplesCount = 5,
                TestsCount = 6
            });
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await handler.Handle(new DeleteLisStatisticsCommand(targetChannelId), TestContext.Current.CancellationToken);

        var remainingStatistics = await DbContext.LisStatistics
            .AsNoTracking()
            .ToListAsync(TestContext.Current.CancellationToken);

        remainingStatistics.Should().ContainSingle(s => s.ChannelId == otherChannelId);
        remainingStatistics.Should().NotContain(s => s.ChannelId == targetChannelId);
    }

    [Fact]
    public async Task Handle_Should_PublishLisStatisticsChangedEvent_WhenStatisticsExist()
    {
        const int channelId = 3;
        var (publisherMock, handler) = CreateHandler(DbContext);

        DbContext.LisStatistics.Add(new LisStatistics
        {
            ChannelId = channelId,
            LisStatisticsCategory = LisStatisticsCategory.Queries,
            SamplesCount = 1
        });
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await handler.Handle(new DeleteLisStatisticsCommand(channelId), TestContext.Current.CancellationToken);

        publisherMock.Verify(
            p => p.Publish(
                It.Is<LisStatisticsChangedEvent>(e => e.ChannelId == channelId),
                TestContext.Current.CancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_NotPublishLisStatisticsChangedEvent_WhenNoStatisticsFound()
    {
        const int channelId = 4;
        var (publisherMock, handler) = CreateHandler(DbContext);

        await handler.Handle(new DeleteLisStatisticsCommand(channelId), TestContext.Current.CancellationToken);

        publisherMock.Verify(
            p => p.Publish(
                It.IsAny<LisStatisticsChangedEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
