using Pharos.Contracts.Notifications;

namespace Pharos.Server.Identity.Infrastructure.Hubs;

internal interface ICurrentUserServerHub : IHub
{
    Task UserInfoChanged(Guid userId);
    Task AutoLogoutSettingsChanged();
}
