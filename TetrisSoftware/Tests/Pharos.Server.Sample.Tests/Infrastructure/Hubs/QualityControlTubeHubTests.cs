using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Pharos.Contracts.Notifications;
using Pharos.Server.Infrastructure.Hubs;
using Pharos.Server.Sample.Infrastructure.Hubs;

namespace Pharos.Server.Sample.Tests.Infrastructure.Hubs;

public class QualityControlTubeHubTests
{
    [Fact]
    public async Task TubeUpdated_BroadcastsTubeIdToSubscribedClients()
    {
        var clients = new Mock<IQualityControlTubeHub>();
        var context = new Mock<IHubContext<QualityControlTubeHub, IQualityControlTubeHub>>();
        context.Setup(x => x.Clients.Group("qualityControlTube_default")).Returns(clients.Object);
        clients.Setup(x => x.TubeUpdated("TUBE1")).Returns(Task.CompletedTask);
        using var hub = new QualityControlTubeHub(context.Object,
            NullLogger<HubBase<QualityControlTubeHub, IQualityControlTubeHub>>.Instance);

        await hub.TubeUpdated("TUBE1");

        clients.Verify(x => x.TubeUpdated("TUBE1"), Times.Once);
        clients.VerifyNoOtherCalls();
    }
}
