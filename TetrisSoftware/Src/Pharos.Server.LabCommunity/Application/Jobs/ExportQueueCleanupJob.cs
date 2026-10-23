using MediatR;
using Microsoft.Extensions.Options;
using Pharos.Server.LabCommunity.Application.Commands;
using Pharos.Server.LabCommunity.Application.Interfaces;
using Pharos.Server.LabCommunity.Domain;
using Quartz;

namespace Pharos.Server.LabCommunity.Application.Jobs;

[DisallowConcurrentExecution]
internal sealed class ExportQueueCleanupJob(
    ISender sender,
    IOptions<LabCommunityDefaultConfiguration> configuration) : ILabCommunityJob
{
    public static JobKey Key => new("labcommunity-export-queue-cleanup");

    public static TriggerKey Trigger => new("labcommunity-export-queue-cleanup-recurring-trigger");

    public static bool ShouldReconfigureAfterConfigurationPull(Configuration? newConfiguration, Configuration? previousConfiguration)
    {
        return false;
    }

    public static bool ShouldStop(LabCommunityDefaultConfiguration defaultConfiguration, Configuration? configuration)
    {
        return false;
    }

    public static TimeSpan ResolveInterval(LabCommunityDefaultConfiguration defaultConfiguration, Configuration? configuration)
    {
        return defaultConfiguration.ExportQueueCleanupInterval;
    }

    public Task Execute(IJobExecutionContext context)
    {
        return sender.Send(
            new CleanupLabCommunityExportQueueCommand(configuration.Value.DeleteExportItemsOlderThanDays),
            context.CancellationToken);
    }
}