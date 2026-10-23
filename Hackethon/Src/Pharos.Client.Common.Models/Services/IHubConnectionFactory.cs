using Microsoft.AspNetCore.SignalR.Client;
using Pharos.Contracts.Notifications;

namespace Pharos.Client.Common.Models.Services;

/// <summary>
/// Defines a SignalR hub connection factory.
/// </summary>
public interface IHubConnectionFactory
{
    /// <summary>
    /// The server timeout in seconds for hub connections.
    /// </summary>
    const int ServerTimeoutSeconds = 30;

    /// <summary>
    /// Creates an instance of <see cref="HubConnection"/> for a specified hub route.
    /// </summary>
    /// <param name="hubRoute">The route of the hub on the server. All hub routes are defined in the <see cref="HubRoutes"/> class.</param>
    /// <returns></returns>
    HubConnection CreateConnection(string hubRoute);
}