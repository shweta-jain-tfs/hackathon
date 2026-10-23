using Microsoft.EntityFrameworkCore;
using Pharos.Server.Domain;
using Pharos.Server.LoadList.Domain;

namespace Pharos.Server.LoadList.Application.Queries;

internal record GetTotalRefillLevelQuery(LoadListItemType LoadListItemType, string SelectedInstrument) : IRequest<RefillLevelInfoDto>;

internal class GetTotalRefillLevelQueryHandler(ILoadListDbContext dbContext) : IRequestHandler<GetTotalRefillLevelQuery, RefillLevelInfoDto>
{
    public async Task<RefillLevelInfoDto> Handle(GetTotalRefillLevelQuery request, CancellationToken cancellationToken)
    {
        if (request.LoadListItemType == LoadListItemType.Cartridge)
        {
            var count = await dbContext.LoadListConfigurations
                .Where(x =>
                    (x.ArticleType == ArticleType.CalibratorCartridge ||
                     x.ArticleType == ArticleType.CurveControlCartridge ||
                     x.ArticleType == ArticleType.ConjugateCartridge ||
                     x.ArticleType == ArticleType.DevelopmentSolutionCartridge ||
                     x.ArticleType == ArticleType.DiluentCartridge ||
                     x.ArticleType == ArticleType.QualityControlCartridge) &&
                    x.InstrumentSerial == request.SelectedInstrument)
                .SumAsync(x => x.Quantity == 0
                    ? 0
                    : (int)Math.Ceiling((double)x.RefillLevel / x.Quantity), cancellationToken);

            return new RefillLevelInfoDto
            {
                TotalRefillLevel = count,
                InternalStorageCapacity = Constants.MaxCartridgeStoragePositionsFront + Constants.MaxCartridgeStoragePositionsRear
            };
        }

        var countCarriers = await dbContext.LoadListConfigurations
            .Where(x =>
                (x.ArticleType == ArticleType.ImmunoCapCarrier ||
                x.ArticleType == ArticleType.EliaWellCarrier ||
                x.ArticleType == ArticleType.EliaCalibratorWellCarrier ||
                x.ArticleType == ArticleType.DilutionWellCarrier ) &&
                x.InstrumentSerial == request.SelectedInstrument)
            .SumAsync(x => x.Quantity == 0
                ? 0
                : (int)Math.Ceiling((double)x.RefillLevel / x.Quantity), cancellationToken);

        return new RefillLevelInfoDto
        {
            TotalRefillLevel = countCarriers,
            InternalStorageCapacity = Constants.MaxCarrierStoragePositions
        };
    }
}


public class RefillLevelInfoDto
{
    public int TotalRefillLevel { get; set; }
    public int InternalStorageCapacity { get; set; }
    public int TotalRefillLevelPercentage => (int) Math.Round((double) TotalRefillLevel / InternalStorageCapacity * 100);

}