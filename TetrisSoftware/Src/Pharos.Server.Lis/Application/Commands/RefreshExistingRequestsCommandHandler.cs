using MediatR;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Lis;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Lis.Application.Interfaces;
using Pharos.Server.Lis.Domain;

namespace Pharos.Server.Lis.Application.Commands;

internal sealed class RefreshExistingRequestsCommandHandler(
                            IOptionsSnapshot<LisSettings> lisSettings,
                            ILisMessageBuilder messageBuilder,
                            ILisMessageDispatcher messageDispatcher) : IRequestHandler<RefreshExistingRequestsCommand>
{
    public async Task Handle(RefreshExistingRequestsCommand request, CancellationToken cancellationToken)
    {
        if (request.SampleIDs.Count == 0)
        {
            return;
        }

        var lisChannels = GetEligibleLisChannels(request.ManualScan);
        if (lisChannels.Count > 0)
        {
            for (var i = 0; i < lisChannels.Count; i++)
            {
                var messageMetadata = new Dictionary<string, string> { [LisMessageMetadataKeys.SampleIds] = string.Join(",", request.SampleIDs) };
                var messageData = messageBuilder.BuildRequestMessage(lisChannels[i].Channel, request.SampleIDs);

                await messageDispatcher.PostMessage(lisChannels[i].Index, LisMessagePriority.High, LisMessageType.TestOrderQuery, messageMetadata, messageData);
            }
        }
    }

    private List<(int Index, LisChannel Channel)> GetEligibleLisChannels(bool manualScan)
    {
        var enabledChannels = lisSettings.Value.LisChannels
            .Select((channel, index) => new { Channel = channel, Index = index })
            .Where(x => x.Channel.IsEnabled);

        return manualScan
            ? enabledChannels
                .Where(x => x.Channel.ImportSettings?.RefreshExistingRequestsManualScan == true)
                .Select(x => (x.Index, x.Channel))
                .ToList()
            : enabledChannels
                .Where(x => x.Channel.ImportSettings?.RefreshExistingRequestsInstrumentScan == true)
                .Select(x => (x.Index, x.Channel))
                .ToList();
    }
}