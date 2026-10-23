using MediatR;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Lis.Application.Interfaces;
using Pharos.Server.Lis.Domain;

namespace Pharos.Server.Lis.Application.Commands;

/// <summary>
/// Sends a query to retrieve all test orders from the specified LIS channel.
/// </summary>
internal sealed record SendAllTestOrderRequestsToLisCommand(int LisChannelId) : IRequest;

internal sealed class SendAllTestOrderRequestsToLisCommandHandler(
    IOptionsSnapshot<LisSettings> lisSettings,
    ILisMessageBuilder messageBuilder,
    ILisMessageDispatcher messageDispatcher) : IRequestHandler<SendAllTestOrderRequestsToLisCommand>
{
    public async Task Handle(SendAllTestOrderRequestsToLisCommand request, CancellationToken cancellationToken)
    {
        var channel = lisSettings.Value.LisChannels[request.LisChannelId];

        var messageData = messageBuilder.BuildRequestMessage(channel, Array.Empty<string>());
        // TODO: For now using poll request frequency as message expiration timeout
        var messageExpiration = TimeSpan.FromMinutes(channel.ImportSettings.Frequency);
        var metadata = new Dictionary<string, string>()
        {
            [LisMessageMetadataKeys.SampleIds] = "ALL"
        };
        await messageDispatcher.PostMessage(request.LisChannelId, LisMessagePriority.High, LisMessageType.TestOrderQuery, metadata, messageData, messageExpiration);
    }
}