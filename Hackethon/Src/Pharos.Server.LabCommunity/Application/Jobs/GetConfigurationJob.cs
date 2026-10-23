using MediatR;
using Pharos.Server.LabCommunity.Application.Commands;
using Pharos.Server.LabCommunity.Application.Interfaces;
using Pharos.Server.LabCommunity.Domain;
using Quartz;

namespace Pharos.Server.LabCommunity.Application.Jobs;

[DisallowConcurrentExecution]
internal sealed class GetConfigurationJob(ISender sender) : ILabCommunityJob
{
    public static JobKey Key => new("labcommunity-get-configuration");
    public static TriggerKey Trigger => new("labcommunity-get-configuration-recurring-trigger");
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
        return sender.Send(new GetLabCommunityConfigurationCommand(), context.CancellationToken);
    }
}
