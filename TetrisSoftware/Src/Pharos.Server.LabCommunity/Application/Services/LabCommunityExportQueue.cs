using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pharos.Server.Infrastructure;
using Pharos.Server.LabCommunity.Application.Interfaces;
using Pharos.Server.LabCommunity.Domain;

namespace Pharos.Server.LabCommunity.Application.Services;

internal sealed class LabCommunityExportQueue(
    ILabCommunityDbContext dbContext,
    IDateTime dateTime,
    ILogger<LabCommunityExportQueue> logger) : ILabCommunityExportQueue
{
    public async Task EnqueueAsync(
        ExportItemType exportItemType,
        string deduplicationKey,
        string payloadJson,
        int payloadVersion,
        CancellationToken cancellationToken = default)
    {
        var existingItem = await dbContext.GetExportItemByDeduplicationKeyAsync(deduplicationKey, cancellationToken);
        if (existingItem is not null)
        {
            logger.LogDebug(
                "LabCommunity export item for {ExportType} was already queued. Existing export item ID = {ExportItemId}.",
                exportItemType,
                existingItem.Id);
            return;
        }

        var exportItem = new ExportItem
        {
            ExportItemType = exportItemType,
            DeduplicationKey = deduplicationKey,
            PayloadJson = payloadJson,
            PayloadVersion = payloadVersion,
            Status = ExportItemStatus.Pending,
            CreatedAtUtc = dateTime.UtcNow
        };

        dbContext.ExportItems.Add(exportItem);

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Created LabCommunity export item {ExportItemId} for {ExportType}. Payload version = {PayloadVersion}.",
            exportItem.Id,
            exportItemType,
            payloadVersion);
    }

    public async Task DequeueBatchAsync(
        IReadOnlyCollection<ExportItem> exportItems,
        CancellationToken cancellationToken = default)
    {
        dbContext.ExportItems.RemoveRange(exportItems);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ExportItem>> PeekBatchAsync(
        ExportItemType exportItemType,
        CancellationToken cancellationToken = default)
    {
        var now = dateTime.UtcNow;

        return await dbContext.ExportItems
            .Where(item => item.ExportItemType == exportItemType
                           && (item.Status == ExportItemStatus.Pending
                               || item.Status == ExportItemStatus.Failed)
                           && (item.NextAttemptAtUtc == null || item.NextAttemptAtUtc <= now))
            .OrderBy(item => item.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }
}