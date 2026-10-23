namespace Pharos.Client.Common.Models.Notifications;

public abstract class HubRefreshNotificationProvider : HubNotificationProvider
{
    protected HubRefreshNotificationProvider(IHubConnectionFactory hubConnectionFactory) : base(hubConnectionFactory)
    {
    }

    public event EventHandler RefreshNeeded
    {
        add => Subscribe(nameof(RefreshNeeded), value);
        remove => Unsubscribe(nameof(RefreshNeeded), value);
    }

    protected void OnRefreshNeeded()
    {
        GetHandler<EventHandler>(nameof(RefreshNeeded))?.Invoke(this, EventArgs.Empty);
    }
}