using MediatR;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Lis;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Lis.Application.Interfaces;
using Pharos.Server.Lis.Domain;

namespace Pharos.Server.Lis.Application.Commands;

internal sealed class SendQualityControlTestResultToLisCommandHandler(
    IOptionsSnapshot<LisSettings> lisSettings,
    ILisMessageBuilder messageBuilder,
    ILisMessageDispatcher messageDispatcher)
    : IRequestHandler<SendQualityControlTestResultToLisCommand>
{
    public async Task Handle(SendQualityControlTestResultToLisCommand command, CancellationToken cancellationToken)
    {
        var lisChannels = lisSettings.Value.LisChannels;
        if (lisChannels.Count == 0)
        {
            return;
        }

        int lisChannelId;
        if (command.LisChannelId == null || command.LisChannelId.Value >= lisChannels.Count)
        {
            lisChannelId = 0;
        }
        else
        {
            lisChannelId = command.LisChannelId.Value;
        }

        var channel = lisChannels[lisChannelId];
        if (!channel.IsEnabled)
        {
            return;
        }

        var priority = command.IsManualExport ? LisMessagePriority.Normal : LisMessagePriority.Low;
        var messageMetadata = new Dictionary<string, string>
        {
            [LisMessageMetadataKeys.QcTestResultId] = command.QcTestResult.Id.ToString()
        };

        var messageData = messageBuilder.BuildQualityControlTestResultMessage(channel, command.QcTestResult);
        var messageExpiration = TimeSpan.FromMinutes(channel.ExportSettings.TimeoutForExports);

        await messageDispatcher.PostMessage(lisChannelId, priority, LisMessageType.QualityControlTestResult, messageMetadata, messageData, messageExpiration);
    }
}
