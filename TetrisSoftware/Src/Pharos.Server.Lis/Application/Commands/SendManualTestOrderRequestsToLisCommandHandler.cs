using MediatR;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Lis.Application.Interfaces;
using Pharos.Server.Lis.Domain;

namespace Pharos.Server.Lis.Application.Commands;

public sealed record SendManualTestOrderRequestsToLisCommand(string[]? SampleIds) : IRequest;

internal sealed class SendManualTestOrderRequestsToLisCommandHandler(
    IOptionsSnapshot<LisSettings> lisSettings,
    ILisMessageBuilder messageBuilder,
    ILisMessageDispatcher messageDispatcher) : IRequestHandler<SendManualTestOrderRequestsToLisCommand>
{
    public async Task Handle(SendManualTestOrderRequestsToLisCommand request, CancellationToken cancellationToken)
    {
        var lisChannels = lisSettings.Value.LisChannels;

        if (request.SampleIds != null && request.SampleIds.Length > 0)
        {
            var lisChannelsWithManualRefreshExistingRequestsEnabled = lisChannels
                .Select((channel, index) => new { Channel = channel, Index = index })
                .Where(x => x.Channel.IsEnabled &&
                               x.Channel.ImportSettings?.ManualRefreshExistingRequests == true)
                .Select(x => (x.Index, x.Channel))
                .ToList();

            await SendQueryToChannels(lisChannelsWithManualRefreshExistingRequestsEnabled, request.SampleIds);
        }
        else
        {
            var lisChannelsWithManualQueryLisAllEnabled = lisChannels
                .Select((channel, index) => new { Channel = channel, Index = index })
                .Where(x => x.Channel.IsEnabled &&
                               x.Channel.ImportSettings?.ManualQueryForAll == true)
                .Select(x => (x.Index, x.Channel))
                .ToList();

            await SendQueryToChannels(lisChannelsWithManualQueryLisAllEnabled, Array.Empty<string>());
        }
    }

    private async Task SendQueryToChannels(List<(int Index, LisChannel Channel)> channels, string[] sampleIds)
    {
        for (var i = 0; i < channels.Count; i++)
        {
            if (channels[i].Channel.ProtocolType == LisProtocolType.HL7 && sampleIds.Length > 1)
            {
                foreach (var sampleId in sampleIds)
                {
                    await PostQueryMessage(channels[i], [sampleId]);
                }

                continue;
            }

            await PostQueryMessage(channels[i], sampleIds);
        }
    }

    private async Task PostQueryMessage((int Index, LisChannel Channel) channel, string[] sampleIds)
    {
        Dictionary<string, string> messageMetadata = sampleIds.Length > 0
           ? new() { [LisMessageMetadataKeys.SampleIds] = string.Join(",", sampleIds) }
           : new();
        var messageData = messageBuilder.BuildRequestMessage(channel.Channel, sampleIds);

        await messageDispatcher.PostMessage(channel.Index, LisMessagePriority.High, LisMessageType.TestOrderQuery, messageMetadata, messageData);
    }
}
