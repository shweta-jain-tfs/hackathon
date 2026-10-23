using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Pharos.Contracts.Notifications;

namespace Pharos.Server.Infrastructure.Hubs;

/// <summary>
/// A base hub for entity-related notifications.
/// </summary>
/// <typeparam name="TNotification">The interface for the client calls.</typeparam>
/// <typeparam name="THub">The hub.</typeparam>
/// <typeparam name="TEntityKey">The type of the entity id property.</typeparam>
/// <remarks>
/// It includes an explicit implementation of the <see cref="Pharos.Contracts.Notifications.IEntityHub"/> 
/// interface to expose hub's client interface as a service to the rest of the application.
/// See also https://learn.microsoft.com/en-us/aspnet/core/signalr/hubcontext
/// </remarks>
public abstract class EntityHub<THub, TNotification, TEntityKey> : HubBase<THub, TNotification>, IEntityHub<TEntityKey>
    where THub : Hub<TNotification>
    where TNotification : class, IEntityHub<TEntityKey>
{
    protected EntityHub(
        IHubContext<THub, TNotification> hubContext,
        ILogger<EntityHub<THub, TNotification, TEntityKey>> logger)
    : base(hubContext, logger)
    {
    }

    public Task Created(TEntityKey entityId)
    {
        if (HubContext == null)
        {
            Logger.LogError("An attempt to send messages to clients without initializing the hub context.");
            return Task.CompletedTask;
        }

        Logger.LogDebug("Sending 'Created' message to clients in the group {HubGroup}.", DefaultGroup);
        return HubContext.Clients.Group(DefaultGroup).Created(entityId);
    }

    public Task Deleted(TEntityKey entityId)
    {
        if (HubContext == null)
        {
            Logger.LogError("An attempt to send messages to clients without initializing the hub context.");
            return Task.CompletedTask;
        }

        Logger.LogDebug("Sending 'Deleted' message to clients in the group {HubGroup}.", DefaultGroup);
        return HubContext.Clients.Group(DefaultGroup).Deleted(entityId);
    }

    public Task Updated(TEntityKey entityId)
    {
        if (HubContext == null)
        {
            Logger.LogError("An attempt to send messages to clients without initializing the hub context.");
            return Task.CompletedTask;
        }

        Logger.LogDebug("Sending 'Updated' message to clients in the group {HubGroup}.", DefaultGroup);
        return HubContext.Clients.Group(DefaultGroup).Updated(entityId);
    }
}

public abstract class EntityHub<THub, TNotification> : EntityHub<THub, TNotification, int>
    where THub : Hub<TNotification>
    where TNotification : class, IEntityHub
{
    protected EntityHub(
        IHubContext<THub, TNotification> hubContext,
        ILogger<EntityHub<THub, TNotification>> logger)
        : base(hubContext, logger)
    {
    }
}