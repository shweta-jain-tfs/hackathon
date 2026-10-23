using Microsoft.AspNetCore.SignalR.Client;
using Pharos.Contracts.Notifications;

namespace Pharos.Client.Common.Models.Notifications;

public abstract class DetailsPageNotificationProvider<TKey> : HubNotificationProvider, IDetailsNotificationProvider<TKey>
{
    protected DetailsPageNotificationProvider(IHubConnectionFactory hubConnectionFactory) : base(hubConnectionFactory)
    {
    }

    public event EventHandler<TKey> ModelUpdated
    {
        add => Subscribe(nameof(ModelUpdated), value);
        remove => Unsubscribe(nameof(ModelUpdated), value);
    }

    public event EventHandler<TKey> ModelDeleted
    {
        add => Subscribe(nameof(ModelDeleted), value);
        remove => Unsubscribe(nameof(ModelDeleted), value);
    }

    protected override void OnHubConnectionCreated(HubConnection hubConnection)
    {
        hubConnection.On<TKey>(nameof(IEntityHub.Updated), OnUpdated);
        hubConnection.On<TKey>(nameof(IEntityHub.Deleted), OnDeleted);
    }

    private void OnUpdated(TKey id)
    {
        GetHandler<EventHandler<TKey>>(nameof(ModelUpdated))?.Invoke(this, id);
    }

    private void OnDeleted(TKey id)
    {
        GetHandler<EventHandler<TKey>>(nameof(ModelDeleted))?.Invoke(this, id);
    }
}