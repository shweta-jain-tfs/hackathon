using Pharos.Server.AuditTrail.Domain;

namespace Pharos.Server.AuditTrail.Application.Interfaces;
internal interface IAuditTrailDbContext : IDbContext
{
    public DbSet<AuditTrailRecord> AuditTrailRecords { get; }
    public DbSet<AuditTrailRecordNotes> AuditTrailRecordNotes { get; }
    Task HashesBulkUpdateAsync(List<(int Id, string Hash)> idHashPairs);
    Task<int> DeleteExpiredRecordsAsync(DateTimeOffset expiry, int batchSize, CancellationToken ct = default);
}