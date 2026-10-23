using Microsoft.EntityFrameworkCore;
using Pharos.Server.Infrastructure.Database;
using Pharos.Server.LabCommunity.Domain;

namespace Pharos.Server.LabCommunity.Application.Interfaces;

internal interface ILabCommunityDbContext : IDbContext
{
    DbSet<Enrollment> Enrollments { get; }
    DbSet<Configuration> Configurations { get; }

    DbSet<ExportItem> ExportItems { get; }

    /// <summary>
    /// Returns the active (non-revoked) enrollment for the given laboratory code, if one exists.
    /// </summary>
    Task<Enrollment?> GetActiveEnrollmentAsync(string laboratoryCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the persisted configuration for the given laboratory code, if one exists.
    /// </summary>
    Task<Configuration?> GetConfigurationAsync(string laboratoryCode, CancellationToken cancellationToken = default);

    Task<ExportItem?> GetExportItemByDeduplicationKeyAsync(string deduplicationKey, CancellationToken cancellationToken = default);

    Task<int> DeleteExportItemsOlderThanAsync(DateTimeOffset cutoff, CancellationToken cancellationToken = default);
}