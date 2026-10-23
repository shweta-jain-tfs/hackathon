using MediatR;
using Pharos.Server.LabCommunity.Application.Commands;
using Pharos.Server.LabCommunity.Application.Interfaces;
using Pharos.Server.LabCommunity.Domain;
using Quartz;

namespace Pharos.Server.LabCommunity.Application.Jobs;

[DisallowConcurrentExecution]
internal sealed class CheckInJob(ISender sender) : ILabCommunityJob
{
    public static JobKey Key => new("labcommunity-check-in");

    public static TriggerKey Trigger => new("labcommunity-check-in-recurring-trigger");

    public static bool ShouldReconfigureAfterConfigurationPull(Configuration? newConfiguration, Configuration? previousConfiguration)
    {
        return newConfiguration is not null
               && (previousConfiguration is null
                   || previousConfiguration.CheckInIntervalSeconds != newConfiguration.CheckInIntervalSeconds);
    }

    public static bool ShouldStop(LabCommunityDefaultConfiguration defaultConfiguration, Configuration? configuration)
    {
        return false;
    }

    public static TimeSpan ResolveInterval(LabCommunityDefaultConfiguration defaultConfiguration, Configuration? configuration)
    {
        if (configuration is not null)
        {
            return TimeSpan.FromSeconds(configuration.CheckInIntervalSeconds);
        }

        return defaultConfiguration.CheckInInterval;
    }

    public Task Execute(IJobExecutionContext context)
    {
        return sender.Send(new CheckInToLabCommunityCommand(), context.CancellationToken);
    }
}
