using Pharos.Server.Infrastructure.Services;
using Pharos.Server.Sample.Domain;

namespace Pharos.Server.Sample.Infrastructure.Database;

internal class SampleDbContextInitializer(
    SampleDbContext context,
    IDbEncryptionService dbEncryptionService) : DbContextInitializer(context, dbEncryptionService)
{
    protected override string SchemaName => SampleDbContext.Schema;

    public override Task SeedAsync()
    {
        return SeedSampleTraysAsync();
    }

    private async Task SeedSampleTraysAsync()
    {
        var hasSampleTrays = await context.SampleTrays.AsQueryable().AnyAsync().ConfigureAwait(false);

        if (!hasSampleTrays)
        {
            var sampleTrays = new List<SampleTray>()
            {
                new()
                {
                    Id = "7897",
                },
                new()
                {
                    Id = "6147",
                },
                new()
                {
                    Id = "8340",
                },
                new()
                {
                    Id = "3644",
                },
                new()
                {
                    Id = "1707",
                },
                new()
                {
                    Id = "5555",
                },
            };

            context.SampleTrays.AddRange(sampleTrays);
        }
    }
}