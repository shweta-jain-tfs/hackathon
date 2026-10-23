using Pharos.Server.Infrastructure.Database;
using Pharos.Server.Infrastructure.Services;

namespace Pharos.Server.LoadList.Infrastructure.Database;

internal class LoadListDbContextInitializer(
    LoadListDbContext context,
    IDbEncryptionService dbEncryptionService) : DbContextInitializer(context, dbEncryptionService)
{
    protected override string SchemaName => LoadListDbContext.Schema;

    public override Task SeedAsync()
    {
        return Task.CompletedTask;
    }
}