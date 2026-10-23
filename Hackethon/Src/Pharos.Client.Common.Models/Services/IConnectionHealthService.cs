namespace Pharos.Client.Common.Models.Services;

public interface IConnectionHealthService
{
    bool IsConnected { get; }
    TimeSpan OfflineTime { get; }

    event Func<Task>? ConnectionLost;
    event Func<Task>? ConnectionRestored;

    Task<bool> StartAsync(CancellationToken token);

    Task NotifyConnectionLostAsync(Exception? error);
}
