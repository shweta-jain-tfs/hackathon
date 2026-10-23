using MediatR;
using Pharos.Server.Application.Settings;
using Pharos.Server.Application.Settings.Security;
using Pharos.Server.Identity.Infrastructure.Hubs;

namespace Pharos.Server.Identity.Application.Events;

internal sealed class AutoLogoutSettingsChangedEventHandler(ICurrentUserServerHub currentUserHub)
    : INotificationHandler<SettingsChangedEvent<SecuritySettings>>
{
    public Task Handle(SettingsChangedEvent<SecuritySettings> notification, CancellationToken cancellationToken)
    {
        var oldSettings = notification.OldValue.AutoLogoutSettings;
        var newSettings = notification.NewValue.AutoLogoutSettings;
        return oldSettings.AutoLogoutEnabled != newSettings.AutoLogoutEnabled
            || oldSettings.AutoLogoutTimeoutMinutes != newSettings.AutoLogoutTimeoutMinutes
            ? currentUserHub.AutoLogoutSettingsChanged()
            : Task.CompletedTask;
    }
}
