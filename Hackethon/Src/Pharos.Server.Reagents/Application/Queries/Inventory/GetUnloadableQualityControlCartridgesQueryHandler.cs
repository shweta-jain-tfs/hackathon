using MediatR;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Reagents;
using Pharos.Server.Reagents.Application.Interfaces;
using Tetris.Protocol.Pharos.Storage;

namespace Pharos.Server.Reagents.Application.Queries.Inventory;

internal class GetUnloadableQualityControlCartridgesQueryHandler(
    IInstrumentManager instrumentManager,
    IReagentBarcodeService reagentBarcodeService,
    IInstrumentReagentStorage reagentStorage)
    : IRequestHandler<GetUnloadableQualityControlCartridgesQuery, List<UnloadableQualityControlCartridgeDto>>
{
    public async Task<List<UnloadableQualityControlCartridgeDto>> Handle(GetUnloadableQualityControlCartridgesQuery request, CancellationToken cancellationToken)
    {
        var result = new List<UnloadableQualityControlCartridgeDto>();

        foreach (var instrument in instrumentManager.GetInstruments())
        {
            var cartridges = reagentStorage.GetCartridges(instrument.SerialNumber)
                .Where(x => x.Position.Area is EnumCartridgeArea.RearStorageArea or EnumCartridgeArea.FrontStorageArea && !x.UnloadPending);

            foreach (var cartridge in cartridges)
            {
                var (articleType, _) = await reagentBarcodeService.DetectArticleType(cartridge.ReagentBarcode);
                if (articleType != ArticleType.QualityControlCartridge)
                {
                    continue;
                }

                var reagentInfo = await reagentBarcodeService.GetReagentInfo(articleType, cartridge.ReagentBarcode);
                if (reagentInfo?.Article.Identity != request.QualityControlName)
                {
                    continue;
                }

                var cellExpiryDate = cartridge.Cells
                    .Where(cell => GetCartridgesQueryHandler.IsDateEmpty(cell.Content.OpenTimestamp))
                    .Min(cell => cell.Content.ExpiryDate?.ToDateTimeOffset());
                var expiryDate = cellExpiryDate.HasValue && cellExpiryDate.Value < reagentInfo.Lot.ExpiryDate
                    ? cellExpiryDate.Value
                    : reagentInfo.Lot.ExpiryDate;
                var openCell = GetCartridgesQueryHandler.GetExpiryDateOpenCell(cartridge, reagentInfo);
                result.Add(new UnloadableQualityControlCartridgeDto
                {
                    Id = Guid.NewGuid(),
                    Doses = cartridge.Cells.Sum(cell => cell.Content.RemainingQuantity),
                    Lot = reagentInfo.Lot.LotNumber,
                    Barcode = cartridge.ReagentBarcode,
                    Status = GetCartridgesQueryHandler.GetCartridgeStatus(cartridge),
                    ExpiryDate = expiryDate,
                    ExpiryDateOpenCell = openCell.ExpiryDate.HasValue ? new DateTimeOffset(openCell.ExpiryDate.Value) : null,
                    InstrumentName = instrument.DisplayName ?? string.Empty,
                    InstrumentNumber = instrument.SerialNumber,
                    Position = cartridge.Position.PositionInternalStorage,
                    Area = GetCartridgesQueryHandler.MapStorageAreaEnum(cartridge.Position.Area)
                });
            }
        }

        return result;
    }
}
