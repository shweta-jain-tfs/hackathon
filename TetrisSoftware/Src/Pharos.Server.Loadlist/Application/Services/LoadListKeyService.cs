using Pharos.Server.Application.LoadList;
using Pharos.Server.Application.Reagents;
using Pharos.Server.LoadList.Domain;
using Tetris.Protocol.Common;
using Tetris.Protocol.Pharos.Consumables;
using Tetris.Protocol.Pharos.Storage;

namespace Pharos.Server.LoadList.Application.Services;

internal class LoadListKeyService : ILoadListKeyService, IStorageToLoadListKeyService
{
    private readonly IReagentBarcodeService _reagentBarcodeService;

    public LoadListKeyService(IReagentBarcodeService reagentBarcodeService)
    {
        _reagentBarcodeService = reagentBarcodeService;
    }

    public string GetKey(MissingConsumable item)
    {
        switch (item.Type)
        {
            //Bottles
            case EnumMissingConsumableType.MissingStop:
                return nameof(ArticleType.StopSolutionBottle);

            case EnumMissingConsumableType.MissingWashConcentrate:
                return item.ReagentName;

            case EnumMissingConsumableType.MissingWashAdditive:
                return item.ReagentName;

            //Carriers
            case EnumMissingConsumableType.MissingImmunoCapCarrier:
                return item.ReagentName;
            case EnumMissingConsumableType.MissingEliaWellCarrier:
                return item.ReagentName;
            case EnumMissingConsumableType.MissingEliaCalibrationWellCarrier:
                return item.ReagentName;
            case EnumMissingConsumableType.MissingDilutionWellCarrier:
                return item.ReagentName;

            //Cartridges
            case EnumMissingConsumableType.MissingCalibrator:
                return nameof(ArticleType.CalibratorCartridge) + item.ReagentName;

            case EnumMissingConsumableType.MissingConjugate:
                return nameof(ArticleType.ConjugateCartridge) + item.ReagentName;

            case EnumMissingConsumableType.MissingCurveControl:
                return nameof(ArticleType.CurveControlCartridge) + item.ReagentName;

            case EnumMissingConsumableType.MissingDiluent:
                return nameof(ArticleType.DiluentCartridge) + item.ReagentName;

            case EnumMissingConsumableType.MissingDevelopment:
                return nameof(ArticleType.DevelopmentSolutionCartridge) + item.ReagentName;

            //Dilution plate
            case EnumMissingConsumableType.MissingDilutionPlateCell:
                return nameof(ArticleType.DilutionPlate);

            //Sample
            case EnumMissingConsumableType.MissingSample:
                return item.ReagentName;

            //Quality control tube or cartridge
            case EnumMissingConsumableType.MissingQualityControl:
                return item.ReagentName;

            // These types do not have a direct mapping to an article, so we don't need to do anything with them here
            case EnumMissingConsumableType.MissingRinse:
            case EnumMissingConsumableType.MissingLiquidWasteSpace:
            case EnumMissingConsumableType.MissingBiohazardSolidWasteSpace:
            case EnumMissingConsumableType.MissingRecyclableSolidWasteSpace:
            case EnumMissingConsumableType.MissingMaintenanceConcentrate:
            case EnumMissingConsumableType.UnspecifiedMissingConsumableType:
                break;

            default:
                throw new ArgumentOutOfRangeException($"Unexpected MissingConsumableType: {item.Type}");
        }

        return string.Empty;
    }

    public string GetKey(LoadListConfiguration item)
    {
        return GetKey(item.ArticleType, item.Identity, item.MethodName);
    }

    public string GetKey(ArticleType articleType, string? articleIdentity, string? methodName)
    {
        switch (articleType)
        {
            case ArticleType.CalibratorCartridge:
            case ArticleType.CurveControlCartridge:
                return articleType + methodName;

            case ArticleType.DevelopmentSolutionCartridge:
            case ArticleType.DiluentCartridge:
            case ArticleType.ConjugateCartridge:
                return articleType + articleIdentity;

            case ArticleType.DilutionPlate:
                return nameof(ArticleType.DilutionPlate);

            case ArticleType.DilutionWellCarrier:
            case ArticleType.EliaCalibratorWellCarrier:
            case ArticleType.EliaWellCarrier:
            case ArticleType.ImmunoCapCarrier:
                return articleIdentity!;

            case ArticleType.QualityControlCartridge:
            case ArticleType.QualityControlTube:
                return articleIdentity!;

            case ArticleType.StopSolutionBottle:
                return nameof(ArticleType.StopSolutionBottle);

            case ArticleType.WashAdditiveBottle:
            case ArticleType.WashConcentrateBottle:
                return articleIdentity!;

            //ignore these
            case ArticleType.FluoroCVial:
            case ArticleType.MaintenanceConcentrateBottle:
            case ArticleType.None:
                break;

            default:
                throw new ArgumentOutOfRangeException($"Unexpected ArticleType: {articleType}");
        }

        throw new InvalidOperationException("Key cannot be null or empty.");
    }

    public string GetKey(CarrierInformation item)
    {
        return item.CarrierOcr;
    }

