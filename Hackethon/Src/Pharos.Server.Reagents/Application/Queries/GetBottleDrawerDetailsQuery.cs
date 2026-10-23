using MediatR;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Reagents;
using Pharos.Server.Domain;
using Pharos.Server.Localization;
using Pharos.Server.Reagents.Application.Interfaces;
using Tetris.Protocol.Common;
using Tetris.Protocol.Pharos.Storage;

namespace Pharos.Server.Reagents.Application.Queries;

public class GetBottleDrawerDetailsQuery : IRequest<BottleDrawerDetailsDto>
{
    public string InstrumentNumber { get; }
    public GetBottleDrawerDetailsQuery(string instrumentNumber)
    {
        InstrumentNumber = instrumentNumber;
    }
}

internal class GetBottleDrawerDetailsQueryHandler(IInstrumentReagentStorage instrumentReagentStorage,
        IReagentBarcodeService reagentBarcodeService,
        IInstrumentManager instrumentManager,
        IReagentsDbContext dbContext) : IRequestHandler<GetBottleDrawerDetailsQuery, BottleDrawerDetailsDto>
{
    public async Task<BottleDrawerDetailsDto> Handle(GetBottleDrawerDetailsQuery request, CancellationToken cancellationToken)
    {

        var bottles = instrumentReagentStorage.GetBottles(request.InstrumentNumber);
        var result = new BottleDrawerDetailsDto
        {
            Positions = FillEmptyBottlesDrawer(),
            LockState = instrumentManager.GetInstrument(request.InstrumentNumber).DrawersLockState[DrawerType.BottledReagent].LockState
        };

        await ProcessBottles(result.Positions, bottles, request.InstrumentNumber, result.LockState);

        return result;
    }

    private async Task ProcessBottles(List<BottlePositionDto> bottlePositionDto, IReadOnlyList<BottledReagent> bottledReagents, string instrumentNumber, LockState lockState)
    {
        foreach (var bottlePosition in bottlePositionDto)
        {
            foreach (var bottleDetailsDto in bottlePosition.Bottles)
            {
                var bottledReagent = bottledReagents.FirstOrDefault(x => EnumReagentTypeToArticleType(x.ReagentType) == bottlePosition.ArticleType && x.Position == bottleDetailsDto.Position);

                if (bottledReagent != null)
                {
                    var reagentInfo = await reagentBarcodeService.GetReagentInfo(EnumReagentTypeToArticleType(bottledReagent.ReagentType), null, bottledReagent.ReagentUdiBarcode);

                    if (reagentInfo != null)
                    {
                        (bottleDetailsDto.Quantity, bottleDetailsDto.UseRemainingQuantity) =
                            await GetQuantity(bottlePosition.ArticleType, bottleDetailsDto, bottledReagent, lockState);
                        bottleDetailsDto.UnitOfMeasure = reagentInfo.Article.UnitOfMeasure;
                        bottleDetailsDto.Name = reagentInfo.Article.FriendlyName;
                        bottleDetailsDto.LotNumber = reagentInfo.Lot.LotNumber;
                        bottleDetailsDto.ExpiryDate = GetExpiryDate(bottledReagent, reagentInfo);
                    }

                    bottleDetailsDto.ReagentUdi = bottledReagent.ReagentUdiBarcode;
                    bottleDetailsDto.InUse = bottledReagent.InUse;
                    bottleDetailsDto.IsPresent = true;
                    bottleDetailsDto.FillPercentage = GetFillPercentage(bottlePosition.ArticleType, bottleDetailsDto);
                    bottleDetailsDto.Status = GetStatus(bottledReagent, bottleDetailsDto.Quantity);
                }

                if (bottlePosition.ArticleType == ArticleType.WashConcentrateBottle && bottlePosition.Position == 4)
                {
                    bottleDetailsDto.Position -= 2; // Adjust position for WashConcentrateB for UI visibility
                }


                bottleDetailsDto.Location = bottledReagent != null ?
                    GetBottleLocation(instrumentNumber, bottlePosition.ArticleType, bottleDetailsDto.Position) : string.Empty;
            }
        }
    }

    private static int GetFillPercentage(ArticleType articleType, BottleDetailDto bottleDetailsDto)
    {
        var maxQuantity = GetMaxQuantity(articleType, bottleDetailsDto.Position);
        var percentage = (double)bottleDetailsDto.Quantity / maxQuantity * 100;
        return (int)percentage;
    }

    private static int GetMaxQuantity(ArticleType articleType, int position)
    {
        return articleType switch
        {
            ArticleType.StopSolutionBottle => Constants.MaxQuantityStopSolution,
            ArticleType.WashAdditiveBottle => Constants.MaxQuantityWashAdditive,
            ArticleType.WashConcentrateBottle => position <= 2 ? Constants.MaxQuantityWashConcentrateA : Constants.MaxQuantityWashConcentrateB,
            _ => throw new ArgumentOutOfRangeException(nameof(articleType), articleType, null)
        };
    }

    private static List<BottlePositionDto> FillEmptyBottlesDrawer()
    {
        var result = new List<BottlePositionDto>();

        result.Add(FillEmptyBottleDetails(ArticleType.StopSolutionBottle, 1));
        result.Add(FillEmptyBottleDetails(ArticleType.WashAdditiveBottle, 2));
        result.Add(FillEmptyBottleDetails(ArticleType.WashConcentrateBottle, 3));
        result.Add(FillEmptyBottleDetails(ArticleType.WashConcentrateBottle, 4));

        return result;
    }

    private static BottlePositionDto FillEmptyBottleDetails(ArticleType articleType, int position)
    {
        var bottlePosition = new BottlePositionDto
        {
            ArticleType = articleType,
            Position = position
        };

        var startIndex = position == 4 ? 3 : 1; // For WashConcentrateB, start from position 3, otherwise start from 1

        for (var i = startIndex; i < startIndex + 2; i++)
        {
            bottlePosition.Bottles.Add(new BottleDetailDto
            {
                Id = HashCode.Combine(articleType, i),
                Position = i,
                IsPresent = false
            });
        }
        return bottlePosition;
    }

    private static ArticleType EnumReagentTypeToArticleType(EnumReagentType reagentType)
    {
        return reagentType switch
        {
            EnumReagentType.StopSolution => ArticleType.StopSolutionBottle,
            EnumReagentType.WashAdditive => ArticleType.WashAdditiveBottle,
            EnumReagentType.WashConcentrate => ArticleType.WashConcentrateBottle,
            _ => throw new ArgumentOutOfRangeException(nameof(reagentType), reagentType, null)
        };
    }

    private string GetBottleLocation(string instrumentNumber, ArticleType articleType, int position)
    {
        var instrumentDisplayName = instrumentManager.FindInstrument(instrumentNumber)?.DisplayName;
        return $"{instrumentDisplayName}, {GetBottleType(articleType)} {position}";
    }

    private static string GetBottleType(ArticleType articleType)
    {
        return articleType switch
        {
            ArticleType.StopSolutionBottle => Translations.BottleLocation_StopSolution,
            ArticleType.WashAdditiveBottle => Translations.BottleLocation_WashAdditive,
            ArticleType.WashConcentrateBottle => Translations.BottleLocation_WashConcentrate,
            _ => string.Empty
        };
    }

    private static ReagentPositionStatus GetStatus(BottledReagent bottle, int quantity)
    {
        return bottle.Error switch
        {
            EnumBottledReagentError.VolumeReadError => ReagentPositionStatus.UndetectedVolume,
            EnumBottledReagentError.CannotPumpLiquid => ReagentPositionStatus.DisconnectedBottle,
            EnumBottledReagentError.WrongVolume => ReagentPositionStatus.UnexpectedVolume,
            EnumBottledReagentError.BottleUnknown => ReagentPositionStatus.UnknownBottle,
            EnumBottledReagentError.BottleExpired => ReagentPositionStatus.Expired,
            EnumBottledReagentError.UnspecifiedBottledReagentError => CheckRemainingStatus(bottle, quantity),
            _ => throw new NotImplementedException()
        };
    }

    private static ReagentPositionStatus CheckRemainingStatus(BottledReagent bottle, int quantity)
    {
        if (quantity == 0)
        {
            return ReagentPositionStatus.Empty;
        }

        if (bottle.InUse)
        {
            return ReagentPositionStatus.InUse;
        }

        if (bottle.Content.ReservedStartedQuantity > 0 || bottle.Content.ReservedScheduledQuantity > 0)
        {
            return ReagentPositionStatus.Reserved;
        }

        return ReagentPositionStatus.Available;
    }

    private async Task<(int, bool)> GetQuantity(ArticleType articleType, BottleDetailDto bottleDetail, BottledReagent bottledReagent, LockState lockState)
    {
        if (lockState is LockState.ClosedLocked or LockState.ClosedUnlocked)
        {
            return (bottledReagent.Content.RemainingQuantity, true);
        }
        else
        {
            return (await GetSavedOrMaxQuantity(bottledReagent.ReagentUdiBarcode, articleType, bottleDetail), false);
        }
    }

    private async Task<int> GetSavedOrMaxQuantity(string reagentUdi, ArticleType articleType, BottleDetailDto bottleDetail)
    {
        var dbReagent = await dbContext.Reagents
                .Include(r => r.ReagentContents)
                .FirstOrDefaultAsync(r => r.Udi == reagentUdi);

        if (dbReagent != null)
        {
            return dbReagent.ReagentContents?.FirstOrDefault()?.Quantity ?? 0;
        }

        return GetMaxQuantity(articleType, bottleDetail.Position);
    }

    private static DateTime? GetExpiryDate(BottledReagent bottle, ReagentInfo reagentInfo)
    {
        var bottleExpiryDate = bottle.Content.ExpiryDate.ToDateTime();
        var reagentInfoExpiryDate = reagentInfo.Lot.ExpiryDate.DateTime;
        return bottleExpiryDate < reagentInfoExpiryDate
            ? bottleExpiryDate
            : reagentInfoExpiryDate;
    }
}

