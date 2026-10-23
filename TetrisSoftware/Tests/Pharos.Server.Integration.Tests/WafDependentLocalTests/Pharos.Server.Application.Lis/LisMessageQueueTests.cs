using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Lis.Application.Interfaces;
using Pharos.Server.Lis.Domain;

namespace Pharos.Server.Integration.Tests.WafDependentLocalTests.Pharos.Server.Application.Lis;

[Trait("Category", "Integration")]
[Collection(nameof(IntegrationTestsCollection))]
public class LisMessageQueueTests : IntegrationTestBase
{
    private readonly DateTimeOffset _testDateTime = DateTimeOffset.Now;

    public LisMessageQueueTests(IntegrationTestsWaf factory) : base(factory)
    {
    }

    [Fact]
    public async Task PeekAsync()
    {
        var originalMessage = new QueuedLisMessage()
        {
            ChannelId = 1,
            MessageType = LisMessageType.SampleTestResult,
            Created = _testDateTime,
            MessageData = "SomeMessageData"
        };

        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var queue = scope.ServiceProvider.GetService<ILisMessageQueue>()!;
            await queue.EnqueueAsync(originalMessage);
        }

        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var queue = scope.ServiceProvider.GetService<ILisMessageQueue>()!;
            var item = await queue.PeekAsync(originalMessage.ChannelId, LisDispatchMode.All);
            item.Should().NotBeNull();
            item.Should().NotBeSameAs(originalMessage);
            item!.ChannelId.Should().Be(originalMessage.ChannelId);
            item.MessageData.Should().Be(originalMessage.MessageData);
            item.Created.Should().Be(originalMessage.Created);
            item.MessageType.Should().Be(originalMessage.MessageType);
        }
    }

    [Fact]
    public async Task DequeueAsync()
    {
        var queue = ServiceProvider.GetService<ILisMessageQueue>()!;

        var channelId = 1;
        var beforeEnqueue = await queue.PeekAsync(channelId, LisDispatchMode.All);

        var message = new QueuedLisMessage()
        {
            MessageId = Guid.NewGuid(),
            ChannelId = channelId,
            MessageType = LisMessageType.SampleTestResult,
            Created = _testDateTime,
            MessageData = "SomeMessageData"
        };
        await queue.EnqueueAsync(message);

        var afterEnqueue = await queue.PeekAsync(channelId, LisDispatchMode.All);
        afterEnqueue.Should().NotBeSameAs(beforeEnqueue);
        afterEnqueue.Should().NotBeNull();

        await queue.DequeueAsync(afterEnqueue!.MessageId);

        var afterDequeue = await queue.PeekAsync(channelId, LisDispatchMode.All);
        afterDequeue.Should().BeSameAs(beforeEnqueue);
    }

    [Fact]
    public async Task MessagesOrderedByPriorityThenPostingTime()
    {
        var dbContext = ServiceProvider.GetRequiredService<ILisDbContext>();
        var queue = ServiceProvider.GetRequiredService<ILisMessageQueue>();
        var ids = new Guid[3];
        var channelId = 1;

        ids[0] = await queue.EnqueueAsync(new QueuedLisMessage
        {
            MessageId = Guid.NewGuid(),
            ChannelId = channelId,
            Priority = 2,
            Created = DateTimeOffset.Now,
            MessageData = "M2",
            MessageType = LisMessageType.SampleTestResult
        });
        ids[1] = await queue.EnqueueAsync(new QueuedLisMessage
        {
            MessageId = Guid.NewGuid(),
            ChannelId = channelId,
            Priority = 3,
            Created = DateTimeOffset.Now,
            MessageData = "M3",
            MessageType = LisMessageType.SampleTestResult
        });
        ids[2] = await queue.EnqueueAsync(new QueuedLisMessage
        {
            MessageId = Guid.NewGuid(),
            ChannelId = channelId,
            Priority = 1,
            Created = DateTimeOffset.Now,
            MessageData = "M1",
            MessageType = LisMessageType.TestOrderQuery
        });
        var items = await dbContext.QueuedLisMessages
                                   .Where(i => ids.Contains(i.MessageId))
                                   .OrderBy(i => i.Id)
                                   .ToListAsync(TestContext.Current.CancellationToken);
        Assert.True(items[0].Created < items[1].Created && items[1].Created < items[2].Created);

        var item1 = await queue.PeekAsync(channelId, LisDispatchMode.All);
        item1!.MessageData.Should().Be("M1");
        await queue.DequeueAsync(item1.MessageId);

        var item2 = await queue.PeekAsync(channelId, LisDispatchMode.All);
        item2!.MessageData.Should().Be("M2");
        await queue.DequeueAsync(item2.MessageId);

        var item3 = await queue.PeekAsync(channelId, LisDispatchMode.All);
        item3!.MessageData.Should().Be("M3");
        await queue.DequeueAsync(item3.MessageId);
    }

}