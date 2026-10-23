using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Server.Application.Lis;
using Pharos.Server.Domain;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Lis.Application.Interfaces;
using Pharos.Server.Lis.Application.Services;
using Pharos.Server.Lis.Domain;

namespace Pharos.Server.Integration.Tests;

[Trait("Category", "Integration")]
[Collection(nameof(IntegrationTestsCollection))]
public class RequestTestOrdersCommandTests : IntegrationTestBase
{
    private readonly Fixture _fixture;

    public RequestTestOrdersCommandTests(IntegrationTestsWaf factory) : base(factory)
    {
        _fixture = new Fixture();
    }

    [Fact]
    public async Task CommandPutsInfoRequestLisMessageIntoQueue()
    {
        var dispatcher = ServiceProvider.GetRequiredService<LisMessageDispatcher>();
        dispatcher.SuspendDispatching = true;
        var channelId = 0;
        try
        {
            var mediator = ServiceProvider.GetService<IMediator>()!;
            var queue = ServiceProvider.GetService<ILisMessageQueue>()!;
            var firstItemBeforePost = await queue.PeekAsync(channelId, LisDispatchMode.All);
            var sampleId = _fixture.Create<string>()[..Constants.SampleIdMaxLength];

            await mediator.Send(new AskForMissingTestOrdersCommand([sampleId], false), TestContext.Current.CancellationToken);

            var item = await queue.PeekAsync(channelId, LisDispatchMode.All);
            item.Should().NotBeNull();
            item.Should().NotBeSameAs(firstItemBeforePost);
            await queue.DequeueAsync(item!.MessageId);

            item.MessageData.Should().Contain(sampleId);
        }
        finally
        {
            dispatcher.SuspendDispatching = false;
        }
    }
}