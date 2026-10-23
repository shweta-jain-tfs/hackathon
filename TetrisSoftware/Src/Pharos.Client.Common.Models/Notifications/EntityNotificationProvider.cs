using Microsoft.AspNetCore.SignalR.Client;
using Pharos.Contracts.Notifications;

namespace Pharos.Client.Common.Models.Notifications;

public abstract class EntityNotificationProvider<TModel, TKey> : HubNotificationProvider, IEntityNotificationProvider<TModel, TKey>
    where TModel : class, IEntityModel<TKey>, new()
{

    protected EntityNotificationProvider(IHubConnectionFactory hubConnectionFactory) : base(hubConnectionFactory)
    {
    }

    protected override void OnHubConnectionCreated(HubConnection hubConnection)
    {
        hubConnection.On<TKey>(nameof(IEntityHub.Created), OnCreated);
        hubConnection.On<TKey>(nameof(IEntityHub.Updated), OnUpdated);
        hubConnection.On<TKey>(nameof(IEntityHub.Deleted), OnDeleted);
    }

    private Task OnCreated(TKey id)
    {
        if (HasSubscribers)
        {
            var handler = GetHandler<Func<TKey, Task>>(nameof(Created));
            return handler?.Invoke(id) ?? Task.CompletedTask;
        }
        return Task.CompletedTask;
    }

    private Task OnDeleted(TKey id)
    {
        if (HasSubscribers)
        {
            var handler = GetHandler<Func<TKey, Task>>(nameof(Deleted));
            return handler?.Invoke(id) ?? Task.CompletedTask;
        }
        return Task.CompletedTask;
    }

    private Task OnUpdated(TKey id)
    {
        if (HasSubscribers)
        {
            var handler = GetHandler<Func<TKey, Task>>(nameof(Updated));
            return handler?.Invoke(id) ?? Task.CompletedTask;
        }
        return Task.CompletedTask;
    }

    public event Func<TKey, Task>? Created
    {
        add => Subscribe(nameof(Created), value);
        remove => Unsubscribe(nameof(Created), value);
    }

    public event Func<TKey, Task>? Updated
    {
        add => Subscribe(nameof(Updated), value);
        remove => Unsubscribe(nameof(Updated), value);
    }

    public event Func<TKey, Task>? Deleted
    {
        add => Subscribe(nameof(Deleted), value);
        remove => Unsubscribe(nameof(Deleted), value);
    }
}

public abstract class EntityNotificationProvider<TModel>
    (IHubConnectionFactory hubConnectionFactory)
    : EntityNotificationProvider<TModel, int>(hubConnectionFactory), IEntityNotificationProvider<TModel>
    where TModel : class, IEntityModel<int>, new();