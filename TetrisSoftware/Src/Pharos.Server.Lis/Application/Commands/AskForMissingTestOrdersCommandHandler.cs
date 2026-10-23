using MediatR;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Lis;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Infrastructure;
using Pharos.Server.Lis.Application.Interfaces;
using Pharos.Server.Lis.Application.Jobs;
using Pharos.Server.Lis.Domain;
using Quartz;

namespace Pharos.Server.Lis.Application.Commands;

internal sealed class AskForMissingTestOrdersCommandHandler(
                            ISchedulerFactory schedulerFactory,
                            IDateTime dateTime,
                            IOptionsSnapshot<LisSettings> lisSettings,
                            ILisMessageBuilder messageBuilder,
                            ILisMessageDispatcher messageDispatcher,
                            IPublisher publisher) : IRequestHandler<AskForMissingTestOrdersCommand>
{
    public async Task Handle(AskForMissingTestOrdersCommand request, CancellationToken cancellationToken)
    {
        if (request.SampleIDs.Count == 0)
        {
            return;
        }

        var hasQuerySettingsEnabled = HasAnyQuerySettingsEnabled();
        var lisChannels = GetEligibleLisChannels(request.ManualScan);
        if (hasQuerySettingsEnabled && lisChannels.Count > 0)
        {
            for (var i = 0; i < lisChannels.Count; i++)
            {
                var messageMetadata = new Dictionary<string, string> { [LisMessageMetadataKeys.SampleIds] = string.Join(",", request.SampleIDs) };
                var messageData = messageBuilder.BuildRequestMessage(lisChannels[i].Channel, request.SampleIDs);

                await messageDispatcher.PostMessage(lisChannels[i].Index, LisMessagePriority.High, LisMessageType.TestOrderQuery, messageMetadata, messageData);
            }

            var timeout = GetLisMaxTimeout(lisChannels.Select(x => x.Channel));
            await SetupListenerForMissingTestOrders(request.SampleIDs, timeout, cancellationToken);
        }
        else
        {
            foreach (var sampleId in request.SampleIDs.Distinct())
            {
                await publisher.Publish(new MissingTestOrdersEvent(sampleId), cancellationToken);
            }
        }
    }

    private bool HasAnyQuerySettingsEnabled()
    {
        return lisSettings.Value.LisChannels
            .Any(x => x.ImportSettings.PollForRequests ||
                        x.ImportSettings.ManualQueryForAll ||
                        x.ImportSettings.ManualRefreshExistingRequests ||
                        x.ImportSettings.AskForMissingRequestsInstrumentScan ||
                        x.ImportSettings.RefreshExistingRequestsInstrumentScan ||
                        x.ImportSettings.AskForMissingRequestsManualScan ||
                        x.ImportSettings.RefreshExistingRequestsManualScan);
    }

    private List<(int Index, LisChannel Channel)> GetEligibleLisChannels(bool manualScan)
    {
        var enabledChannels = lisSettings.Value.LisChannels
            .Select((channel, index) => new { Channel = channel, Index = index })
            .Where(x => x.Channel.IsEnabled);

        return manualScan
            ? enabledChannels
                .Where(x => x.Channel.ImportSettings?.AskForMissingRequestsManualScan == true)
                .Select(x => (x.Index, x.Channel))
                .ToList()
            : enabledChannels
                .Where(x => x.Channel.ImportSettings?.AskForMissingRequestsInstrumentScan == true)
                .Select(x => (x.Index, x.Channel))
                .ToList();
    }

    private static int GetLisMaxTimeout(IEnumerable<LisChannel> channels)
    {
        var maxTimeout = channels.Select(c => c.ImportSettings?.Timeout ?? 0).DefaultIfEmpty(0).Max();
        return maxTimeout * 60;
    }

    private async Task SetupListenerForMissingTestOrders(IReadOnlyCollection<string> sampleIds, int secondsUntilMissingTestOrder, CancellationToken cancellationToken)
    {
        var scheduler = await schedulerFactory.GetScheduler(cancellationToken);

        var startAt = dateTime.UtcNow.AddSeconds(secondsUntilMissingTestOrder);

        foreach (var sampleId in sampleIds.Distinct())
        {
            var jobKey = PublishMissingTestOrdersJob.Key(sampleId);

            if (await scheduler.CheckExists(jobKey, cancellationToken))
            {
                continue;
            }

            var job = JobBuilder.Create<PublishMissingTestOrdersJob>()
                                .WithIdentity(jobKey)
                                .UsingJobData(PublishMissingTestOrdersJob.SampleId, sampleId)
                                .Build();

            var trigger = Quartz.TriggerBuilder.Create()
                .StartAt(startAt)
                .ForJob(job)
                .Build();

            await scheduler.ScheduleJob(job, trigger, cancellationToken);
        }
    }
}