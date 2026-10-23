using System.Drawing;
using MediatR;
using Pharos.Server.Application.Reagents;
using Pharos.Server.Domain;
using Pharos.Server.Reagents.Application.Interfaces;
using Tetris.Protocol.Common;
using Tetris.Protocol.Pharos.Storage;

namespace Pharos.Server.Reagents.Application.Queries;

public class GetLoadingPositionQuery : IRequest<LoadingPositionDto>
{
    public string InstrumentNumber { get; }
    public string Barcode { get; }

    public GetLoadingPositionQuery(string instrumentNumber, string barcode)
    {
        InstrumentNumber = instrumentNumber;
        Barcode = barcode;
    }
}

internal class GetLoadingPositionQueryHandler(
    IInstrumentReagentStorage instrumentReagentStorage,
    IReagentBarcodeService reagentBarcodeService,
    IReagentsDbContext dbContext) : IRequestHandler<GetLoadingPositionQuery, LoadingPositionDto>
{
    public async Task<LoadingPositionDto> Handle(GetLoadingPositionQuery request, CancellationToken cancellationToken)
    {
        // Detect article type from barcode
        var (articleType, _) = await reagentBarcodeService.DetectArticleType(request.Barcode).ConfigureAwait(false);

        if (articleType == ArticleType.None)
        {
            return new LoadingPositionDto
            {
                Status = LoadingPositionStatus.NotFound,
                ArticleType = ArticleType.None,
                Position = 0
            };
        }

        var reagentInfo = await reagentBarcodeService.GetReagentInfo(articleType, null, request.Barcode);

        var loadingPositionDto = new LoadingPositionDto();

        if (reagentInfo == null)
        {
            loadingPositionDto.Status = LoadingPositionStatus.NotFound;
            loadingPositionDto.Position = 0;
            return loadingPositionDto;
        }

        // Get bottles from storage to check available positions
        var bottles = instrumentReagentStorage.GetBottles(request.InstrumentNumber);

        // Find available position based on article type
        var (loadingPositionStatus, availablePosition) = await FindAvailablePosition(bottles, articleType, reagentInfo);

        loadingPositionDto.Status = loadingPositionStatus;
        loadingPositionDto.ArticleType = articleType;
        loadingPositionDto.Position = availablePosition;

        if (loadingPositionDto.Status == LoadingPositionStatus.Found)
        {
            loadingPositionDto.Id = HashCode.Combine(articleType, availablePosition);
            loadingPositionDto.Quantity = await GetSavedOrMaxQuantity(request.Barcode, articleType, reagentInfo.Article.Quantity);
            loadingPositionDto.UnitOfMeasure = reagentInfo.Article.UnitOfMeasure;
            loadingPositionDto.Name = reagentInfo.Article.FriendlyName;
            loadingPositionDto.LotNumber = reagentInfo.Lot.LotNumber;
            loadingPositionDto.ExpiryDate = reagentInfo.Lot.ExpiryDate.DateTime;
            loadingPositionDto.Colors = reagentInfo.Article.Colors.Select(c => c.HasValue ? ColorTranslator.ToHtml(c.Value) : null).ToList();
        }

        return loadingPositionDto;
    }

    private async Task<(LoadingPositionStatus, int)> FindAvailablePosition(IReadOnlyList<BottledReagent> bottles, ArticleType articleType, ReagentInfo scannedBottleInfo)
    {

        if(!IsValidBottleSizeScanned(articleType, scannedBottleInfo))
        {
            return (LoadingPositionStatus.NotFound, 0); 
        }

        var (reagentType, allowedPositions) = GetReagentTypeAndPositions(articleType, scannedBottleInfo);

        if (!allowedPositions.Any())
        {
            return (LoadingPositionStatus.NotFound, 0);
        }

        var usedPositions = bottles
            .Where(bottle => bottle.ReagentType == reagentType)
            .ToDictionary(bottle => bottle.Position, bottle => bottle.ReagentUdiBarcode);

        //Find the first free position in the slot for a bottle of this type
        var freePosition = allowedPositions.FirstOrDefault(position => !usedPositions.ContainsKey(position));

        if (freePosition == 0)
        {
            return (LoadingPositionStatus.NotFound, 0);
        }

        // Validate pair compatibility if this bottle type requires pair validation
        if (CheckForPair(articleType))
        {
            return await ValidatePairCompatibility(allowedPositions.ToList(), usedPositions, freePosition, scannedBottleInfo);
        }

        return (LoadingPositionStatus.Found, freePosition);
    }

    private async Task<(LoadingPositionStatus, int)> ValidatePairCompatibility(
        List<int> allowedPositions,
        Dictionary<int, string> usedPositions,
        int freePosition,
        ReagentInfo scannedBottleInfo)
    {
        var pairPosition = freePosition == allowedPositions.First() ? allowedPositions.Last() : allowedPositions.First();

        if (!usedPositions.TryGetValue(pairPosition, out var existingBottleInPairPositionUdi) ||
            string.IsNullOrWhiteSpace(existingBottleInPairPositionUdi))
        {
            // No pair validation needed - pair position is empty or contains an unknown bottle
            return (LoadingPositionStatus.Found, freePosition);
        }

        var isCompatiblePair = await AreBottlesValidPair(existingBottleInPairPositionUdi, scannedBottleInfo);

        return isCompatiblePair ? (LoadingPositionStatus.Found, freePosition) : (LoadingPositionStatus.NotValidPair, 0);
    }

    private async Task<bool> AreBottlesValidPair(string existingBottleUdi, ReagentInfo scannedBottleInfo)
    {
        var existingBottleInfo = await reagentBarcodeService.GetReagentInfo(scannedBottleInfo.Article.ArticleType, null, existingBottleUdi);

        return existingBottleInfo?.Article.Identity == scannedBottleInfo.Article.Identity;
    }

    private static (EnumReagentType, IReadOnlyList<int>) GetReagentTypeAndPositions(ArticleType articleType, ReagentInfo? reagentInfo)
    {
        var firstBottleIndex = GetFirstBottleIndex(articleType, reagentInfo?.Article.Quantity);
        var availablePositions = new[] { firstBottleIndex, firstBottleIndex + 1 };

        var reagentType = articleType switch
        {
            ArticleType.StopSolutionBottle => EnumReagentType.StopSolution,
            ArticleType.WashAdditiveBottle => EnumReagentType.WashAdditive,
            ArticleType.WashConcentrateBottle => EnumReagentType.WashConcentrate,
            _ => throw new ArgumentOutOfRangeException(nameof(articleType), articleType, "Unsupported article type for reagent positioning")
        };

        return (reagentType, availablePositions);
    }

    private async Task<int> GetSavedOrMaxQuantity(string reagentUdi, ArticleType articleType, int articleQuantity)
    {
        var dbReagent = await dbContext.Reagents
                .Include(r => r.ReagentContents)
                .FirstOrDefaultAsync(r => r.Udi == reagentUdi);

        if (dbReagent != null)
        {
            return dbReagent.ReagentContents?.FirstOrDefault()?.Quantity ?? 0;
        }

        return GetMaxQuantity(articleType, articleQuantity);
    }

    private static int GetMaxQuantity(ArticleType articleType, int articleQuantity)
    {
        return articleType switch
        {
            ArticleType.StopSolutionBottle => Constants.MaxQuantityStopSolution,
            ArticleType.WashAdditiveBottle => Constants.MaxQuantityWashAdditive,
            ArticleType.WashConcentrateBottle => articleQuantity > Constants.BottleCutoffQuantityForSize ? Constants.MaxQuantityWashConcentrateA : Constants.MaxQuantityWashConcentrateB,
            _ => throw new ArgumentOutOfRangeException(nameof(articleType), articleType, null)
        };
    }

    private static int GetFirstBottleIndex(ArticleType articleType, int? quantity)
    {
        return articleType == ArticleType.WashConcentrateBottle && quantity <= Constants.BottleCutoffQuantityForSize ? 3 : 1;
    }

    private static bool CheckForPair(ArticleType articleType)
    {
        return articleType is ArticleType.WashConcentrateBottle or ArticleType.WashAdditiveBottle;
    }

    private static bool IsValidBottleSizeScanned(ArticleType articleType, ReagentInfo scannedBottleInfo)
    {
        switch (articleType)
        {
            case ArticleType.WashConcentrateBottle:
                return true; // All sizes of Wash Concentrate bottles are valid for loading
            case ArticleType.WashAdditiveBottle:
                return scannedBottleInfo.Article.Quantity <= Constants.BottleCutoffQuantityForSize;
            case ArticleType.StopSolutionBottle:
                return scannedBottleInfo.Article.Quantity > Constants.BottleCutoffQuantityForSize;
            default:
                return false;
            }
        }
}