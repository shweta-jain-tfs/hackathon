using Pharos.Server.Infrastructure.Database;
using Pharos.Server.Infrastructure.Services;

namespace Pharos.Server.LabCommunity.Infrastructure.Database;

internal class LabCommunityDbContextInitializer(LabCommunityDbContext context, IDbEncryptionService dbEncryptionService)
    : DbContextInitializer(context, dbEncryptionService)
{
    protected override string SchemaName => LabCommunityDbContext.Schema;

    public override Task SeedAsync()
    {
        return Task.CompletedTask;
    }
}
