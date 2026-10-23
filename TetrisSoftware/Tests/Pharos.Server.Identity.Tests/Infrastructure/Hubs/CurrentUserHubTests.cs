using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Pharos.Contracts.Notifications;
using Pharos.Server.Application;

namespace Pharos.Server.Identity.Infrastructure.Hubs;

public class CurrentUserHubTests
{
    [Fact]
    public async Task AutoLogoutSettingsChanged_BroadcastsToAllClientsThroughServerInterface()
    {
        var clients = new Mock<ICurrentUserHub>();
        var context = new Mock<IHubContext<CurrentUserHub, ICurrentUserHub>>();
        context.Setup(x => x.Clients.All).Returns(clients.Object);
        using var hub = new CurrentUserHub(context.Object, Mock.Of<ICurrentUserService>(),
            NullLogger<CurrentUserHub>.Instance);

        await ((ICurrentUserServerHub)hub).AutoLogoutSettingsChanged();

        clients.Verify(x => x.AutoLogoutSettingsChanged(), Times.Once);
        // Clients must not be able to invoke a hub method that resets every user's inactivity timer.
        typeof(CurrentUserHub).GetMethod(nameof(ICurrentUserServerHub.AutoLogoutSettingsChanged)).Should().BeNull();
    }
}
