using Pharos.Server.Infrastructure.Database;
using Pharos.Server.Infrastructure.Services;

namespace Pharos.Server.SystemUpdate.Infrastructure.Database;

internal class SystemUpdateDbContextInitializer(SystemUpdateDbContext context, IDbEncryptionService dbEncryptionService)
    : DbContextInitializer(context, dbEncryptionService)
{
    protected override string SchemaName => SystemUpdateDbContext.Schema;
    public override Task SeedAsync()
    {
        return Task.CompletedTask;
    }
}
