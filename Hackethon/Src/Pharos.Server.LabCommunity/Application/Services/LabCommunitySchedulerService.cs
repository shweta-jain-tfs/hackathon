using Microsoft.Extensions.Options;
using Pharos.Server.LabCommunity.Application.Interfaces;
using Pharos.Server.LabCommunity.Domain;

namespace Pharos.Server.LabCommunity.Application.Services;

internal sealed class LabCommunitySchedulerService(
    IOptions<LabCommunityDefaultConfiguration> defaultConfiguration,
    ILabCommunityDbContext dbContext,
    IEnumerable<ILabCommunityJobScheduler> schedulers) : ILabCommunitySchedulerService
{
    public async Task ConfigureAllFromCurrentConfiguration(
        string laboratoryCode,
        CancellationToken cancellationToken)
    {
        var persistedConfiguration = await dbContext.GetConfigurationAsync(laboratoryCode, cancellationToken);

        await ConfigureAll(persistedConfiguration, cancellationToken);
    }

    public async Task ReconfigureAfterConfigurationPull(
        Configuration? newConfiguration,
        Configuration? previousConfiguration,
        CancellationToken cancellationToken)
    {
        foreach (var scheduler in schedulers.Where(scheduler => scheduler.ShouldReconfigureAfterConfigurationPull(newConfiguration, previousConfiguration)))
        {
            await ConfigureOrStop(scheduler, newConfiguration, useInitialDelay: true, cancellationToken);
        }
    }

    public async Task StopAll(CancellationToken cancellationToken)
    {
        foreach (var scheduler in schedulers)
        {
            await scheduler.StopScheduler(cancellationToken);
        }
    }

    private async Task ConfigureAll(
        Configuration? configuration,
        CancellationToken cancellationToken)
    {
        foreach (var scheduler in schedulers)
        {
            await ConfigureOrStop(scheduler, configuration, useInitialDelay: false, cancellationToken);
        }
    }

    private async Task ConfigureOrStop(
        ILabCommunityJobScheduler scheduler,
        Configuration? configuration,
        bool useInitialDelay,
        CancellationToken cancellationToken)
    {
        if (scheduler.ShouldStop(defaultConfiguration.Value, configuration))
        {
            await scheduler.StopScheduler(cancellationToken);
            return;
        }

        var interval = scheduler.ResolveInterval(defaultConfiguration.Value, configuration);
        await scheduler.ConfigureScheduler(
            interval,
            cancellationToken,
            useInitialDelay ? interval : default);
    }
}
