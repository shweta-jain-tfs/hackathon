using Google.Protobuf.WellKnownTypes;
using MediatR;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Reagents;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Parameters;
using Pharos.Server.Reagents.Application.Interfaces;
using Pharos.Server.Reagents.Application.Mapping;
using Pharos.Server.Reagents.Domain;

namespace Pharos.Server.Reagents.Application.Queries;

internal record GetReagentContentDetailQuery(string? Barcode, string? ReagentUdi, string InstrumentNumber) : IRequest<ReagentContentDetailDto>;
internal class GetReagentContentDetailQueryHandler(
    IReagentsDbContext dbContext, 
    IReagentBarcodeService reagentBarcodeService, 
    IMethodsService methodsService,
    IInstrumentReagentStorage instrumentReagentStorage) : IRequestHandler<GetReagentContentDetailQuery, ReagentContentDetailDto>
{
    public async Task<ReagentContentDetailDto> Handle(GetReagentContentDetailQuery request, CancellationToken cancellationToken)
    {
        var reagentContentDetailDto = new ReagentContentDetailDto();

        if ((request.Barcode == null || string.IsNullOrWhiteSpace(request.Barcode))
                && (request.ReagentUdi == null || string.IsNullOrWhiteSpace(request.ReagentUdi)))
        {
            return reagentContentDetailDto;
        }
        var reagentInfo = await GetReagentInfo(request).ConfigureAwait(false);

        if (reagentInfo != null)
        {
            reagentContentDetailDto = reagentInfo.ToReagentContentDetailDto();

            var type = reagentContentDetailDto.Type;
            
            var dbReagent = await dbContext.Reagents
                .Include(r => r.ReagentContents)
                .FirstOrDefaultAsync(r => r.Barcode == request.Barcode, cancellationToken);

            if (type == ArticleType.CalibratorCartridge || type == ArticleType.CurveControlCartridge)
            {
                // First check if quantity is stored in the database (in the first cell)
                var dbQuantity = GetQuantityFromDatabase(dbReagent);
                if (dbQuantity.HasValue)
                {
                    reagentContentDetailDto.Quantity = dbQuantity.Value;
                }
                else
                {
                    // Fall back to live instrument calculation if not in database
                    reagentContentDetailDto.Quantity = await GetQuantityForCalibratorsAndCurveControls(request.Barcode, reagentInfo, request.InstrumentNumber);
                }
                reagentContentDetailDto.UnitOfMeasure = ArticleQuantityUnit.Sets;
            }
            else
            {
                if (dbReagent != null && dbReagent.ReagentContents is not null and { Count: > 0 })
                {
                    var isReagentCartridgeType = IsArticleCartridgeType(type);

                    if (isReagentCartridgeType)
                    {
                        // only sum quantities which is not pierced
                        reagentContentDetailDto.Quantity = dbReagent.ReagentContents
                            .Where(rc => rc.OpenTimestamp == null || rc.OpenTimestamp.Value == DateTimeOffset.MinValue)
                            .Sum(rc => rc.Quantity);
                    }
                    else
                    {
                        reagentContentDetailDto.Quantity = dbReagent.ReagentContents.Sum(rc => rc.Quantity);
                    }
                }

                if(IsArticleBottleType(type))
                {
                    reagentContentDetailDto.Size = reagentInfo.Article.Quantity <= Constants.BottleCutoffQuantityForSize ? ReagentSize.Small : ReagentSize.Large;
                }
                 else
                {
                    reagentContentDetailDto.Size = ReagentSize.NotApplicable;
                }
            }
        }

        return reagentContentDetailDto;
    }

    private async Task<int> GetQuantityForCalibratorsAndCurveControls(string? barcode, ReagentInfo reagentInfo, string instrumentNumber)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        {
            return 0;
        }

        var cartridge = FindCartridgeInInstruments(barcode, instrumentNumber);
        
        if (cartridge == null)
        {
            // Cartridge not found in any instrument - cannot determine pierced cells, return 0
            return 0;
        }

        // Count unpierced cells from live instrument data
        var remainingCells = cartridge.Cells.Count(cell => IsDateEmpty(cell.Content.OpenTimestamp));
        
        return await CalculateSets(remainingCells, reagentInfo);
    }

    /// <summary>
    /// Searches all instruments for a cartridge with the specified barcode
    /// </summary>
    private Tetris.Protocol.Pharos.Storage.Cartridge? FindCartridgeInInstruments(string barcode, string instrumentNumber)
    {
        var cartridges = instrumentReagentStorage.GetCartridges(instrumentNumber);
        var cartridge = cartridges.FirstOrDefault(c => c.ReagentBarcode == barcode);

        if (cartridge != null)
        {
            return cartridge;
        }

        return null;
    }

    /// <summary>
    /// Calculates the number of sets based on remaining (unpierced) cell count
    /// </summary>
    private async Task<int> CalculateSets(int remainingCells, ReagentInfo reagentInfo)
    {
        var sets = 0;
        
        if (reagentInfo.Article.ArticleType == ArticleType.CalibratorCartridge)
        {
            sets = (int)Math.Floor(remainingCells / 6.0);
        }
        else if (reagentInfo.Article.ArticleType == ArticleType.CurveControlCartridge)
        {
            var methodName = reagentInfo.Article.MethodName;
            
            if (!string.IsNullOrEmpty(methodName))
            {
                var ccCount = await methodsService.GetMethodCurveControlCount(methodName);
                
                if (ccCount > 0)
                {
                    sets = (int)Math.Floor(remainingCells / (double)ccCount);
                }
            }
        }
        
        return sets;
    }

    /// <summary>
    /// Checks if a cell's OpenTimestamp indicates it has not been pierced
    /// </summary>
    private static bool IsDateEmpty(Timestamp timestamp)
    {
        return timestamp.ToDateTime() == DateTime.MinValue;
    }

    private async Task<ReagentInfo?> GetReagentInfo(GetReagentContentDetailQuery request)
    {
        var barcode = !string.IsNullOrWhiteSpace(request.Barcode) ? request.Barcode : request.ReagentUdi;
        if (string.IsNullOrWhiteSpace(barcode))
        {
            return null;
        }
        else
        {
            var (articleType, _) = await reagentBarcodeService.DetectArticleType(barcode);
            var reagentInfo = await reagentBarcodeService.GetReagentInfo(articleType, request?.Barcode, request?.ReagentUdi);
            return reagentInfo;
        }
    }

    private static bool IsArticleCartridgeType(ArticleType? type)
    {
        return type is ArticleType.CalibratorCartridge or
            ArticleType.ConjugateCartridge or
            ArticleType.CurveControlCartridge or
            ArticleType.DevelopmentSolutionCartridge or
            ArticleType.DiluentCartridge or
            ArticleType.QualityControlCartridge;
    }

    /// <summary>
    /// Gets the quantity from the first cell of the reagent in the database.
    /// Returns null if the reagent is not found or has no contents.
    /// </summary>
    private static int? GetQuantityFromDatabase(Reagent? dbReagent)
    {
        if (dbReagent?.ReagentContents is not { Count: > 0 })
        {
            return null;
        }

        var firstCell = dbReagent.ReagentContents.FirstOrDefault(c => c.CellPosition == 1);
        return firstCell?.Quantity;
    }

    private static bool IsArticleBottleType(ArticleType? articleType)
    {
        return articleType == ArticleType.StopSolutionBottle ||
               articleType == ArticleType.WashAdditiveBottle ||
               articleType == ArticleType.WashConcentrateBottle;
    }
}
