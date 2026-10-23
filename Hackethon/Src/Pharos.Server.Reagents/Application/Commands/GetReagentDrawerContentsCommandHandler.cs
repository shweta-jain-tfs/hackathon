using MediatR;
using Pharos.Server.Application.Reagents;
using Pharos.Server.Reagents.Application.Mapping;
using Tetris.Protocol.Common;
using Tetris.Protocol.Pharos;

namespace Pharos.Server.Reagents.Application.Commands;

internal class GetReagentDrawerContentsCommandHandler : IRequestHandler<GetReagentDrawerContentsCommand, ReagentDrawerContentsResponse>
{
    private readonly IMediator _mediator;

    public GetReagentDrawerContentsCommandHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<ReagentDrawerContentsResponse> Handle(GetReagentDrawerContentsCommand request, CancellationToken cancellationToken)
    {
        var cartridgesBarcodeList = request.Message.Cartridges.Select(r => r.ReagentBarcode).ToList();
        var cartridges = await _mediator.Send(new GetCartridgeContentsQuery(cartridgesBarcodeList), cancellationToken);

        var carrierOcrList = request.Message.Carriers.Select(r => r.CarrierOcr).ToList();
        var carriersContent = await _mediator.Send(new GetCarrierContentsByOcrQuery(carrierOcrList), cancellationToken);

        var result = new ReagentDrawerContentsResponse();

        foreach (var scannedCartridge in request.Message.Cartridges)
        {
            var detectedItem = new DetectedCartridgeInfo
            {
                Position = scannedCartridge.Position,
                UnknownCartridge = true,
                ReagentType = EnumReagentType.UnspecifiedReagentType,
            };

            if (!string.IsNullOrEmpty(scannedCartridge.ReagentBarcode) && cartridges.TryGetValue(scannedCartridge.ReagentBarcode, out var item))
            {
                detectedItem.Content = item.ToReagentCarrierContent();
                detectedItem.UnknownCartridge = false;
                detectedItem.ReagentType = item.ArticleType.ToEnumReagentType();
            }

            result.Cartridges.Add(detectedItem);
        }

        foreach (var scannedCarrier in request.Message.Carriers)
        {
            var detectedItem = new DetectedCarrierInfo
            {
                Position = scannedCarrier.Position,
                UnknownCarrier = true,
                CarrierType = EnumReagentType.UnspecifiedReagentType,
                TestName = string.Empty,
                QuantityFull = 0

            };

            if (!string.IsNullOrEmpty(scannedCarrier.CarrierOcr) && !scannedCarrier.CannotReadOcr)
            {
                if (carriersContent.TryGetValue(scannedCarrier.CarrierOcr, out var carrierDto) && carrierDto != null)
                {
                    var types = new List<ArticleType> { carrierDto.ArticleType };
                    var quantity = carrierDto.ContentCells.Min(c => c.RemainingQuantity);
                    // If all matching carriers have the same type
                    if (types.Count == 1 && types[0] != ArticleType.None)
                    {
                        detectedItem.CarrierType = types[0].ToEnumReagentType();
                        detectedItem.TestName = scannedCarrier.CarrierOcr;
                        detectedItem.UnknownCarrier = false;
                        detectedItem.QuantityFull = quantity;
                    }
                }
            }

            result.Carriers.Add(detectedItem);
        }

        return result;
    }
}
