using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pharos.Server.Infrastructure.Events;

namespace Pharos.Server.Infrastructure.Services;

internal sealed class UsbDriveMonitor(
    ILogger<UsbDriveMonitor> logger,
    IServiceScopeFactory serviceScopeFactory,
    IVolumeChangeEventSource eventSource) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        eventSource.VolumeChanged += OnVolumeChanged;
        eventSource.Start(stoppingToken);
        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        eventSource.VolumeChanged -= OnVolumeChanged;
        await base.StopAsync(cancellationToken);
    }

    private void OnVolumeChanged(ushort eventType, string driveName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(driveName))
            {
                logger.LogDebug("VolumeChangeEvent without DriveName; ignored.");
                return;
            }

            using var scope = serviceScopeFactory.CreateScope();
            var insertedHandlers = scope.ServiceProvider.GetServices<IDriveInsertedHandler>();
            var removedHandlers = scope.ServiceProvider.GetServices<IDriveRemovedHandler>();

            switch (eventType)
            {
                case 2: // Inserted
                    logger.LogInformation("USB drive insertion detected: {Drive}", driveName);
                    _ = Task.WhenAll(insertedHandlers.Select(h => h.OnDriveInsertedAsync(driveName, CancellationToken.None)));
                    break;
                case 3: // Removed
                    logger.LogInformation("USB drive removal detected: {Drive}", driveName);
                    _ = Task.WhenAll(removedHandlers.Select(h => h.OnDriveRemovedAsync(driveName, CancellationToken.None)));
                    break;
                default:
                    logger.LogDebug("Unhandled USB EventType {Type} for drive {Drive}", eventType, driveName);
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing USB volume change event.");
        }
    }
}