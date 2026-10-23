using Pharos.Server.Infrastructure.Database;
using Pharos.Server.Infrastructure.Services;

namespace Pharos.Server.Maintenance.Infrastructure.Database;

internal sealed class MaintenanceDbContextInitializer(MaintenanceDbContext context, IDbEncryptionService dbEncryptionService) : DbContextInitializer(context, dbEncryptionService)
{
    protected override string SchemaName => MaintenanceDbContext.Schema;

    public override Task SeedAsync()
    {
        return Task.CompletedTask;
    }
}