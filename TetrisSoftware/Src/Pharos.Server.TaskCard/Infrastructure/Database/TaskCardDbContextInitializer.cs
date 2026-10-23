using Pharos.Server.Infrastructure.Database;
using Pharos.Server.Infrastructure.Services;

namespace Pharos.Server.TaskCard.Infrastructure.Database;

internal sealed class TaskCardDbContextInitializer(
    TaskCardDbContext context,
    IDbEncryptionService dbEncryptionService) : DbContextInitializer(context, dbEncryptionService)
{
    protected override string SchemaName => TaskCardDbContext.Schema;

    public override Task SeedAsync()
    {
        return Task.CompletedTask;
    }
}