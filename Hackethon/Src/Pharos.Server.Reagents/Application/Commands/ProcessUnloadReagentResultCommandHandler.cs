using System.Runtime.CompilerServices;
using FluentValidation;
using Google.Protobuf.Collections;
using MediatR;
using Microsoft.Extensions.Logging;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Reagents;
using Pharos.Server.Reagents.Application.Interfaces;
using Pharos.Server.Reagents.Application.Mapping;
using Pharos.Server.Reagents.Domain;
using Tetris.Protocol.Common;
using Tetris.Protocol.Pharos.Move;
using ReagentContent = Pharos.Server.Reagents.Domain.ReagentContent;

namespace Pharos.Server.Reagents.Application.Commands;

internal class ProcessUnloadReagentResultCommandHandler(
    IReagentsDbContext dbContext,
    IReagentBarcodeService reagentInfoService,
    IMethodsService methodsService,
    ILogger<ProcessUnloadReagentResultCommandHandler> logger) : IRequestHandler<ProcessUnloadReagentResultCommand>
{

    public async Task Handle(ProcessUnloadReagentResultCommand request, CancellationToken cancellationToken)
    {
        var unloaded = request.UnloadResult.Reagent;
        if (unloaded?.ContentCells == null || unloaded.ContentCells.Count == 0)
        {
            return;
        }

        var reagentBarcode = unloaded.ReagentBarcode;
        var reagentUdiBarcode = unloaded.ReagentUdiBarcode;
        var reagentType = unloaded.ReagentType;

        Reagent? reagent = null;
        if (IsCartridgeType(reagentType))
        {
            if (string.IsNullOrWhiteSpace(reagentBarcode))
            {
                throw new ValidationException("Reagent cartridge barcode is null or empty.");
            }
            else
            {
                reagent = await dbContext.Reagents
                    .Include(x => x.ReagentContents)
                    .FirstOrDefaultAsync(r => r.Barcode == reagentBarcode, cancellationToken);
            }
        }
        else if (IsCarrierType(reagentType))
        {
            if (string.IsNullOrWhiteSpace(reagentUdiBarcode) && string.IsNullOrWhiteSpace(reagentBarcode))
            {
                throw new ValidationException("Both reagent carrier UDI and barcode are null or empty.");
            }
            else
            {
                reagent = await dbContext.Reagents
                    .Include(x => x.ReagentContents)
                    .FirstOrDefaultAsync(r =>
                        (!string.IsNullOrWhiteSpace(reagentUdiBarcode) && r.Udi == reagentUdiBarcode) ||
                        (!string.IsNullOrWhiteSpace(reagentBarcode) && r.Barcode == reagentBarcode),
                        cancellationToken);
            }
        }
        else if (IsBottleType(reagentType))
        {
            if (string.IsNullOrWhiteSpace(reagentUdiBarcode))
            {
                throw new ValidationException("Reagent bottle UDI is null or empty.");
            }
            else
            {
                reagent = await dbContext.Reagents
                    .Include(x => x.ReagentContents)
                    .FirstOrDefaultAsync(r => r.Udi == reagentUdiBarcode, cancellationToken);
            }
        }

        reagent ??= await CreateReagent(reagentBarcode, reagentUdiBarcode, cancellationToken);

        var instrumentCells = unloaded.ContentCells;

        if (reagent.ReagentContents is { Count: > 0 })
        {
            if (IsCalibratorOrCurveControlType(reagentType))
            {
                var setsQuantity = await CalculateSetsQuantity(reagentBarcode, reagentUdiBarcode, reagentType, instrumentCells);
                StoreQuantityInFirstCell(reagent.ReagentContents, setsQuantity);
            }
            else
            {
                UpdateReagentContent(instrumentCells, reagent.ReagentContents, reagentType);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Reagent> CreateReagent(string reagentBarcode, string reagentUdiBarcode, CancellationToken cancellationToken)
    {
        logger.LogDebug("Getting reagent info from the barcode: Barcode = {Barcode}, UDI = {Udi}.", reagentBarcode, reagentUdiBarcode);

        // Let the service handle barcode/UDI logic and validation
        var barcode = !string.IsNullOrWhiteSpace(reagentBarcode) ? reagentBarcode : reagentUdiBarcode;
        if (string.IsNullOrWhiteSpace(barcode))
        {
            throw new ValidationException("Both reagent barcode and UDI are null or empty.");
        }

        var (articleType, _) = await reagentInfoService.DetectArticleType(barcode).ConfigureAwait(false);
        if (articleType == ArticleType.None)
        {
            throw new ValidationException($"Unable to detect article type from barcode: {barcode}");
        }

        var reagentInfo = await reagentInfoService.GetReagentInfo(articleType, reagentBarcode, reagentUdiBarcode).ConfigureAwait(false);

        if (reagentInfo == null)
        {
            throw new ValidationException("Unknown reagent.");
        }
        var article = reagentInfo.Article;
        if (string.IsNullOrWhiteSpace(reagentUdiBarcode) && article.ArticleType is ArticleType.ImmunoCapCarrier or ArticleType.EliaWellCarrier or ArticleType.EliaCalibratorWellCarrier)
        {
            throw new ValidationException("Carrier UDI is required.");
        }

        var lotInfo = reagentInfo.Lot;
        logger.LogDebug("Checking the reagent lot number {LotNumber} in the database.", lotInfo.LotNumber);
        var lotId = await GetLotId(lotInfo.LotNumber, cancellationToken);

        if (lotId == 0)
        {
            logger.LogDebug("Lot number {LotNumber} is not found in the database.", lotInfo.LotNumber);

            var lot = lotInfo.ToLot();
            dbContext.Lots.Add(lot);
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                lotId = lot.Id;
            }
            catch (InvalidOperationException)
            {
                logger.LogDebug("A concurrent database transaction created the lot number {LotNumber}. Fetching the lot ID.", lot.LotNumber);
                dbContext.Lots.Remove(lot);
                lotId = await GetLotId(lot.LotNumber, cancellationToken);
            }
        }

        var reagent = new Reagent
        {
            Barcode = reagentBarcode,
            Udi = reagentUdiBarcode,
            LoadTimestamp = null,
            LotId = lotId,
            OverrideExpiration = false,
            ReagentContents = new List<ReagentContent>()
        };
        if (article.NumberOfCells == null)
        {
            reagent.ReagentContents.Add(new ReagentContent
            {
                Quantity = article.Quantity,
                ExpiryDate = reagentInfo.Lot.ExpiryDate,
                //In case of carriers and bottle cell position is always 1
                CellPosition = 1,
            });
        }
        else
        {
            for (var i = 1; i <= article.NumberOfCells; i++)
            {
                reagent.ReagentContents.Add(new ReagentContent
                {
                    CellPosition = i,
                    Quantity = article.Quantity / article.NumberOfCells.Value,
                    ExpiryDate = reagentInfo.Lot.ExpiryDate,
                });
            }
        }

        dbContext.Reagents.Add(reagent);

        return reagent;
    }

    private ConfiguredTaskAwaitable<int> GetLotId(string lotNumber, CancellationToken cancellationToken)
    {
        return dbContext.Lots
            .Where(l => l.LotNumber == lotNumber)
            .Select(l => l.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private static void UpdateReagentContent(RepeatedField<UnloadedReagentCell> instrumentCells, IList<ReagentContent> dbCells, EnumReagentType reagentType)
    {
        foreach (var cell in instrumentCells)
        {
            var dbCell = dbCells.FirstOrDefault(x => x.CellPosition == cell.Position);
            if (dbCell == null)
            {
                // this should not normally happen, only if someone explicitly deleted records from the database
                continue;
            }

            var content = cell.Content;
            if (content.OpenTimestamp != null && content.OpenTimestamp.ToDateTimeOffset() != DateTimeOffset.MinValue)
            {
                dbCell.OpenTimestamp = content.OpenTimestamp.ToDateTimeOffset();
            }

            if (content.ExpiryDate != null)
            {
                dbCell.ExpiryDate = content.ExpiryDate.ToDateTimeOffset();
            }

            //if cartridge cell is pierced, set quantity to 0
            var isPierced = IsCellPierced(dbCell.OpenTimestamp);
            dbCell.Quantity = isPierced && IsCartridgeType(reagentType) ? 0 : content.RemainingQuantity;
            dbCell.Unusable = content.Unusable;
        }
    }

    private static readonly HashSet<EnumReagentType> _cartridgeEnumTypes = new()
    {
        EnumReagentType.CalibratorCartridge,
        EnumReagentType.ConjugateCartridge,
        EnumReagentType.CurveControlCartridge,
        EnumReagentType.DevelopmentSolutionCartridge,
        EnumReagentType.DiluentCartridge,
        EnumReagentType.FluoroC,
        EnumReagentType.QualityControlCartridge
    };

    private static readonly HashSet<EnumReagentType> _carrierEnumTypes = new()
    {
        EnumReagentType.ImmunoCapCarrier,
        EnumReagentType.EliaWellCarrier,
        EnumReagentType.EliaCalibrationWellCarrier
    };

    private static readonly HashSet<EnumReagentType> _bottleEnumTypes = new()
    {
        EnumReagentType.StopSolution,
        EnumReagentType.WashAdditive,
        EnumReagentType.WashConcentrate,
        EnumReagentType.MaintenanceConcentrate
    };

    private static bool IsCartridgeType(EnumReagentType reagentType) => _cartridgeEnumTypes.Contains(reagentType);
    private static bool IsCarrierType(EnumReagentType reagentType) => _carrierEnumTypes.Contains(reagentType);
    private static bool IsBottleType(EnumReagentType reagentType) => _bottleEnumTypes.Contains(reagentType);
    private static bool IsCalibratorOrCurveControlType(EnumReagentType reagentType) => 
        reagentType is EnumReagentType.CalibratorCartridge or EnumReagentType.CurveControlCartridge;

    private static bool IsCellPierced(DateTimeOffset? openTimestamp) => openTimestamp.HasValue && openTimestamp.Value != DateTimeOffset.MinValue;

    /// <summary>
    /// Calculates the number of sets based on remaining (unpierced) cell count for calibrator and curve control cartridges
    /// </summary>
    private async Task<int> CalculateSetsQuantity(string reagentBarcode, string reagentUdiBarcode, EnumReagentType reagentType, RepeatedField<UnloadedReagentCell> instrumentCells)
    {
        // Count unpierced cells from instrument data
        var remainingCells = instrumentCells.Count(cell => 
            cell.Content.OpenTimestamp == null || 
            cell.Content.OpenTimestamp.ToDateTimeOffset() == DateTimeOffset.MinValue);
        
        if (reagentType == EnumReagentType.CalibratorCartridge)
        {
            return (int)Math.Floor(remainingCells / 6.0);
        }
        
        if (reagentType == EnumReagentType.CurveControlCartridge)
        {
            var barcode = !string.IsNullOrWhiteSpace(reagentBarcode) ? reagentBarcode : reagentUdiBarcode;
            if (!string.IsNullOrWhiteSpace(barcode))
            {
                var (articleType, _) = await reagentInfoService.DetectArticleType(barcode).ConfigureAwait(false);
                var reagentInfo = await reagentInfoService.GetReagentInfo(articleType, reagentBarcode, reagentUdiBarcode).ConfigureAwait(false);
                
                var methodName = reagentInfo?.Article?.MethodName;
                if (!string.IsNullOrEmpty(methodName))
                {
                    var ccCount = await methodsService.GetMethodCurveControlCount(methodName).ConfigureAwait(false);
                    if (ccCount > 0)
                    {
                        return (int)Math.Floor(remainingCells / (double)ccCount);
                    }
                }
            }
        }
        
        return 0;
    }

    /// <summary>
    /// Stores the calculated quantity (in sets) in the first cell of the reagent
    /// </summary>
    private static void StoreQuantityInFirstCell(IList<ReagentContent> dbCells, int quantity)
    {
        var firstCell = dbCells.FirstOrDefault(c => c.CellPosition == 1);
        if (firstCell != null)
        {
            firstCell.Quantity = quantity;
        }
    }
}