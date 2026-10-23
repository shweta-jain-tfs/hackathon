using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace Pharos.Client.Common.Models.Services;

internal sealed class HubConnectionFactory : IHubConnectionFactory
{
    private readonly IWebAssemblyHostEnvironment _environment;

    public HubConnectionFactory(IWebAssemblyHostEnvironment environment)
    {
        _environment = environment;
    }

    public HubConnection CreateConnection(string hubRoute)
    {
        return new HubConnectionBuilder()
            .WithUrl(_environment.BaseAddress + hubRoute, HttpTransportType.WebSockets)
            .WithKeepAliveInterval(TimeSpan.FromSeconds(10))
            .WithServerTimeout(TimeSpan.FromSeconds(IHubConnectionFactory.ServerTimeoutSeconds))
            .WithAutomaticReconnect(new HubConnectionRetryPolicy())
            .Build();
    }
}