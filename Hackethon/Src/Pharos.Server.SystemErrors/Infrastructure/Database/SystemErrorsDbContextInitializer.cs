using Pharos.Server.Infrastructure.Database;
using Pharos.Server.Infrastructure.Services;

namespace Pharos.Server.SystemErrors.Infrastructure.Database;

internal class SystemErrorsDbContextInitializer(
    SystemErrorsDbContext context,
    IDbEncryptionService dbEncryptionService) : DbContextInitializer(context, dbEncryptionService)
{
    protected override string SchemaName => SystemErrorsDbContext.Schema;

    public override Task SeedAsync()
    {
        return Task.CompletedTask;
    }
}