using Pharos.Server.Infrastructure.Database;
using Pharos.Server.Infrastructure.Services;

namespace Pharos.Server.Lis.Infrastructure.Database;

internal class LisDbContextInitializer(
    LisDbContext context,
    IDbEncryptionService dbEncryptionService) : DbContextInitializer(context, dbEncryptionService)
{
    protected override string SchemaName => LisDbContext.Schema;

    public override Task SeedAsync()
    {
        return Task.CompletedTask;
    }
}