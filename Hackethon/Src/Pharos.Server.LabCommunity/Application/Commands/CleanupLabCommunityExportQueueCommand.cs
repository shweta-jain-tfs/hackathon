using MediatR;
using Microsoft.Extensions.Logging;
using Pharos.Server.Infrastructure;
using Pharos.Server.LabCommunity.Application.Interfaces;

namespace Pharos.Server.LabCommunity.Application.Commands;

internal sealed record CleanupLabCommunityExportQueueCommand(int OlderThanDays) : IRequest;

internal sealed class CleanupLabCommunityExportQueueCommandHandler(
    ILabCommunityDbContext dbContext,
    IDateTime dateTime,
    ILogger<CleanupLabCommunityExportQueueCommandHandler> logger)
    : IRequestHandler<CleanupLabCommunityExportQueueCommand>
{
    public async Task Handle(CleanupLabCommunityExportQueueCommand request, CancellationToken cancellationToken)
    {
        if (request.OlderThanDays <= 0)
        {
            logger.LogWarning(
                "LabCommunity export queue cleanup skipped because the retention period is not positive. Retention days = {RetentionDays}.",
                request.OlderThanDays);
            return;
        }

        var cutoff = dateTime.UtcNow.Subtract(TimeSpan.FromDays(request.OlderThanDays));
        var deletedItemCount = await dbContext.DeleteExportItemsOlderThanAsync(cutoff, cancellationToken);

        logger.LogInformation(
            "Deleted {DeletedItemCount} LabCommunity export items created before {Cutoff}.",
            deletedItemCount,
            cutoff);
    }
}