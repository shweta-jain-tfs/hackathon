using MediatR;
using Pharos.Server.Application.Reagents;
using Pharos.Server.Reagents.Application.Interfaces;

namespace Pharos.Server.Reagents.Application.Queries.Inventory;

internal class GetCartridgeContentQueryHandler(
    IReagentsDbContext dbContext,
    IReagentBarcodeService reagentBarcodeService) : IRequestHandler<GetCartridgeContentsQuery, IReadOnlyDictionary<string, ReagentContentDto>>
{
    public async Task<IReadOnlyDictionary<string, ReagentContentDto>> Handle(GetCartridgeContentsQuery request, CancellationToken cancellationToken)
    {
        var barcodeList = request.CartridgeBarcodeList;
        var result = await dbContext.Reagents
            .Where(r => barcodeList.Contains(r.Barcode))
            .Select(r => new ReagentContentDto
            {
                ReagentBarcode = r.Barcode,
                LotNumber = r.Lot.LotNumber,
                LotExpiryDate = r.Lot.ExpiryDate,
                ContentCells = r.ReagentContents!.Select(rc => new ReagentContentCellDto
                {
                    Position = rc.CellPosition,
                    RemainingQuantity = rc.Quantity,
                    UseRemainingQuantity = true, // TODO: implement when decision logic is in place (use quantity or measure)
                    OpenTimestamp = rc.OpenTimestamp,
                }).ToList()
            })
            .ToDictionaryAsync(r => r.ReagentBarcode, cancellationToken)
            .ConfigureAwait(false);

        //TODO: Need to check if this causes performance issue when barcodeList is large
        var reagentsInfoList = await GetReagentInfoList(barcodeList).ConfigureAwait(false);

        for (var i = 0; i < reagentsInfoList.Count; i++)
        {
            var barcode = barcodeList[i];
            var reagentInfo = reagentsInfoList[i];
            if (reagentInfo == null)
            {
                continue;
            }
            var article = reagentInfo.Article;

            if (result.TryGetValue(barcode, out var reagent))
            {
                reagent.ArticleType = article.ArticleType;
                reagent.ArticleNumber = article.ArticleNumber;
                continue;
            }
            var dto = new ReagentContentDto
            {
                ReagentBarcode = barcode,
                ArticleType = article.ArticleType,
                ArticleNumber = article.ArticleNumber,
                LotNumber = reagentInfo.Lot.LotNumber,
                LotExpiryDate = reagentInfo.Lot.ExpiryDate,
                ContentCells = new List<ReagentContentCellDto>()
            };
            result[barcode] = dto;

            if (article.NumberOfCells == null)
            {
                dto.ContentCells.Add(new ReagentContentCellDto
                {
                    RemainingQuantity = article.Quantity,
                    UseRemainingQuantity = true
                });
            }
            else
            {
                for (var j = 1; j <= article.NumberOfCells; j++)
                {
                    dto.ContentCells.Add(new ReagentContentCellDto
                    {
                        Position = j,
                        RemainingQuantity = article.Quantity,
                        UseRemainingQuantity = true
                    });
                }
            }
        }

        return result;
    }

    private async Task<List<ReagentInfo?>> GetReagentInfoList(IReadOnlyList<string> barcodeList)
    {
        var reagentsInfoList = new List<ReagentInfo?>();

        foreach (var barcode in barcodeList)
        {
            var (articleType, _) = await reagentBarcodeService.DetectArticleType(barcode).ConfigureAwait(false);
            var reagentInfo = await reagentBarcodeService.GetReagentInfo(articleType, barcode).ConfigureAwait(false);
            if (reagentInfo != null)
            {
                reagentsInfoList.Add(reagentInfo);
            }
        }

        return reagentsInfoList;
    }
}