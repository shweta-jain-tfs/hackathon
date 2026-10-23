using MediatR;
using Pharos.Server.Application.Reagents;
using Pharos.Server.Reagents.Application.Interfaces;

namespace Pharos.Server.Reagents.Application.Queries.Inventory
{
    internal class GetCarrierContentQueryHandler(IReagentsDbContext dbContext, IReagentBarcodeService reagentBarcodeService)
        : IRequestHandler<GetCarrierContentQuery, ReagentContentDto?>
    {
        public async Task<ReagentContentDto?> Handle(GetCarrierContentQuery request, CancellationToken cancellationToken)
        {
            ReagentContentDto? result = null;

            var reagentInfo = await GetReagentInfo(request).ConfigureAwait(false);

            if (reagentInfo != null)
            {
                var article = reagentInfo.Article;

                var dbReagent = await dbContext.Reagents
                    .Include(r => r.ReagentContents)
                    .Where(r => r.Barcode == request.ReagentBarcode && r.Udi == request.CarrierUdi)
                    .FirstOrDefaultAsync(cancellationToken);

                var exists = dbReagent != null;
                var remainingQuantity = article.Quantity;
                if (exists && dbReagent?.ReagentContents is not null and { Count: > 0 })
                {
                    remainingQuantity = dbReagent.ReagentContents[0].Quantity;
                }

                result = new()
                {
                    ReagentBarcode = request.ReagentBarcode,
                    Name = article.Identity,
                    ArticleType = article.ArticleType,
                    ArticleNumber = article.ArticleNumber,
                    LotNumber = reagentInfo.Lot.LotNumber,
                    LotExpiryDate = reagentInfo.Lot.ExpiryDate,
                    SerialNumber = reagentInfo.SerialNumber,
                    ContentCells = new List<ReagentContentCellDto>()
                };

                result.ContentCells.Add(new ReagentContentCellDto
                {
                    RemainingQuantity = remainingQuantity,
                    UseRemainingQuantity = exists
                });
            }

            return result;
        }

        private async Task<ReagentInfo?> GetReagentInfo(GetCarrierContentQuery request)
        {
            var barcode = !string.IsNullOrWhiteSpace(request.ReagentBarcode) ? request.ReagentBarcode : request.CarrierUdi ?? string.Empty;
            var (articleType, _) = await reagentBarcodeService.DetectArticleType(barcode).ConfigureAwait(false);
            var reagentInfo = await reagentBarcodeService.GetReagentInfo(articleType, request.ReagentBarcode, request.CarrierUdi);
            return reagentInfo;
        }
    }
}
