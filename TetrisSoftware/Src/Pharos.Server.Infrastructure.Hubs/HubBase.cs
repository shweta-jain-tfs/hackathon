using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Pharos.Contracts.Notifications;

namespace Pharos.Server.Infrastructure.Hubs;

/// <summary>
/// Base class for permission-based notification hubs. Non-abstract classes must be marked with
/// the Pharos.Server.Application.Security.AuthorizeAttribute attribute.
/// </summary>
/// <typeparam name="THub">The hub.</typeparam>
/// <typeparam name="TNotification">The interface for the client calls.</typeparam>
public abstract class HubBase<THub, TNotification> : Hub<TNotification>, IHub
    where THub : Hub<TNotification>
    where TNotification : class
{
    protected HubBase(
        IHubContext<THub, TNotification> hubContext,
        ILogger<HubBase<THub, TNotification>> logger)
    {
        HubContext = hubContext;
        Logger = logger;
    }

    /// <summary>
    /// The hub context is used for sending messages to clients outside the hub.
    /// </summary>
    protected IHubContext<THub, TNotification> HubContext { get; }

    /// <summary>
    /// Gets the default group name for client connections. All client connections are added to this group
    /// after authorization. Descendants must use Clients.Group(DefaultGroup) for sending messages to clients.
    /// Otherwise, the data may be exposed to the users that are not authorized to view it.
    /// </summary>
    protected abstract string DefaultGroup { get; }

    protected ILogger<HubBase<THub, TNotification>> Logger { get; }

    public async Task Subscribe()
    {
        Logger.LogInformation("Subscribing a client to the hub notifications. User ID = {UserId}, Group = {HubGroup}.", Context.UserIdentifier, DefaultGroup);

        await AddClientToGroup();
    }

    protected virtual Task AddClientToGroup()
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, DefaultGroup);
    }
}
