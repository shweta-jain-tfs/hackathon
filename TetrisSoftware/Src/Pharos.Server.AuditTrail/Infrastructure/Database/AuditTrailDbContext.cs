using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Pharos.Server.Application.AuditTrail.Enums;
using Pharos.Server.AuditTrail.Application.Interfaces;
using Pharos.Server.AuditTrail.Domain;

namespace Pharos.Server.AuditTrail.Infrastructure.Database;

internal class AuditTrailDbContext(
    IConfiguration configuration,
    IAuditableEntitySaveChangesInterceptor auditableEntitySaveChangesInterceptor,
    IMediator? mediator) : BaseDbContext<AuditTrailDbContext>(configuration, auditableEntitySaveChangesInterceptor, mediator), IAuditTrailDbContext
{
    private readonly IConfiguration _configuration = configuration;
    public const string Schema = "AuditTrailRecord";
    protected override string DefaultSchema => Schema;
    public DbSet<AuditTrailRecord> AuditTrailRecords => Set<AuditTrailRecord>();
    public DbSet<AuditTrailRecordNotes> AuditTrailRecordNotes => Set<AuditTrailRecordNotes>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var record = modelBuilder.Entity<AuditTrailRecord>();
        record.HasIndex(r => r.Created).IsDescending();
        record.HasIndex(r => r.Comment);

        modelBuilder.Entity<AuditTrailRecord>()
            .Property(e => e.EventId)
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<EventId>(v));

        modelBuilder.Entity<AuditTrailRecord>()
            .Property(e => e.EventData)
            .HasConversion(
                c => JsonSerializer.Serialize(c, JsonSerializerOptions.Default),
                c => JsonSerializer.Deserialize<List<AuditEventData>>(c, JsonSerializerOptions.Default)!);

        modelBuilder.Entity<AuditTrailRecord>()
            .HasOne<AuditTrailRecordNotes>(r => r.AuditTrailRecordNotes)
            .WithOne()
            .HasForeignKey<AuditTrailRecordNotes>(n => n.AuditTrailRecordId)
            .OnDelete(DeleteBehavior.Cascade);
        base.OnModelCreating(modelBuilder);
    }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer(_configuration.GetConnectionString("Audit"), options =>
                options.CommandTimeout(180)); // command timeout in seconds, used for batch verification and deletion
        }
    }
    public async Task HashesBulkUpdateAsync(List<(int Id, string Hash)> idHashPairs)
    {
        var dataTable = new DataTable();
        dataTable.Columns.Add("Id", typeof(int));
        dataTable.Columns.Add("Hash", typeof(string));

        foreach (var (id, hash) in idHashPairs)
        {
            dataTable.Rows.Add(id, hash);
        }

        var param = new SqlParameter("@IdHashTable", SqlDbType.Structured)
        {
            TypeName = "dbo.IdHashTableType", // user-defined table type in SQL Server
            Value = dataTable
        };
        await Database.ExecuteSqlRawAsync(@"
            UPDATE at
            SET at.Hash = tvp.Hash
            FROM AuditTrailRecord.AuditTrailRecords at
            INNER JOIN @IdHashTable tvp ON at.Id = tvp.Id
        ", param);
    }

    public async Task<int> DeleteExpiredRecordsAsync(DateTimeOffset auditTrailRetentionThreshold, int batchSize, CancellationToken ct = default)
    {
        return await AuditTrailRecords
            .Where(r => r.Created < auditTrailRetentionThreshold)
            .OrderBy(r => r.Id)
            .Take(batchSize)
            .ExecuteDeleteAsync(ct);
    }
}