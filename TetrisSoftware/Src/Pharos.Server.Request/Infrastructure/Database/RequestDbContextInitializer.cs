using Pharos.Server.Infrastructure.Database;
using Pharos.Server.Infrastructure.Services;

namespace Pharos.Server.Request.Infrastructure.Database;

internal sealed class RequestDbContextInitializer(
    RequestDbContext context,
    IDbEncryptionService dbEncryptionService) : DbContextInitializer(context, dbEncryptionService)
{
    protected override string SchemaName => RequestDbContext.Schema;

    public override Task SeedAsync()
    {
        return Task.CompletedTask;
    }
}