using MediatR;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Lis;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Lis.Application.Interfaces;
using Pharos.Server.Lis.Domain;

namespace Pharos.Server.Lis.Application.Commands;

internal sealed class SendSampleTestResultsToLisCommandHandler(
    IOptionsSnapshot<LisSettings> lisSettings,
    ILisMessageBuilder messageBuilder,
    ILisMessageDispatcher messageDispatcher)
    : IRequestHandler<SendSampleTestResultsToLisCommand>
{
    public async Task Handle(SendSampleTestResultsToLisCommand request, CancellationToken cancellationToken)
    {
        var lisChannels = lisSettings.Value.LisChannels;
        if (lisChannels.Count == 0)
        {
            return;
        }

        int lisChannelId;
        if (request.LisChannelId == null || request.LisChannelId.Value >= lisChannels.Count)
        {
            // TODO: There should be a setting for which channel is the default
            lisChannelId = 0;
        }
        else
        {
            lisChannelId = request.LisChannelId.Value;
        }

        var channel = lisChannels[lisChannelId];
        if (!channel.IsEnabled)
        {
            return;
        }

        var priority = request.IsManualExport ? LisMessagePriority.Normal : LisMessagePriority.Low;
        var sampleIds = request.Patients.SelectMany(p => p.Samples).Select(s => s.SampleId).Distinct();
        var messageMetadata = new Dictionary<string, string>
        {
            [LisMessageMetadataKeys.TestOrderIds] = string.Join(",", request.TestOrderIds),
            [LisMessageMetadataKeys.SampleIds] = string.Join(",", sampleIds)
        };

        var messageData = messageBuilder.BuildSampleTestResultMessage(channel, request.Patients);
        var messageExpiration = TimeSpan.FromMinutes(channel.ExportSettings.TimeoutForExports);

        await messageDispatcher.PostMessage(lisChannelId, priority, LisMessageType.SampleTestResult, messageMetadata, messageData, messageExpiration);
    }
}