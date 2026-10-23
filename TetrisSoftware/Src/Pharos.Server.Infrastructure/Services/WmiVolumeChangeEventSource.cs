using System.Management;
using Microsoft.Extensions.Logging;
using Pharos.Server.Infrastructure.Events;

namespace Pharos.Server.Infrastructure.Services;

internal sealed class WmiVolumeChangeEventSource(ILogger<WmiVolumeChangeEventSource> logger)
    : IVolumeChangeEventSource
{
    private ManagementEventWatcher? _watcher;

    public event Action<ushort, string>? VolumeChanged;

    public void Start(CancellationToken stoppingToken)
    {
        if (_watcher is not null)
        {
            return;
        }

        try
        {
#pragma warning disable CA1416
            var query = new WqlEventQuery("SELECT * FROM Win32_VolumeChangeEvent WHERE EventType = 2 OR EventType = 3");
            _watcher = new ManagementEventWatcher(query);

            _watcher.EventArrived += OnArrived;
            _watcher.Start();
#pragma warning restore CA1416

            logger.LogInformation("Started Management WMI volume watcher.");

            stoppingToken.Register(Dispose);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start WmiVolumeChangeEventSource.");
            Dispose();
        }
    }

    private void OnArrived(object? sender, EventArrivedEventArgs args)
    {
        try
        {
#pragma warning disable CA1416
            var eventType = (ushort?)(args.NewEvent?["EventType"]);
            var driveName = args.NewEvent?["DriveName"] as string;
#pragma warning disable CA1416

            if (eventType is null || string.IsNullOrWhiteSpace(driveName))
            {
                return;
            }

            VolumeChanged?.Invoke(eventType.Value, driveName);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Error translating WMI event.");
        }
    }

    public void Dispose()
    {
        try
        {
            _watcher?.Stop();
            _watcher?.Dispose();
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Error disposing WmiVolumeChangeEventSource.");
        }
        finally
        {
            _watcher = null;
        }
    }
}