using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Pharos.Contracts.Notifications;

namespace Pharos.Server.Integration.Tests.Common;
internal class HubConnectionHelper
{
    internal static async Task<HubConnection> SetupHubConnection(string hubName, WebApplicationFactory<Program> factory)
    {
        var server = factory.Server;
        var connection = await StartConnectionAsync(server.CreateHandler(), server.BaseAddress, hubName);

        connection.Closed += async error =>
        {
#pragma warning disable CA5394 // Do not use insecure randomness
            await Task.Delay(new Random().Next(0, 5) * 1000);
#pragma warning restore CA5394 // Do not use insecure randomness
            await connection.StartAsync();
        };

        await connection.InvokeAsync(nameof(IHub.Subscribe));

        return connection;
    }

    private static async Task<HubConnection> StartConnectionAsync(HttpMessageHandler handler, Uri serverBaseAddress, string hubName)
    {
        var url = serverBaseAddress + hubName;
        var hubConnection = new HubConnectionBuilder()
            .WithUrl(url, o =>
            {
                o.Transports = HttpTransportType.LongPolling;
                o.HttpMessageHandlerFactory = _ => handler;
            })
            .Build();

        await hubConnection.StartAsync();
        return hubConnection;
    }
}