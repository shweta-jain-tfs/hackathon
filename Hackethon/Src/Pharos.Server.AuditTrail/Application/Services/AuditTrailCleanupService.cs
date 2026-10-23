using System.Text;
using Pharos.Server.AuditTrail.Application.Interfaces;

namespace Pharos.Server.AuditTrail.Application.Services;
internal class AuditTrailCleanupService(IAuditTrailDbContext dbContext, AuditDataProtectionService dataProtectionService) : IAuditTrailCleanupService
{
    private const int _batchSize = 5000;

    public async Task<bool> AuditTrailCleanupAsync()
    {
        var auditTrailRetentionThreshold = DateTimeOffset.Now.AddYears(-5);
        var totalDeleted = 0;

        while (true)
        {
            var expiredBatchCount = await dbContext.DeleteExpiredRecordsAsync(auditTrailRetentionThreshold, _batchSize);

            if (expiredBatchCount == 0)
            {
                break;
            }
            totalDeleted += expiredBatchCount;
        }

        if (totalDeleted == 0)
        {
            return false;
        }

        await RecomputeAuditTrailHashes();
        return true;
    }

    private async Task RecomputeAuditTrailHashes()
    {
        var cumulativeHash = "";
        int? lastProcessedId = null;
        var sb = new StringBuilder();

        while (true)
        {
            var id = lastProcessedId;
            var batch = await dbContext.AuditTrailRecords
                .AsNoTracking()
                .Where(a => id == null || a.Id > id)
                .OrderBy(a => a.Id)
                .Take(_batchSize)
                .ToListAsync();
            if (batch is { Count: 0 })
            {
                break;
            }
            var idHashPairs = new List<(int Id, string Hash)>(batch.Count);

            foreach (var record in batch)
            {
                sb.Clear();
                cumulativeHash = dataProtectionService.ComputeHash(cumulativeHash, StringHelper.BuildHashString(record, sb));
                idHashPairs.Add((record.Id, cumulativeHash));
            }

            // Update hashes using TVP
            await dbContext.HashesBulkUpdateAsync(idHashPairs);
            lastProcessedId = batch.Last().Id;
        }

        if (lastProcessedId != null)
        {
            dataProtectionService.SaveHash(cumulativeHash);
        }
    }
}