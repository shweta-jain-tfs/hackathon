using Microsoft.AspNetCore.SignalR.Client;
using Pharos.Contracts.Notifications;

namespace Pharos.Client.Common.Models.Notifications;

/// <summary>
/// An abstract notification provider that maintains a SignalR connections and
/// subscribes (calls <see cref="IHub.Subscribe"/>) to a specified hub.
/// </summary>
public abstract class HubNotificationProvider : NotificationProviderBase
{
    private readonly IHubConnectionFactory _hubConnectionFactory;
    private HubConnection? _hubConnection;

    protected HubNotificationProvider(IHubConnectionFactory hubConnectionFactory)
    {
        _hubConnectionFactory = hubConnectionFactory;
    }

    protected abstract string HubRoute { get; }

    protected sealed override void OnSubscribing(object notification, Delegate? value)
    {
        if (_hubConnection == null)
        {
            _hubConnection = _hubConnectionFactory.CreateConnection(HubRoute);
            _hubConnection.Closed += HubConnectionClosed;
            _hubConnection.Reconnected += HubConnectionReconnected;
            OnHubConnectionCreated(_hubConnection);
        }

        OpenHubConnection().ConfigureAwait(false);
    }

    protected sealed override void OnUnsubscribed(object notification, Delegate? value)
    {
        if (HasSubscribers || _hubConnection == null)
        {
            return;
        }

        _hubConnection.Closed -= HubConnectionClosed;

        if (_hubConnection.State != HubConnectionState.Disconnected)
        {
            try
            {
                _hubConnection.StopAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                OnError(e);
            }
            finally
            {
                _hubConnection = null;
            }
        }
        else
        {
            _hubConnection = null;
        }
    }

    protected virtual void OnHubConnectionCreated(HubConnection hubConnection) { }

    private Task HubConnectionReconnected(string? connectionId)
    {
        return SubscribeToHub();
    }

    private Task HubConnectionClosed(Exception? exception)
    {
        if (exception != null)
        {
            OnError(exception);
        }

        return Task.CompletedTask;
    }

    private async Task OpenHubConnection()
    {
        if (_hubConnection == null || _hubConnection.State != HubConnectionState.Disconnected)
        {
            return;
        }

        try
        {
            await _hubConnection.StartAsync();
            await SubscribeToHub();
        }
        catch (Exception e)
        {
            OnError(e);
        }
    }

    private Task SubscribeToHub()
    {
        return _hubConnection?.InvokeAsync(nameof(IHub.Subscribe)) ?? Task.CompletedTask;
    }
}