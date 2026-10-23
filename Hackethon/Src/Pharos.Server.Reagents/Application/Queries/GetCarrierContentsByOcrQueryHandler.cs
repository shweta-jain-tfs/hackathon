using MediatR;
using Pharos.Server.Application.Reagents;

namespace Pharos.Server.Reagents.Application.Queries;

internal class GetCarrierContentsByOcrQueryHandler(IReagentBarcodeService reagentBarcodeService) : IRequestHandler<GetCarrierContentsByOcrQuery, IReadOnlyDictionary<string, ReagentContentDto>>
{
    public async Task<IReadOnlyDictionary<string, ReagentContentDto>> Handle(GetCarrierContentsByOcrQuery request, CancellationToken cancellationToken)
    {
        var ocrList = request.OcrList;
        var result = new Dictionary<string, ReagentContentDto>();
        var reagentsInfoDict = await reagentBarcodeService.GetCarrierContentsFromOcr(ocrList);

        foreach (var ocr in ocrList)
        {
            if (!reagentsInfoDict.TryGetValue(ocr, out var reagentInfo) || reagentInfo == null || reagentInfo.Article == null)
            {
                result[ocr] = new ReagentContentDto
                {
                    Name = ocr,
                    ArticleType = ArticleType.None,
                    ArticleNumber = string.Empty,
                    ContentCells = new List<ReagentContentCellDto>
                    {
                        new() {
                            RemainingQuantity = 0,
                            UseRemainingQuantity = false
                        }
                    }
                };
                continue;
            }

            var article = reagentInfo.Article;
            var articleType = article.ArticleType;
            var quantity = article.Quantity;
            if (articleType == ArticleType.None || quantity == 0)
            {
                result[ocr] = new ReagentContentDto
                {
                    Name = ocr,
                    ArticleType = ArticleType.None,
                    ArticleNumber = string.Empty,
                    ContentCells = new List<ReagentContentCellDto>
                    {
                        new() {
                            RemainingQuantity = 0,
                            UseRemainingQuantity = false
                        }
                    }
                };
                continue;
            }

            var dto = new ReagentContentDto
            {
                Name = article.Identity,
                ArticleType = articleType,
                ArticleNumber = article.ArticleNumber,
                ContentCells = new List<ReagentContentCellDto>()
            };

            if (article.NumberOfCells == null)
            {
                dto.ContentCells.Add(new ReagentContentCellDto
                {
                    RemainingQuantity = quantity,
                    UseRemainingQuantity = false
                });
            }
            else
            {
                for (var j = 1; j <= article.NumberOfCells; j++)
                {
                    dto.ContentCells.Add(new ReagentContentCellDto
                    {
                        Position = j,
                        RemainingQuantity = quantity,
                        UseRemainingQuantity = false
                    });
                }
            }
            result[ocr] = dto;
        }
        return result;
    }
}
