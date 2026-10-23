using Pharos.Server.Infrastructure.Services;

namespace Pharos.Server.Result.Infrastructure.Database;

internal class ResultDbContextInitializer(
    ResultDbContext context,
    IDbEncryptionService dbEncryptionService) : DbContextInitializer(context, dbEncryptionService)
{
    protected override string SchemaName => ResultDbContext.Schema;

    public override Task SeedAsync()
    {
        return Task.CompletedTask;
    }
}