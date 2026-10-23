using Pharos.Server.Domain.Parameters;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Infrastructure.Database;
using Pharos.Server.Infrastructure.Services;
using Pharos.Server.Localization;

namespace Pharos.Server.Parameters.Infrastructure.Database;

internal class ParametersDbContextInitializer(
    ParametersDbContext context,
    IDbEncryptionService dbEncryptionService) : DbContextInitializer(context, dbEncryptionService)
{
    protected override string SchemaName => ParametersDbContext.Schema;

    public override async Task InitializeAsync(RecreateObjectsMode recreateMode)
    {
        await base.InitializeAsync(recreateMode);

        await InitializePreDefinedMarkets();
    }

    private async Task InitializePreDefinedMarkets()
    {
        var marketsInDatabase = await context.IntendedMarkets
            .Where(x => IntendedMarket.PredefinedMarketsIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync()
            .ConfigureAwait(false);

        if (!marketsInDatabase.Contains(IntendedMarket.Worldwide))
        {
            context.IntendedMarkets.Add(new IntendedMarket { Id = IntendedMarket.Worldwide, Name = Translations.IntendedMarket_Worldwide });
        }

        if (!marketsInDatabase.Contains(IntendedMarket.Japan))
        {
            context.IntendedMarkets.Add(new IntendedMarket { Id = IntendedMarket.Japan, Name = Translations.IntendedMarket_Japan });
        }

        if (!marketsInDatabase.Contains(IntendedMarket.US))
        {
            context.IntendedMarkets.Add(new IntendedMarket { Id = IntendedMarket.US, Name = Translations.IntendedMarket_US });
        }

        if (!marketsInDatabase.Contains(IntendedMarket.Taiwan))
        {
            context.IntendedMarkets.Add(new IntendedMarket { Id = IntendedMarket.Taiwan, Name = Translations.IntendedMarket_Taiwan });
        }

        if (!marketsInDatabase.Contains(IntendedMarket.China))
        {
            context.IntendedMarkets.Add(new IntendedMarket { Id = IntendedMarket.China, Name = Translations.IntendedMarket_China });
        }

        if (context.ChangeTracker.HasChanges())
        {
            await context.SaveChangesAsync().ConfigureAwait(false);
        }
    }

    public override async Task SeedAsync()
    {
        await SeedMethodQualityControlInformation();
    }


    private async Task SeedMethodQualityControlInformation()
    {
        var qcSige = await context.PhadiaQualityControlTests
            .Include(x => x.QualityControl)
            .Include(q => q.Test)
                .ThenInclude(mt => mt.Method)
            .Where(q => q.QualityControl.Name == "sIgE Pos" && q.Test.Method.MethodName == "sIgE" && q.Test.TestName == "d1")
            .FirstOrDefaultAsync();
        if (qcSige != null)
        {
            qcSige.ReplicateCvLimit = 10;

            if (!await context.MethodQualityControlLot.AnyAsync(l => l.LotNumber == "AVMSS").ConfigureAwait(false))
            {
                context.MethodQualityControlLot.Add(new MethodQualityControlLot
                {
                    MethodQualityControlId = qcSige.Id,
                    ExpirationDate = DateTimeOffset.Now,
                    LotNumber = "AVMSS",
                    ExpectedConcentration = 2.4,
                    ExpectedStdDev = 0.3,
                    ExcludeRangeMin = false,
                    ExcludeRangeMax = false,
                    RangeMin = 2.1,
                    RangeMax = 3.1,
                    StdDevLimit = 2
                });
            }
        }

        var qcTige = await context.PhadiaQualityControlTests
            .Include(x => x.QualityControl)
            .Include(q => q.Test)
            .ThenInclude(mt => mt.Method)
            .Where(q => q.QualityControl.Name == "tIgE M" && q.Test.Method.MethodName == "tIgE" && q.Test.TestName == "a-IgE")
            .FirstOrDefaultAsync();
        if (qcTige != null)
        {
            qcTige.ReplicateCvLimit = 10;

            if (!await context.MethodQualityControlLot.AnyAsync(l => l.LotNumber == "AVMSS").ConfigureAwait(false))
            {
                context.MethodQualityControlLot.Add(new MethodQualityControlLot
                {
                    MethodQualityControlId = qcTige.Id,
                    ExpirationDate = DateTimeOffset.Now,
                    LotNumber = "AWASS",
                    ExpectedConcentration = 2.4,
                    ExpectedStdDev = 0.3,
                    ExcludeRangeMin = false,
                    ExcludeRangeMax = false,
                    RangeMin = 2.1,
                    RangeMax = 3.1,
                    StdDevLimit = 2
                });
            }
        }

        var qcElg = await context.PhadiaQualityControlTests
            .Include(x => x.QualityControl)
            .Include(q => q.Test)
            .ThenInclude(mt => mt.Method)
            .Where(q => q.QualityControl.Name == "CCP Pos" && q.Test.Method.MethodName == "El-G" && q.Test.TestName == "cp")
            .FirstOrDefaultAsync();
        if (qcElg != null)
        {
            qcElg.ReplicateCvLimit = 10;

            if (!await context.MethodQualityControlLot.AnyAsync(l => l.LotNumber == "C9VSS").ConfigureAwait(false))
            {
                context.MethodQualityControlLot.Add(new MethodQualityControlLot
                {
                    MethodQualityControlId = qcElg.Id,
                    ExpirationDate = DateTimeOffset.Now,
                    LotNumber = "C9VSS",
                    ExpectedConcentration = 2.4,
                    ExpectedStdDev = 0.3,
                    ExcludeRangeMin = false,
                    ExcludeRangeMax = false,
                    RangeMin = 2.1,
                    RangeMax = 3.1,
                    StdDevLimit = 2
                });
            }
        }

        var qcElt = await context.PhadiaQualityControlTests
            .Include(x => x.QualityControl)
            .Include(q => q.Test)
            .ThenInclude(mt => mt.Method)
            .Where(q => q.QualityControl.Name == "Thyr Pos" && q.Test.Method.MethodName == "El-T" && q.Test.TestName == "tsr")
            .FirstOrDefaultAsync();
        if (qcElt != null)
        {
            qcElt.ReplicateCvLimit = 10;

            if (!await context.MethodQualityControlLot.AnyAsync(l => l.LotNumber == "DFZSS").ConfigureAwait(false))
            {
                context.MethodQualityControlLot.Add(new MethodQualityControlLot
                {
                    MethodQualityControlId = qcElt.Id,
                    ExpirationDate = DateTimeOffset.Now,
                    LotNumber = "DFZSS",
                    ExpectedConcentration = 2.4,
                    ExpectedStdDev = 0.3,
                    ExcludeRangeMin = false,
                    ExcludeRangeMax = false,
                    RangeMin = 2.1,
                    RangeMax = 3.1,
                    StdDevLimit = 2
                });
            }
        }
    }
}
