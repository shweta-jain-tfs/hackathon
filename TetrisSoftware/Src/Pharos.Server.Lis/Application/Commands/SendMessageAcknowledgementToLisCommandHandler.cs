using MediatR;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Lis.Application.Interfaces;
using Pharos.Server.Lis.Domain;

namespace Pharos.Server.Lis.Application.Commands;


/// <summary>
/// This command is used to send an acknowledgement message back to the LIS system after receiving a message from it. The acknowledgement indicates whether the message was processed successfully or not.
/// </summary>
/// <param name="LisChannelId">The ID of the LIS channel to which the acknowledgement message will be sent.</param>
/// <param name="MessageMetadata">A dictionary containing metadata about the message being acknowledged, such as the trigger event and message control ID.</param>
/// <param name="Success">A boolean indicating whether the message was processed successfully.</param>
public sealed record SendMessageAcknowledgementToLisCommand(
    int LisChannelId,
    Dictionary<string, string> MessageMetadata,
    bool Success) : IRequest;


internal sealed class SendMessageAcknowledgementToLisCommandHandler(
    IOptionsSnapshot<LisSettings> lisSettings,
    ILisMessageBuilder messageBuilder,
    ILisMessageDispatcher messageDispatcher)
    : IRequestHandler<SendMessageAcknowledgementToLisCommand>
{
    public async Task Handle(SendMessageAcknowledgementToLisCommand request, CancellationToken cancellationToken)
    {
        var lisChannels = lisSettings.Value.LisChannels;
        if (lisChannels.Count == 0)
        {
            return;
        }

        if (request.LisChannelId < 0 || request.LisChannelId >= lisChannels.Count)
        {
            return;
        }
        var channel = lisChannels[request.LisChannelId];
        if (channel == null || !channel.IsEnabled)
        {
            return;
        }

        var messageData = messageBuilder.BuildAcknowledgementMessage(channel, request.MessageMetadata, request.Success);

        if (messageData == null)
        {
            return;
        }

        await messageDispatcher.PostMessage(request.LisChannelId, LisMessagePriority.High, LisMessageType.Acknowledgement, new Dictionary<string, string>(), messageData);
    }
}