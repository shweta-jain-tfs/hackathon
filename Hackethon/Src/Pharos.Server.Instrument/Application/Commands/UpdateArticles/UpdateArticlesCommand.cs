using MediatR;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Security;
using Pharos.Server.Application.Settings.Reagents;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Instrument.Application.Interfaces;
using Tetris.Protocol.Instrument.Articles;

namespace Pharos.Server.Instrument.Application.Commands.UpdateArticles;

internal record UpdateArticlesCommand(string InstrumentNumber, Guid? SessionId) : IStateSynchronizationCommand;

internal class UpdateArticlesCommandCommandHandler(
    ICommunicationSessionManager sessionManager,
    IArticlesService articlesService,
    IAuthorizationService authorizationService,
    IOptionsSnapshot<ReagentsSettings> settingsSnapshot,
    IPublisher publisher) : StateSynchronizationCommandHandlerBase<UpdateArticlesCommand>(sessionManager, CommunicationSynchronizationState.UpdateArticlesSent, publisher)
{
    protected override async Task DoHandle(UpdateArticlesCommand request, CommunicationSession session, CancellationToken cancellationToken)
    {
        var articles = await articlesService.GetArticles();
        var settings = settingsSnapshot.Value;

        var immunoCaps = articles.FilterAndMap(ArticleType.ImmunoCapCarrier, Mapping.ArticleMapper.ToCarrier);
        var artSet = settings.ImmunoCap;
        immunoCaps.ForEach(x =>
        {
            x.WasteTypeWhenEmpty = artSet.WasteTypeEmpty.ToEnumWasteType();
            x.WasteTypeWhenNotEmpty = artSet.WasteTypeNonEmpty.ToEnumWasteType();
            x.UseExpiredReagents = authorizationService.RndMode && artSet.UseExpiredReagents;
        });

        var eliaWells = articles.FilterAndMap(ArticleType.EliaWellCarrier, Mapping.ArticleMapper.ToCarrier);
        artSet = settings.EliaWell;
        eliaWells.ForEach(x =>
        {
            x.WasteTypeWhenEmpty = artSet.WasteTypeEmpty.ToEnumWasteType();
            x.WasteTypeWhenNotEmpty = artSet.WasteTypeNonEmpty.ToEnumWasteType();
            x.UseExpiredReagents = authorizationService.RndMode && artSet.UseExpiredReagents;
        });

        var eliaCalibratorWells = articles.FilterAndMap(ArticleType.EliaCalibratorWellCarrier, Mapping.ArticleMapper.ToCarrier);
        artSet = settings.EliaCalibratorWell;
        eliaCalibratorWells.ForEach(x =>
        {
            x.WasteTypeWhenEmpty = artSet.WasteTypeEmpty.ToEnumWasteType();
            x.WasteTypeWhenNotEmpty = artSet.WasteTypeNonEmpty.ToEnumWasteType();
            x.UseExpiredReagents = authorizationService.RndMode && artSet.UseExpiredReagents;
        });

        var dilutionWells = articles.FilterAndMap(ArticleType.DilutionWellCarrier, Mapping.ArticleMapper.ToCarrier);
        artSet = settings.DilutionWell;
        dilutionWells.ForEach(x =>
        {
            x.WasteTypeWhenEmpty = artSet.WasteTypeEmpty.ToEnumWasteType();
            x.WasteTypeWhenNotEmpty = artSet.WasteTypeNonEmpty.ToEnumWasteType();
            x.UseExpiredReagents = authorizationService.RndMode && artSet.UseExpiredReagents;
        });

        var conjugateCartridges = articles.FilterAndMap(ArticleType.ConjugateCartridge, Mapping.ArticleMapper.ToConjugate);
        artSet = settings.Conjugate;
        conjugateCartridges.ForEach(x =>
        {
            x.WasteTypeWhenEmpty = artSet.WasteTypeEmpty.ToEnumWasteType();
            x.WasteTypeWhenNotEmpty = artSet.WasteTypeNonEmpty.ToEnumWasteType();
            x.UseExpiredReagents = authorizationService.RndMode && artSet.UseExpiredReagents;
        });

        var calibratorCartridges = articles.FilterAndMap(ArticleType.CalibratorCartridge, Mapping.ArticleMapper.ToCalCcCartridge);
        artSet = settings.Calibrator;
        calibratorCartridges.ForEach(x =>
        {
            x.WasteTypeWhenEmpty = artSet.WasteTypeEmpty.ToEnumWasteType();
            x.WasteTypeWhenNotEmpty = artSet.WasteTypeNonEmpty.ToEnumWasteType();
            x.UseExpiredReagents = authorizationService.RndMode && artSet.UseExpiredReagents;
        });

        var curveControlCartridges = articles.FilterAndMap(ArticleType.CurveControlCartridge, Mapping.ArticleMapper.ToCalCcCartridge);
        artSet = settings.CurveControl;
        curveControlCartridges.ForEach(x =>
        {
            x.WasteTypeWhenEmpty = artSet.WasteTypeEmpty.ToEnumWasteType();
            x.WasteTypeWhenNotEmpty = artSet.WasteTypeNonEmpty.ToEnumWasteType();
            x.UseExpiredReagents = authorizationService.RndMode && artSet.UseExpiredReagents;
        });

        var qualityControlCartridges = articles.FilterAndMap(ArticleType.QualityControlCartridge, Mapping.ArticleMapper.ToQualityControl);
        artSet = settings.QualityControl;
        curveControlCartridges.ForEach(x =>
        {
            x.WasteTypeWhenEmpty = artSet.WasteTypeEmpty.ToEnumWasteType();
            x.WasteTypeWhenNotEmpty = artSet.WasteTypeNonEmpty.ToEnumWasteType();
            x.UseExpiredReagents = authorizationService.RndMode && artSet.UseExpiredReagents;
        });

        var developmentSolutionCartridges = articles.FilterAndMap(ArticleType.DevelopmentSolutionCartridge, Mapping.ArticleMapper.ToDevelopment);
        artSet = settings.DevelopmentSolution;
        developmentSolutionCartridges.ForEach(x =>
        {
            x.WasteTypeWhenEmpty = artSet.WasteTypeEmpty.ToEnumWasteType();
            x.WasteTypeWhenNotEmpty = artSet.WasteTypeNonEmpty.ToEnumWasteType();
            x.UseExpiredReagents = authorizationService.RndMode && artSet.UseExpiredReagents;
        });

        var diluentCartridges = articles.FilterAndMap(ArticleType.DiluentCartridge, Mapping.ArticleMapper.ToDiluent);
        artSet = settings.Diluent;
        diluentCartridges.ForEach(x =>
        {
            x.WasteTypeWhenEmpty = artSet.WasteTypeEmpty.ToEnumWasteType();
            x.WasteTypeWhenNotEmpty = artSet.WasteTypeNonEmpty.ToEnumWasteType();
            x.UseExpiredReagents = authorizationService.RndMode && artSet.UseExpiredReagents;
        });

        var stopSolutions = articles.FilterAndMap(ArticleType.StopSolutionBottle, Mapping.ArticleMapper.ToStopSolution);
        var value = settings.StopSolution.UseExpiredReagents;
        stopSolutions.ForEach(x => x.UseExpiredReagents = authorizationService.RndMode && value);

        var washConcentrates = articles.FilterAndMap(ArticleType.WashConcentrateBottle, Mapping.ArticleMapper.ToWashConcentrate);
        value = settings.WashConcentrate.UseExpiredReagents;
        washConcentrates.ForEach(x => x.UseExpiredReagents = authorizationService.RndMode && value);

        var washAdditives = articles.FilterAndMap(ArticleType.WashAdditiveBottle, Mapping.ArticleMapper.ToWashAdditive);
        value = settings.WashAdditive.UseExpiredReagents;
        washAdditives.ForEach(x => x.UseExpiredReagents = authorizationService.RndMode && value);

        var maintenanceConcentrates = articles.FilterAndMap(ArticleType.MaintenanceConcentrateBottle, Mapping.ArticleMapper.ToMaintenanceConcentrate);
        value = settings.MaintenanceConcentrate.UseExpiredReagents;
        maintenanceConcentrates.ForEach(x => x.UseExpiredReagents = authorizationService.RndMode && value);

        var fluoroCArticles = articles.FilterAndMap(ArticleType.FluoroCVial, Mapping.ArticleMapper.ToFluoroC);
        value = settings.FluoroC.UseExpiredReagents;
        fluoroCArticles.ForEach(x => x.UseExpiredReagents = authorizationService.RndMode && value);

        var qualityControlTube = articles.FilterAndMap(ArticleType.QualityControlTube, Mapping.ArticleMapper.ToQualityControlTube);
        value = settings.QualityControlTube.UseExpiredReagents;
        qualityControlTube.ForEach(x => x.UseExpiredReagents = authorizationService.RndMode && value);

        var articlesInfo = new Articles
        {
            ImmunoCaps = { immunoCaps },
            EliaWells = { eliaWells },
            EliaCalibrationWells = { eliaCalibratorWells },
            DilutionWells = { dilutionWells },
            ConjugateCartridges = { conjugateCartridges },
            CalibratorCartridges = { calibratorCartridges },
            CurveControlCartridges = { curveControlCartridges },
            QualityControlCartridges = { qualityControlCartridges },
            DevelopmentSolutionCartridges = { developmentSolutionCartridges },
            DiluentCartridges = { diluentCartridges },
            StopSolutions = { stopSolutions },
            WashConcentrates = { washConcentrates },
            WashAdditives = { washAdditives },
            MaintenanceConcentrates = { maintenanceConcentrates },
            FluoroCs = { fluoroCArticles },
            QualityControl = { qualityControlTube }
        };

        await session.Connection.UpdateArticlesAsync(articlesInfo, cancellationToken);
    }
}