    public async Task<string> GetKey(Cartridge item)
    {
        if (item.ReagentType is EnumReagentType.CalibratorCartridge or
                                EnumReagentType.CurveControlCartridge)
        {
            var articleType = item.ReagentType switch
            {
                EnumReagentType.CalibratorCartridge => ArticleType.CalibratorCartridge,
                EnumReagentType.CurveControlCartridge => ArticleType.CurveControlCartridge,
                _ => throw new ArgumentOutOfRangeException($"Unexpected Cartridge, ReagentType: {item.ReagentType}")
            };

            //return item.ArticleType + item.MethodName;
            var reagent = await _reagentBarcodeService.GetReagentInfo(articleType, item.ReagentBarcode);
            return GetKey(reagent, x => x.Article.ArticleType + x.Article.MethodName!);
        }

        if (item.ReagentType is EnumReagentType.ConjugateCartridge)
        {
            //return item.ArticleType + item.Identity;
            var reagent = await _reagentBarcodeService.GetReagentInfo(ArticleType.ConjugateCartridge, item.ReagentBarcode);
            return GetKey(reagent, x => x.Article.ArticleType + x.Article.Identity!);
        }

        if (item.ReagentType is EnumReagentType.DevelopmentSolutionCartridge or
                                EnumReagentType.DiluentCartridge)
        {
            var articleType = item.ReagentType switch
            {
                EnumReagentType.DevelopmentSolutionCartridge => ArticleType.DevelopmentSolutionCartridge,
                EnumReagentType.DiluentCartridge => ArticleType.DiluentCartridge,
                _ => throw new ArgumentOutOfRangeException($"Unexpected Cartridge, ReagentType: {item.ReagentType}")
            };

            //return item.ArticleType + item.FriendlyName;
            var reagent = await _reagentBarcodeService.GetReagentInfo(articleType, item.ReagentBarcode);
            return GetKey(reagent, x => x.Article.ArticleType + x.Article.FriendlyName!);
        }

        if (item.ReagentType is EnumReagentType.QualityControlCartridge or
                                EnumReagentType.QualityControlTube)
        {
            var articleType = item.ReagentType switch
            {
                EnumReagentType.QualityControlCartridge => ArticleType.QualityControlCartridge,
                EnumReagentType.QualityControlTube => ArticleType.QualityControlTube,
                _ => throw new ArgumentOutOfRangeException($"Unexpected Cartridge, ReagentType: {item.ReagentType}")
            };
            var reagent = await _reagentBarcodeService.GetReagentInfo(articleType, item.ReagentBarcode);
            return GetKey(reagent, x => x.Article.FriendlyName!);
        }

        throw new ArgumentOutOfRangeException($"Unexpected Cartridge, ReagentType: {item.ReagentType}");
    }

    public async Task<string> GetKey(BottledReagent item)
    {
        if (item.ReagentType == EnumReagentType.StopSolution)
        {
            return nameof(ArticleType.StopSolutionBottle);
        }

        if (item.ReagentType is EnumReagentType.WashAdditive or
                                EnumReagentType.WashConcentrate)
        {
            var articleType = item.ReagentType switch
            {
                EnumReagentType.WashAdditive => ArticleType.WashAdditiveBottle,
                EnumReagentType.WashConcentrate => ArticleType.WashConcentrateBottle,
                _ => throw new ArgumentOutOfRangeException($"Unexpected bottle, ReagentType: {item.ReagentType}")
            };

            //return item.FriendlyName;
            var reagent = await _reagentBarcodeService.GetReagentInfo(articleType, null, item.ReagentUdiBarcode);
            return GetKey(reagent, x => x.Article.FriendlyName!);
        }

        throw new ArgumentOutOfRangeException($"Unexpected BottledReagent, ReagentType: {item.ReagentType}");
    }

    public async Task<string> GetKey(EnumReagentType reagentType, string reagentBarcode, string reagentUdiBarcode)
    {
        var articleType = GetArticleType(reagentType);
        var reagent = await _reagentBarcodeService.GetReagentInfo(articleType, reagentBarcode, reagentUdiBarcode);

        return GetKey(reagent, x => GetKey(x.Article.ArticleType, x.Article.Identity, x.Article.MethodName));
    }

    private static ArticleType GetArticleType(EnumReagentType reagentType)
    {
        return reagentType switch
        {
            EnumReagentType.QualityControlCartridge => ArticleType.QualityControlCartridge,
            EnumReagentType.CalibratorCartridge => ArticleType.CalibratorCartridge,
            EnumReagentType.CurveControlCartridge => ArticleType.CurveControlCartridge,
            EnumReagentType.ConjugateCartridge => ArticleType.ConjugateCartridge,
            EnumReagentType.DevelopmentSolutionCartridge => ArticleType.DevelopmentSolutionCartridge,
            EnumReagentType.DiluentCartridge => ArticleType.DiluentCartridge,
            EnumReagentType.ImmunoCapCarrier => ArticleType.ImmunoCapCarrier,
            EnumReagentType.EliaWellCarrier => ArticleType.EliaWellCarrier,
            EnumReagentType.EliaCalibrationWellCarrier => ArticleType.EliaCalibratorWellCarrier,
            EnumReagentType.DilutionWellCarrier => ArticleType.DilutionWellCarrier,
            EnumReagentType.StopSolution => ArticleType.StopSolutionBottle,
            EnumReagentType.WashAdditive => ArticleType.WashAdditiveBottle,
            EnumReagentType.WashConcentrate => ArticleType.WashConcentrateBottle,
            EnumReagentType.MaintenanceConcentrate => ArticleType.MaintenanceConcentrateBottle,
            EnumReagentType.FluoroC => ArticleType.FluoroCVial,
            EnumReagentType.DilutionPlate => ArticleType.DilutionPlate,
            EnumReagentType.QualityControlTube => ArticleType.QualityControlTube,
            _ => throw new ArgumentOutOfRangeException(nameof(reagentType), reagentType, null)
        };
    }

    private static string GetKey(ReagentInfo? reagent, Func<ReagentInfo, string> keySelector)
    {
        ArgumentNullException.ThrowIfNull(reagent);

        var key = keySelector(reagent);

        if (string.IsNullOrEmpty(key))
        {
            throw new InvalidOperationException("Key cannot be null or empty.");
        }

        return key;
    }
}
