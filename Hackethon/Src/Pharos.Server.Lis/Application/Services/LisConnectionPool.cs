using System.Collections.Concurrent;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Settings;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Lis.Application.Commands.LisStatisticsCommands;
using Pharos.Server.Lis.Application.Events;
using Pharos.Server.Lis.Application.Interfaces;

namespace Pharos.Server.Lis.Application.Services;

/// <summary>
/// Represents a pool of existing LIS connections.
/// </summary>
internal sealed class LisConnectionPool(
    IOptionsMonitor<LisSettings> lisSettings,
    ILisConnectionFactory connectionFactory,
    IServiceScopeFactory scopeFactory,
    ILogger<LisConnectionPool> logger) : BackgroundService, ILisConnectionPool
{
    public sealed class PooledLisConnection(ILisConnection connection)
    {
        private volatile bool _isLocked;

        public ILisConnection Connection { get; } = connection;

        public bool IsLocked
        {
            get => _isLocked;
            set => _isLocked = value;
        }
    }

    private readonly ConcurrentDictionary<int, List<PooledLisConnection>> _poolItems = new();
    private readonly object _lockRoot = new();

    public event EventHandler<ILisConnection>? ConnectionAdded;
    public event EventHandler<ILisConnection>? ConnectionRemoved;

    public IEnumerable<int> LisChannels => _poolItems.Keys;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Starting LIS connection pool background service.");

        await StartConnectionsAsync(lisSettings.CurrentValue.LisChannels);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Stopping LIS connection pool background service.");

        await base.StopAsync(cancellationToken);

        await RemoveAndCloseConnections(cancellationToken);
    }

    public IReadOnlyList<LisConnectionStatus> GetConnectionStates(int lisChannelId)
    {
        if (!_poolItems.TryGetValue(lisChannelId, out var poolItems))
        {
            return [new(LisConnectionRole.Both, LisConnectionState.Closed)];
        }

        return poolItems.Select(x => new LisConnectionStatus(x.Connection.Role, x.Connection.State)).ToList();
    }

    public ILisConnection? LockConnection(int lisChannelId, bool useImportConnection = false)
    {
        logger.LogDebug("Attempting to lock a connection for the LIS channel ID={LisChannelId}", lisChannelId);
        lock (_lockRoot)
        {
            if (!_poolItems.TryGetValue(lisChannelId, out var poolItems))
            {
                logger.LogError("Connection for the LIS channel ID={LisChannelId} is missing from the pool.", lisChannelId);
                return null;
            }

            var role = useImportConnection ? LisConnectionRole.Import : LisConnectionRole.Export;
            var poolItem = poolItems.FirstOrDefault(x => x.Connection.Role.HasFlag(role));
            if (poolItem == null)
            {
                logger.LogError("No {Role} connection found for LIS channel ID={LisChannelId}.", role, lisChannelId);
                return null;
            }

            return TryLockConnection(poolItem);
        }
    }

    public void UnlockConnection(ILisConnection lisConnection)
    {
        logger.LogDebug("Attempting to unlock a connection.");
        lock (_lockRoot)
        {
            var poolItem = _poolItems.Values
               .SelectMany(x => x)
               .FirstOrDefault(x => x.Connection == lisConnection);

            if (poolItem != null)
            {
                poolItem.IsLocked = false;
                logger.LogDebug("Unlocked a connection for the LIS channel ID={LisChannelId}", lisConnection.LisChannelId);
            }
        }
    }

    private async Task StartConnectionsAsync(List<LisChannel> lisChannels)
    {
        var hasErrors = false;

        for (var i = 0; i < lisChannels.Count; i++)
        {
            var channel = lisChannels[i];

            if (!channel.IsEnabled)
            {
                continue;
            }

            var connection = connectionFactory.CreateConnection(i, channel, LisConnectionRole.Import);
            var opened = await OpenConnectionAsync(i, connection, channel.ProtocolType);
            hasErrors |= !opened;

            if (!connection.Role.HasFlag(LisConnectionRole.Both))
            {
                var exportConnection = connectionFactory.CreateConnection(i, channel, LisConnectionRole.Export);
                var exportOpened = await OpenConnectionAsync(i, exportConnection, channel.ProtocolType);
                hasErrors |= !exportOpened;
            }
        }

        await PublishLisConnectionStateChangedAsync();

        if (hasErrors)
        {
            logger.LogInformation("LIS connection pool was started with errors.");
        }
        else
        {
            logger.LogInformation("LIS connection pool was started successfully");
        }
    }

    private async Task<bool> OpenConnectionAsync(int lisChannelId, ILisConnection lisConnection, LisProtocolType lisProtocolType)
    {
        var opened = false;
        try
        {
            opened = await lisConnection.OpenAsync();
            if (!opened)
            {
                logger.LogError("Failed to open {ProtocolType} connection for LIS channel ID={LisChannelId}, connection role={Role}.",
                    lisProtocolType, lisChannelId, lisConnection.Role);
            }
        }
        catch (Exception e)
        {
            // We suppress the exception here because we want the application
            // to start even if the LIS connection cannot be opened.
            logger.LogError(e, "Error opening {ProtocolType} connection for LIS channel ID={LisChannelId}, connection role={Role}.",
                lisProtocolType, lisChannelId, lisConnection.Role);
        }

        lisConnection.StateChanged += OnConnectionStateChanged;
        _poolItems.GetOrAdd(lisChannelId, _ => []).Add(new PooledLisConnection(lisConnection));
        ConnectionAdded?.Invoke(this, lisConnection);

        return opened;
    }

    private ILisConnection? TryLockConnection(PooledLisConnection poolItem)
    {
        if (poolItem.IsLocked)
        {
            logger.LogDebug("Connection for the LIS channel ID={LisChannelId} is already locked for another transmission.", poolItem.Connection.LisChannelId);
            return null;
        }

        if (poolItem.Connection.State is LisConnectionState.Connecting)
        {
            logger.LogDebug("Cannot lock connection in the {LisConnectionState} state, LIS channel ID={LisChannelId}.", poolItem.Connection.State, poolItem.Connection.LisChannelId);
            return null;
        }

        if (poolItem.Connection.State is LisConnectionState.Broken)
        {
            logger.LogDebug("Detected broken connection for LIS channel ID={LisChannelId}. And closing the connection.", poolItem.Connection.LisChannelId);
            poolItem.Connection.Close();
        }

        poolItem.IsLocked = true;
        logger.LogDebug("Connection is successfully locked for LIS channel ID={LisChannelId}", poolItem.Connection.LisChannelId);

        return poolItem.Connection;
    }

    private async Task RemoveAndCloseConnections(CancellationToken cancellationToken)
    {
        foreach (var channelId in _poolItems.Keys)
        {
            if (!_poolItems.TryRemove(channelId, out var poolItems))
            {
                continue;
            }

            foreach (var poolItem in poolItems)
            {
                ConnectionRemoved?.Invoke(this, poolItem.Connection);
                if (poolItem.IsLocked)
                {
                    logger.LogInformation("The connection for the LIS channel ID={LisChannelId}, connection role={Role} is locked. Waiting before closing...", channelId, poolItem.Connection.Role);
                    await Task.Delay(2000, cancellationToken);
                }

                try
                {
                    poolItem.Connection.Close();
                    poolItem.Connection.StateChanged -= OnConnectionStateChanged;
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Failed to close the connection for the LIS channel ID={LisChannelId}, connection role={Role}.", channelId, poolItem.Connection.Role);
                }
            }
        }
    }

    private async void OnConnectionStateChanged(object? sender, LisConnectionStateChangedEventArgs e)
    {
        await PublishLisConnectionStateChangedAsync();
    }

    private async Task PublishLisConnectionStateChangedAsync()
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();
            await publisher.Publish(new LisConnectionStateChangedEvent());
        }
        catch (Exception error)
        {
            logger.LogError(error, "Failed to publish LIS connection state change event.");
        }
    }

    internal class LisSettingsChangedEventHandler(
        ILisConnectionPool connectionPool,
        ISender sender,
        ILogger<LisSettingsChangedEventHandler> logger) : INotificationHandler<SettingsChangedEvent<LisSettings>>
    {
        public async Task Handle(SettingsChangedEvent<LisSettings> notification, CancellationToken cancellationToken)
        {
            var pool = (LisConnectionPool)connectionPool;
            logger.LogInformation("Restarting LIS connections with new settings");

            await pool.RemoveAndCloseConnections(cancellationToken);
            await DeleteChannelSnapshotsAsync(notification.OldValue.LisChannels, notification.NewValue.LisChannels, cancellationToken);
            await pool.StartConnectionsAsync(notification.NewValue.LisChannels);
        }

        private async Task DeleteChannelSnapshotsAsync(List<LisChannel> oldChannels, List<LisChannel> newChannels, CancellationToken cancellationToken)
        {
            if (newChannels.Count >= oldChannels.Count)
            {
                return;
            }
            var newChannelNames = newChannels.Select(c => c.Name).ToHashSet(StringComparer.Ordinal);
            for (var i = 0; i < oldChannels.Count; i++)
            {
                if (!newChannelNames.Contains(oldChannels[i].Name))
                {
                    logger.LogInformation("Deleting statistics for deleted LIS channel ID={LisChannelId}.", i);
                    await sender.Send(new DeleteLisStatisticsCommand(i), cancellationToken);
                }
            }
        }
    }
}
