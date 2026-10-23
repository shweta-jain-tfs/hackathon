using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Lis.Application.Commands.LisStatisticsCommands;
using Quartz;

namespace Pharos.Server.Lis.Application.Jobs;

[DisallowConcurrentExecution]
internal sealed class ResetLisStatisticsJob(
    ISender sender,
    IOptionsMonitor<LisSettings> lisSettings,
    ILogger<ResetLisStatisticsJob> logger) : IJob
{
    public static readonly JobKey JobKey = new("reset-statistics-at-midnight");
    public static readonly TriggerKey MidnightTriggerKey = new("reset-statistics-at-midnight-trigger");

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            var channelIds = lisSettings.CurrentValue.LisChannels
            .Select((channel, index) => (channel, index))
            .Where(x => x.channel.Other.ResetStatisticsAtMidnight)
            .Select(x => x.index)
            .ToList();

            if (channelIds.Count == 0) { return; }

            logger.LogInformation("Starting midnight statistics reset for {ChannelCount} eligible LIS channels.", channelIds.Count);

            foreach (var channelId in channelIds)
            {
                await sender.Send(new ResetLisStatisticsCommand(channelId), context.CancellationToken);
            }

            logger.LogInformation("Midnight statistics reset completed for all eligible LIS channels.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to execute midnight statistics reset.");
        }
    }
}
