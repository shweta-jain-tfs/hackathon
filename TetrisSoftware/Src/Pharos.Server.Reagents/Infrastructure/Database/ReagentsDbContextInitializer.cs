using Pharos.Server.Infrastructure.Services;

namespace Pharos.Server.Reagents.Infrastructure.Database;

internal class ReagentsDbContextInitializer(
    ReagentsDbContext context,
    IDbEncryptionService dbEncryptionService) : DbContextInitializer(context, dbEncryptionService)
{
    protected override string SchemaName => ReagentsDbContext.Schema;

    public override Task SeedAsync()
    {
        return Task.CompletedTask;
    }

}