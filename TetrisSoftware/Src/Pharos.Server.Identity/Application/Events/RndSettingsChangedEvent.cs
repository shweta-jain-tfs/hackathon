using MediatR;
using Pharos.Contracts.Security;
using Pharos.Server.Application.Settings;
using Pharos.Server.Application.Settings.RnD;
using Pharos.Server.Identity.Infrastructure.Authorization;
using Pharos.Server.Identity.Infrastructure.Hubs;

namespace Pharos.Server.Identity.Application.Events;

internal class RndSettingsChangedEvent(
    IApplicationUserManager userManager,
    ICurrentUserServerHub currentUserHub) : INotificationHandler<SettingsChangedEvent<RndSettings>>
{
    public async Task Handle(SettingsChangedEvent<RndSettings> notification, CancellationToken cancellationToken)
    {
        var newValue = notification.NewValue.RndMode;
        var rndModeDisabled = newValue != notification.OldValue.RndMode && !newValue;
        if (rndModeDisabled)
        {
            // We need to revoke the RnD role from users, otherwise the claims will persist
            // and the clients will think they still have RnD access (even though the server checks the permissions correctly).
            var users = await userManager.GetUsersInRoleAsync(BuiltInRoles.RndUser);
            foreach (var user in users)
            {
                await userManager.RemoveFromRolesAsync(user, [BuiltInRoles.RndUser]);
                await currentUserHub.UserInfoChanged(user.Id);
            }
        }
    }
}