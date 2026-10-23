using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Pharos.Contracts.Notifications;
using Pharos.Server.Application;
using Pharos.Server.Infrastructure.Hubs;

namespace Pharos.Server.Identity.Infrastructure.Hubs;

[Authorize]
public class CurrentUserHub(
    IHubContext<CurrentUserHub, ICurrentUserHub> hubContext,
    ICurrentUserService currentUser,
    ILogger<CurrentUserHub> logger) : HubBase<CurrentUserHub, ICurrentUserHub>(hubContext, logger), ICurrentUserServerHub
{
    // We use the user ID as group name to ensure that each user is in their own group.
    protected override string DefaultGroup => currentUser.UserId?.ToString() ?? "user_error";

    public Task UserInfoChanged(Guid userId)
    {
        return HubContext.Clients.Group(userId.ToString()).UserInfoChanged();
    }

    Task ICurrentUserServerHub.AutoLogoutSettingsChanged()
    {
        return HubContext.Clients.All.AutoLogoutSettingsChanged();
    }
}
