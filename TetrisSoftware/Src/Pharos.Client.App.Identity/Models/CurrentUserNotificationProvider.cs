using Microsoft.AspNetCore.SignalR.Client;
using Pharos.Client.Common.Models.Notifications;
using Pharos.Client.Common.Models.Services;
using Pharos.Contracts.Notifications;

namespace Pharos.Client.App.Identity.Models;
internal class CurrentUserNotificationProvider : HubNotificationProvider, ICurrentUserNotificationProvider
{
    public CurrentUserNotificationProvider(IHubConnectionFactory hubConnectionFactory) : base(hubConnectionFactory)
    {
    }

    protected override string HubRoute => HubRoutes.CurrentUser;

    protected override void OnHubConnectionCreated(HubConnection hubConnection)
    {
        hubConnection.On(nameof(ICurrentUserHub.UserInfoChanged), OnUserInfoChanged);
        hubConnection.On(nameof(ICurrentUserHub.AutoLogoutSettingsChanged), OnAutoLogoutSettingsChanged);
        hubConnection.Reconnected += _ => OnAutoLogoutSettingsChanged();
    }

    private Task OnUserInfoChanged()
    {
        return GetHandler<Func<Task>>(nameof(UserInfoChanged))?.Invoke() ?? Task.CompletedTask;
    }

    public event Func<Task>? UserInfoChanged
    {
        add => Subscribe(nameof(UserInfoChanged), value);
        remove => Unsubscribe(nameof(UserInfoChanged), value);
    }

    private Task OnAutoLogoutSettingsChanged()
    {
        var handlers = GetHandler<Func<Task>>(nameof(AutoLogoutSettingsChanged));
        return handlers is null
            ? Task.CompletedTask
            : Task.WhenAll(handlers.GetInvocationList().Cast<Func<Task>>().Select(handler => handler()));
    }

    public event Func<Task>? AutoLogoutSettingsChanged
    {
        add => Subscribe(nameof(AutoLogoutSettingsChanged), value);
        remove => Unsubscribe(nameof(AutoLogoutSettingsChanged), value);
    }
}
