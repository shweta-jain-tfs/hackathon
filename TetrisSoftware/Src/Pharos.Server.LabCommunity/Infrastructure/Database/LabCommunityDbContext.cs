using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Pharos.Server.Infrastructure.Database;
using Pharos.Server.LabCommunity.Application.Interfaces;
using Pharos.Server.LabCommunity.Domain;

namespace Pharos.Server.LabCommunity.Infrastructure.Database;

internal class LabCommunityDbContext : BaseDbContext<LabCommunityDbContext>, ILabCommunityDbContext
{
    public const string Schema = "LabCommunity";

    public LabCommunityDbContext(IConfiguration configuration, IAuditableEntitySaveChangesInterceptor? auditableEntitySaveChangesInterceptor, IMediator? mediator)
        : base(configuration, auditableEntitySaveChangesInterceptor, mediator)
    {
    }

    protected override string DefaultSchema => Schema;

    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<Configuration> Configurations => Set<Configuration>();

    public DbSet<ExportItem> ExportItems => Set<ExportItem>();

    public Task<Enrollment?> GetActiveEnrollmentAsync(string laboratoryCode, CancellationToken cancellationToken = default)
    {
        return Enrollments
            .Where(e => e.LaboratoryCode == laboratoryCode && e.RevokedAtUtc == null)
            .OrderByDescending(e => e.EnrolledAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<ExportItem?> GetExportItemByDeduplicationKeyAsync(string deduplicationKey, CancellationToken cancellationToken = default)
    {
        return ExportItems
            .FirstOrDefaultAsync(item => item.DeduplicationKey == deduplicationKey, cancellationToken);
    }

    public Task<int> DeleteExportItemsOlderThanAsync(DateTimeOffset cutoff, CancellationToken cancellationToken = default)
    {
        return ExportItems
            .Where(item => item.CreatedAtUtc < cutoff)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public Task<Configuration?> GetConfigurationAsync(string laboratoryCode, CancellationToken cancellationToken = default)
    {
        return Configurations
            .FirstOrDefaultAsync(c => c.LaboratoryCode == laboratoryCode, cancellationToken);
    }
}