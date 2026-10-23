using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pharos.Server.AuditTrail.Application.Interfaces;

namespace Pharos.Server.AuditTrail.Application.Services;
internal class AuditTrailCleanupBackgroundWorker(
    ILogger<AuditTrailCleanupBackgroundWorker> logger,
    IServiceProvider serviceProvider) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Trigger to run cleanup on the 1st day of the month at midnight
            // This invocation will be changed later when exact time is decided
            var now = DateTimeOffset.Now;
            var nextRun = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero).AddMonths(1);
            var timeUntilNextMonth = nextRun - now;

            logger.LogInformation("Audit trail cleanup scheduled after: {Time}", timeUntilNextMonth);

            try
            {
                // Only run the cleanup if its 1st day of the month
                await Task.Delay(timeUntilNextMonth, stoppingToken);
                await RunCleanup();
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    private async Task RunCleanup()
    {
        using var scope = serviceProvider.CreateScope();
        var cleanupService = scope.ServiceProvider.GetRequiredService<IAuditTrailCleanupService>();

        try
        {
            logger.LogInformation("Starting audit trail cleanup at {Time}", DateTimeOffset.Now);

            var success = await cleanupService.AuditTrailCleanupAsync();

            if (success)
            {
                logger.LogInformation("Audit trail cleanup completed successfully at {Time}", DateTimeOffset.Now);
            }
            else
            {
                logger.LogWarning("No records to delete. Process completed at {Time}", DateTimeOffset.Now);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Audit trail cleanup failed at {Time}", DateTimeOffset.Now);
        }
    }
}
