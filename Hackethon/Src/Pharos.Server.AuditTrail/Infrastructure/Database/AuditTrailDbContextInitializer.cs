using Pharos.Server.Infrastructure.Services;

namespace Pharos.Server.AuditTrail.Infrastructure.Database;

internal sealed class AuditTrailDbContextInitializer(
    AuditTrailDbContext context,
    IDbEncryptionService dbEncryptionService) : DbContextInitializer(context, dbEncryptionService)
{
    protected override string SchemaName => AuditTrailDbContext.Schema;

    public override Task SeedAsync()
    {
        return Task.CompletedTask;
    }
}