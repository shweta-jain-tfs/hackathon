using Pharos.Server.LabCommunity.Domain;

namespace Pharos.Server.LabCommunity.Application.Interfaces;

internal interface ILabCommunityExportQueue
{
    Task EnqueueAsync(
        ExportItemType exportItemType,
        string deduplicationKey,
        string payloadJson,
        int payloadVersion,
        CancellationToken cancellationToken = default);

    Task DequeueBatchAsync(
        IReadOnlyCollection<ExportItem> exportItems,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExportItem>> PeekBatchAsync(
        ExportItemType exportItemType,
        CancellationToken cancellationToken = default);
}