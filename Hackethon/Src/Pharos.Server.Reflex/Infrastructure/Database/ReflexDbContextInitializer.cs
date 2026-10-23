using Pharos.Server.Domain.Reflex;
using Pharos.Server.Infrastructure.Services;
using Pharos.Server.Reflex.Domain;

namespace Pharos.Server.Reflex.Infrastructure.Database;

internal sealed class ReflexDbContextInitializer(
    ReflexDbContext context,
    IDbEncryptionService dbEncryptionService) : DbContextInitializer(context, dbEncryptionService)
{
    protected override string SchemaName => ReflexDbContext.Schema;

    public override async Task SeedAsync()
    {
        await SeedReflexDefAsync();

        if (context.HasChanges())
        {
            await context.SaveChangesAsync().ConfigureAwait(false);
        }
    }

    private async Task SeedReflexDefAsync()
    {
        var hasReflexDef = await context.ReflexDefinitions.AsQueryable().AnyAsync().ConfigureAwait(false);

        if (!hasReflexDef)
        {
            var reflexDef = new ReflexDefinition
            {
                Name = "SeedReflexDef",
                Automatic = true,
                Active = true,
                RequestorId = "R0001",
                ReflexCondition = new ReflexCondition
                {
                    TestName = "c1",
                    MethodName = "sIgE",
                    ResultType = ResultType.Concentration,
                    CompareType = CompareType.Above,
                    ConcentrationCompareValue = 1
                }
            };

            reflexDef.ReflexTests.Add(new ReflexTest
            {
                TestName = "c2",
                MethodName = "sIgE",
                NumberOfReplicates = 1,
                InstrumentDilutionFactor = 1,
                TestSortOrder = 2,
            });

            context.ReflexDefinitions.Add(reflexDef);
        }
    }
}