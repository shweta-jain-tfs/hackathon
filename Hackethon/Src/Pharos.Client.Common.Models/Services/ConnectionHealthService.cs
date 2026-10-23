using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Pharos.Contracts.Notifications;

namespace Pharos.Client.Common.Models.Services;

internal class ConnectionHealthService : IConnectionHealthService
{
    private readonly ILogger<ConnectionHealthService> _logger;
    private readonly HubConnection _hubConnection;
    private DateTimeOffset? _disconnectTime;

    public ConnectionHealthService(IHubConnectionFactory hubConnectionFactory, ILogger<ConnectionHealthService> logger)
    {
        _logger = logger;
        _hubConnection = hubConnectionFactory.CreateConnection(HubRoutes.Health);
        _hubConnection.Reconnecting += OnReconnecting;
        _hubConnection.Reconnected += OnReconnected;
    }

    private Task OnReconnected(string? error)
    {
        return NotifyConnectionRestoredAsync();
    }

    private Task OnReconnecting(Exception? error)
    {
        return NotifyConnectionLostAsync(error, true);
    }

    public bool IsConnected => _hubConnection.State == HubConnectionState.Connected;

    public TimeSpan OfflineTime => _disconnectTime != null ? DateTimeOffset.Now - _disconnectTime.Value : TimeSpan.Zero;

    public event Func<Task>? ConnectionLost;

    public event Func<Task>? ConnectionRestored;

    public async Task<bool> StartAsync(CancellationToken token)
    {
        // Keep trying to until we can start or the token is canceled.
        while (true)
        {
            try
            {
                await _hubConnection.StartAsync(token);
                _logger.LogInformation("Started connection health monitoring.");
                await NotifyConnectionRestoredAsync();
                return true;
            }
            catch when (token.IsCancellationRequested)
            {
                return false;
            }
            catch (Exception ex)
            {
                await NotifyConnectionLostAsync(ex);
                await Task.Delay(5000, token);
            }
        }
    }

    public Task NotifyConnectionLostAsync(Exception? error)
    {
        return NotifyConnectionLostAsync(error, false);

    }

    private Task NotifyConnectionLostAsync(Exception? error, bool addServerTimeout)
    {
        if (_disconnectTime != null)
        {
            return Task.CompletedTask;
        }

        _disconnectTime = DateTimeOffset.Now;
        if (addServerTimeout)
        {
            _disconnectTime -= TimeSpan.FromSeconds(IHubConnectionFactory.ServerTimeoutSeconds);
        }

        _logger.LogError(error, "Connection to the server is lost.");
        return ConnectionLost?.Invoke() ?? Task.CompletedTask;
    }

    private Task NotifyConnectionRestoredAsync()
    {
        if (_disconnectTime == null)
        {
            return Task.CompletedTask;
        }

        _disconnectTime = null;
        _logger.LogInformation("Connection to the server is restored.");
        return ConnectionRestored?.Invoke() ?? Task.CompletedTask;
    }
}