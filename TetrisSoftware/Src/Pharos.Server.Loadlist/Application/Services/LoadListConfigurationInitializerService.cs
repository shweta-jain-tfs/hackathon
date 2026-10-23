using Microsoft.EntityFrameworkCore;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.LoadList.Application.Mapping;
using Pharos.Server.LoadList.Domain;
using Method = Pharos.Server.Domain.Parameters.Methods.Method;

namespace Pharos.Server.LoadList.Application.Services;

internal class LoadListConfigurationInitializerService(
    ILoadListDbContext dbContext,
    IMethodsService methodsService,
    IArticlesService articlesService) : ILoadListConfigurationInitializerService
{
    public async Task InitializeForInstrument(string instrumentSerial, CancellationToken cancellationToken)
    {
        // Fetch enabled detailed methods once
        var methods = await methodsService.GetDetailedMethods();

        await InitializeDilutionPlate(instrumentSerial, cancellationToken);
        await InitializeStopBottleConfiguration(instrumentSerial, cancellationToken);
        await InitializeCalAndCcConfigurations(instrumentSerial, methods, cancellationToken);
        await InitializeConjugateConfiguration(instrumentSerial, methods, cancellationToken);
        await InitializePhadiaQualityControlCartridgeConfiguration(instrumentSerial, cancellationToken);
        await InitializeDevelopmentConfiguration(instrumentSerial, methods, cancellationToken);
        await InitializeDiluentConfiguration(instrumentSerial, methods, cancellationToken);
        await InitializeCarriersConfiguration(instrumentSerial, methods, cancellationToken);
        await InitializeMethodSpecificBottleConfiguration(instrumentSerial, methods, cancellationToken);
    }

    public async Task InitializeQcForInstrument(string instSerialNumber, CancellationToken cancellationToken)
    {
        await InitializePhadiaQualityControlCartridgeConfiguration(instSerialNumber, cancellationToken);
    }

    private async Task InitializeDilutionPlate(string instrumentId, CancellationToken ct)
    {
        var existing = await dbContext.LoadListConfigurations.Where(x => x.InstrumentSerial == instrumentId && x.ArticleType == ArticleType.DilutionPlate).ToHashSetAsync(ct);

        const string plateKey = nameof(ArticleType.DilutionPlate);

        await SyncLoadListConfigurations(
            instrumentId,
            ArticleType.DilutionPlate,
            new HashSet<string> { plateKey },
            existing,
            _ => plateKey,
            tuple => tuple,
            _ => null,
            ct);
    }

    private async Task InitializeStopBottleConfiguration(string instrumentSerial, CancellationToken cancellationToken)
    {
        //Stop solution bottles are method independent and are stored in article table
        var stopSolutionIdentities = await articlesService.GetArticles(x => x.ArticleType == ArticleType.StopSolutionBottle, x => x.Identity);

        var existing = await dbContext.LoadListConfigurations
            .Where(x => x.InstrumentSerial == instrumentSerial && x.ArticleType == ArticleType.StopSolutionBottle)
            .ToHashSetAsync(cancellationToken);

        await SyncLoadListConfigurations(
            instrumentSerial,
            ArticleType.StopSolutionBottle,
            stopSolutionIdentities.ToHashSet(),
            existing,
            x => x.Identity!,
            tuple => tuple,
            _ => null,
            cancellationToken);
    }
    
    private async Task InitializeMethodSpecificBottleConfiguration(string instrumentId, List<Method> methods, CancellationToken ct)
    {
        var tuples = methods
            .SelectMany(m => m.Reagents)
            .Where(r => r is { ReagentIdentity: not null, ReagentType: ReagentType.WashAdditive or ReagentType.WashSolutionConcentrate or ReagentType.StopSolution })
            .Select(r => (ArticleType: GetArticleType(r.ReagentType), Identity: r.ReagentIdentity))
            .Where(t => t.Identity != null)
            .ToHashSet();

        var existing = await dbContext.LoadListConfigurations
            .Where(x => x.InstrumentSerial == instrumentId &&
                        (x.ArticleType == ArticleType.WashConcentrateBottle || x.ArticleType == ArticleType.WashAdditiveBottle || x.ArticleType == ArticleType.StopSolutionBottle))
            .ToHashSetAsync(ct);

        var washConcActive = tuples.Where(x => x.ArticleType == ArticleType.WashConcentrateBottle).Select(y=>y.Identity).ToHashSet();
        var washConcExisting = existing.Where(x => x.ArticleType == ArticleType.WashConcentrateBottle).ToHashSet();
        await SyncLoadListConfigurations(
            instrumentId,
            ArticleType.WashConcentrateBottle,
            washConcActive!,
            washConcExisting,
            x => x.Identity,
            tuple => tuple,
            _ => null,
            ct);

        var washAddActive = tuples.Where(x => x.ArticleType == ArticleType.WashAdditiveBottle).Select(y=>y.Identity).ToHashSet();
        var washAddExisting = existing.Where(x => x.ArticleType == ArticleType.WashAdditiveBottle).ToHashSet();
        await SyncLoadListConfigurations(
            instrumentId,
            ArticleType.WashAdditiveBottle,
            washAddActive!,
            washAddExisting,
            x => x.Identity,
            tuple => tuple,
            _ => null,
            ct);
    }

    private async Task InitializeDiluentConfiguration(string instrumentId, List<Method> methods, CancellationToken ct)
    {
        var diluent = methods
            .SelectMany(m => m.Tests)
            .Where(t => t.Active)
            .Select(t => t.DiluentIdentity)
            .ToHashSet();

        var existing = await dbContext.LoadListConfigurations
            .Where(x => x.InstrumentSerial == instrumentId && x.ArticleType == ArticleType.DiluentCartridge)
            .ToHashSetAsync(ct);
        
        await SyncLoadListConfigurations(
            instrumentId,
            ArticleType.DiluentCartridge,
            diluent,
            existing,
            x => x.Identity!,
            tuple => tuple,
            _ => null,
            ct);
    }

    private async Task InitializeDevelopmentConfiguration(string instrumentId, List<Method> methods, CancellationToken ct)
    {
        var devSolutions = methods
            .SelectMany(m => m.Tests)
            .Where(t => t.Active && t.DevelopmentSolutionIdentity != null)
            .Select(t => t.DevelopmentSolutionIdentity!)
            .ToHashSet();

        var existing = await dbContext.LoadListConfigurations
            .Where(x => x.InstrumentSerial == instrumentId && x.ArticleType == ArticleType.DevelopmentSolutionCartridge)
            .ToHashSetAsync(ct);

        await SyncLoadListConfigurations(
            instrumentId,
            ArticleType.DevelopmentSolutionCartridge,
            devSolutions,
            existing,
            x => x.Identity!,
            tuple => tuple,
            _ => null,
            ct);
    }

    private async Task InitializePhadiaQualityControlCartridgeConfiguration(string instrumentId, CancellationToken ct)
    {
        var existing = await dbContext.LoadListConfigurations
            .Where(x => x.InstrumentSerial == instrumentId && x.ArticleType == ArticleType.QualityControlCartridge)
            .ToHashSetAsync(ct);

        var qcNames = await methodsService.GetPhadiaQualityControlCartridgeNames();

        await SyncLoadListConfigurations(
            instrumentId,
            ArticleType.QualityControlCartridge,
            qcNames,
            existing,
            x => x.Identity!,
            tuple => tuple,
            _ => null,
            ct);
    }

    private async Task InitializeConjugateConfiguration(string instrumentId, List<Method> methods, CancellationToken ct)
    {
        var activeTuples = methods
            .SelectMany(m => m.Reagents)
            .Where(r => r.ReagentType == ReagentType.Conjugate)
            .Select(r => (ArticleType.ConjugateCartridge, r.DefaultReagentIdentity))
            .ToHashSet();

        var existing = await dbContext.LoadListConfigurations
            .Where(x => x.InstrumentSerial == instrumentId && x.ArticleType == ArticleType.ConjugateCartridge)
            .ToHashSetAsync(ct);

        await SyncLoadListConfigurations(
            instrumentId,
            ArticleType.ConjugateCartridge,
            activeTuples,
            existing,
            x => (x.ArticleType, x.Identity),
            tuple => tuple.Item2,
            _ => null,
            ct);
    }

    private async Task InitializeCalAndCcConfigurations(string instrumentId, List<Method> methods, CancellationToken ct)
    {
        var existing = await dbContext.LoadListConfigurations
            .Where(x => x.InstrumentSerial == instrumentId &&
                        (x.ArticleType == ArticleType.CalibratorCartridge || x.ArticleType == ArticleType.CurveControlCartridge))
            .ToHashSetAsync(ct);

        var calActive = methods
            .Select(m => (ArticleType.CalibratorCartridge, m.MethodName))
            .ToHashSet();

        var calExisting = existing.Where(x => x.ArticleType == ArticleType.CalibratorCartridge).ToHashSet();
        await SyncLoadListConfigurations(
            instrumentId,
            ArticleType.CalibratorCartridge,
            calActive!,
            calExisting,
            x => (x.ArticleType, x.MethodName),
            _ => null,
            tuple => tuple.MethodName,
            ct);

        var ccTuples = methods
            .Select(m => (ArticleType.CurveControlCartridge, m.MethodName))
            .ToHashSet();

        var ccExisting = existing.Where(x => x.ArticleType == ArticleType.CurveControlCartridge).ToHashSet();
        await SyncLoadListConfigurations(
            instrumentId,
            ArticleType.CurveControlCartridge,
            ccTuples!,
            ccExisting,
            x => (x.ArticleType, x.MethodName),
            _ => null,
            tuple => tuple.MethodName,
            ct);
    }

    private async Task InitializeCarriersConfiguration(string instrumentId, List<Method> methods, CancellationToken ct)
    {
        var testNames = methods
            .SelectMany(m => m.Tests)
            .Where(t => t.Active)
            .Select(t => t.TestName)
            .Distinct()
            .ToHashSet();

        var existing = await dbContext.LoadListConfigurations
            .Where(x => x.InstrumentSerial == instrumentId &&
                        x.Identity != null &&
                        (x.ArticleType == ArticleType.ImmunoCapCarrier ||
                         x.ArticleType == ArticleType.EliaWellCarrier ||
                         x.ArticleType == ArticleType.EliaCalibratorWellCarrier ||
                         x.ArticleType == ArticleType.DilutionWellCarrier))
            .ToHashSetAsync(ct);

        await SyncLoadListConfigurations(
            instrumentId,
            null,
            testNames,
            existing,
            x => x.Identity!,
            tuple => tuple,
            _ => null,
            ct);
    }

    private async Task SyncLoadListConfigurations<TTuple>(
        string instrumentId,
        ArticleType? articleType,
        HashSet<TTuple> activeTuples,
        HashSet<LoadListConfiguration> existingConfigs,
        Func<LoadListConfiguration, TTuple> existingTupleSelector,
        Func<TTuple, string?> identitySelector,
        Func<TTuple, string?> methodNameSelector,
        CancellationToken ct)
    {
        var existingTuples = existingConfigs.Select(existingTupleSelector).ToHashSet();

        // Remove obsolete
        var toRemove = existingConfigs.Where(x => !activeTuples.Contains(existingTupleSelector(x))).ToList();
        if (toRemove.Count > 0)
        {
            dbContext.LoadListConfigurations.RemoveRange(toRemove);
        }

        // Add new
        var toAdd = activeTuples
            .Where(tuple => !existingTuples.Contains(tuple))
            .Select(tuple => new LoadListConfiguration
            {
                InstrumentSerial = instrumentId,
                Identity = identitySelector(tuple),
                MethodName = methodNameSelector(tuple),
                RefillLevel = 0,
                TriggerLevel = 0,
                ArticleType = articleType ?? ArticleType.None
            })
            .ToList();

        var enriched = new List<LoadListConfiguration>();
        foreach (var config in toAdd)
        {
            if (config.MethodName == "View")
            {
                config.Identity = "View";
                config.MethodName = null;
            }

            var articleInfo = await GetArticle(config);

            if (articleInfo == null)
            {
                continue;
            }

            LoadListConfigurationMapper.UpdateFromArticle(articleInfo, config);
            
            enriched.Add(config);
        }

        if (enriched.Count > 0)
        {
            await dbContext.LoadListConfigurations.AddRangeAsync(enriched, ct);
        }
    }

    private async Task<Article?> GetArticle(LoadListConfiguration config)
    {
        if (config.ArticleType == ArticleType.DilutionPlate)
        {
            var dilutionPlates = await articlesService.GetArticles(x => x.ArticleType == ArticleType.DilutionPlate, x => x);
            return dilutionPlates.FirstOrDefault();
        }

        return config.ArticleType == ArticleType.None
            ? await articlesService.GetArticleFromIdentity(config.Identity, config.MethodName, null)
            : await articlesService.GetArticleFromIdentity(config.Identity, config.MethodName, config.ArticleType);
    }

    private static ArticleType? GetArticleType(ReagentType reagentType)
    {
        return reagentType switch
        {
            ReagentType.Conjugate => ArticleType.ConjugateCartridge,
            ReagentType.DevelopmentSolution => ArticleType.DevelopmentSolutionCartridge,
            ReagentType.StopSolution => ArticleType.StopSolutionBottle,
            ReagentType.WashSolutionConcentrate => ArticleType.WashConcentrateBottle,
            ReagentType.Calibrator => ArticleType.CalibratorCartridge,
            ReagentType.CurveControl => ArticleType.CurveControlCartridge,
            ReagentType.WashAdditive => ArticleType.WashAdditiveBottle,
            _ => null
        };
    }
}