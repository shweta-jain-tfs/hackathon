using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pharos.Server.Lis.Application.Interfaces;
using Pharos.Server.Lis.Domain;

namespace Pharos.Server.Lis.Application.Services;

internal sealed class LisMessageQueue(
    ILisDbContext dbContext,
    ILogger<LisMessageQueue> logger) : ILisMessageQueue
{
    public async Task DequeueAsync(Guid queuedMessageId)
    {
        logger.LogDebug("De-queueing message {MessageId} from queue", queuedMessageId);
        var deletedMessage = dbContext.QueuedLisMessages.FirstOrDefault(m => m.MessageId == queuedMessageId);

        if (deletedMessage == null)
        {
            logger.LogWarning("Did not find message ID {MessageId} to remove.", queuedMessageId);
            return;
        }

        dbContext.Entry(deletedMessage).State = EntityState.Deleted;
        dbContext.QueuedLisMessages.Remove(deletedMessage);

        await dbContext.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
        logger.LogDebug("Removed message ID {MessageId} from the queue.", queuedMessageId);
    }

    public async Task<Guid> EnqueueAsync(QueuedLisMessage message)
    {
        logger.LogDebug("Enqueueing message {MessageId} to queue", message.MessageId);
        dbContext.QueuedLisMessages.Add(message);
        await dbContext.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);

        logger.LogDebug("Added a message ID {MessageId} to the queue.", message.MessageId);

        return message.MessageId;
    }

    public async Task<QueuedLisMessage?> PeekAsync(int channelId, LisDispatchMode dispatchMode)
    {
        var query = dbContext.QueuedLisMessages
            .Where(m => m.ChannelId == channelId);

        query = dispatchMode switch
        {
            LisDispatchMode.OnlyLow => query.Where(m => m.Priority == (int)LisMessagePriority.Low),
            LisDispatchMode.ExcludeLow => query.Where(m => m.Priority != (int)LisMessagePriority.Low),
            _ => query
        };

        return await query
            .OrderBy(m => m.Priority)
            .ThenBy(m => m.Created)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);
    }

    public async Task<int> DeleteMessagesByChannelAsync(int channelId)
    {
        var messages = await dbContext.QueuedLisMessages
            .Where(m => m.ChannelId == channelId)
            .ToListAsync()
            .ConfigureAwait(false);

        if (messages.Count == 0)
        {
            return 0;
        }

        dbContext.QueuedLisMessages.RemoveRange(messages);
        await dbContext.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);

        logger.LogDebug("Deleted {Count} messages for channel ID {LisChannelId}.", messages.Count, channelId);

        return messages.Count;
    }
}