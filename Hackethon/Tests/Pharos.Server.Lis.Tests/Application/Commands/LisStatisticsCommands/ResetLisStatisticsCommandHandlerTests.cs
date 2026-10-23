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
[Trait("ProductRequirement", "PR_564")]
[Trait("ModuleRequirement", "MR_5865")]
public class ResetLisStatisticsCommandHandlerTests(LisTestsWaf factory) : DatabaseTestBase<LisTestsWaf>(factory)
{
    private ILisDbContext DbContext => ServiceProvider.GetRequiredService<ILisDbContext>();

    private static (Mock<IPublisher> PublisherMock, ResetLisStatisticsCommandHandler Handler)
        CreateHandler(ILisDbContext dbContext)
    {
        var publisherMock = new Mock<IPublisher>();
        var handler = new ResetLisStatisticsCommandHandler(dbContext, publisherMock.Object);
        return (publisherMock, handler);
    }

    [Fact]
    public async Task Handle_Should_ResetAllStatisticsFieldsToDefault_When_ChannelHasStatistics()
    {
        // Arrange
        const int targetChannelId = 1;
        var (_, handler) = CreateHandler(DbContext);

        DbContext.LisStatistics.AddRange(
            new LisStatistics
            {
                ChannelId = targetChannelId,
                LisStatisticsCategory = LisStatisticsCategory.Queries,
                SamplesCount = 10,
                TestsCount = 5,
                LatestSampleId = "S001",
                LatestTime = DateTimeOffset.UtcNow
            },
            new LisStatistics
            {
                ChannelId = targetChannelId,
                LisStatisticsCategory = LisStatisticsCategory.Imported,
                SamplesCount = 20,
                TestsCount = 30,
                LatestSampleId = "S002",
                LatestTime = DateTimeOffset.UtcNow
            });
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        await handler.Handle(new ResetLisStatisticsCommand(targetChannelId), TestContext.Current.CancellationToken);

        // Assert
        var statistics = await DbContext.LisStatistics
            .AsNoTracking()
            .Where(s => s.ChannelId == targetChannelId)
            .ToListAsync(TestContext.Current.CancellationToken);

        statistics.Should().AllSatisfy(s =>
        {
            s.SamplesCount.Should().Be(0);
            s.LatestSampleId.Should().BeNull();
            s.LatestTime.Should().BeNull();
        });

        statistics.Where(s => s.LisStatisticsCategory != LisStatisticsCategory.Queries)
            .Should().AllSatisfy(s => s.TestsCount.Should().Be(0));

        statistics.Where(s => s.LisStatisticsCategory == LisStatisticsCategory.Queries)
            .Should().AllSatisfy(s => s.TestsCount.Should().BeNull());
    }

    [Fact]
    public async Task Handle_Should_NotResetStatistics_ForOtherChannels()
    {
        // Arrange
        const int targetChannelId = 1;
        const int otherChannelId = 2;
        const int otherSamplesCount = 15;
        const int otherTestsCount = 8;
        const string otherLatestSampleId = "S999";

        var (_, handler) = CreateHandler(DbContext);

        DbContext.LisStatistics.AddRange(
            new LisStatistics
            {
                ChannelId = targetChannelId,
                LisStatisticsCategory = LisStatisticsCategory.Queries,
                SamplesCount = 5,
                TestsCount = 3
            },
            new LisStatistics
            {
                ChannelId = otherChannelId,
                LisStatisticsCategory = LisStatisticsCategory.Exported,
                SamplesCount = otherSamplesCount,
                TestsCount = otherTestsCount,
                LatestSampleId = otherLatestSampleId
            });
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        await handler.Handle(new ResetLisStatisticsCommand(targetChannelId), TestContext.Current.CancellationToken);

        // Assert
        var otherChannelStats = await DbContext.LisStatistics
            .AsNoTracking()
            .Where(s => s.ChannelId == otherChannelId)
            .SingleAsync(TestContext.Current.CancellationToken);

        otherChannelStats.SamplesCount.Should().Be(otherSamplesCount);
        otherChannelStats.TestsCount.Should().Be(otherTestsCount);
        otherChannelStats.LatestSampleId.Should().Be(otherLatestSampleId);
    }

    [Fact]
    public async Task Handle_Should_PublishLisStatisticsChangedEvent_With_CorrectChannelId()
    {
        // Arrange
        const int channelId = 3;
        var (publisherMock, handler) = CreateHandler(DbContext);

        // Act
        await handler.Handle(new ResetLisStatisticsCommand(channelId), TestContext.Current.CancellationToken);

        // Assert
        publisherMock.Verify(
            p => p.Publish(
                It.Is<LisStatisticsChangedEvent>(e => e.ChannelId == channelId),
                TestContext.Current.CancellationToken),
            Times.Once);
    }
}

