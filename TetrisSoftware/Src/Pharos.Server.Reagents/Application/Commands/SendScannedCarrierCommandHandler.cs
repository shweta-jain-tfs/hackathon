using MediatR;
using Pharos.Server.Application.Reagents;
using Pharos.Server.Reagents.Application.Mapping;
using Tetris.Protocol.Common;
using Tetris.Protocol.Pharos;

namespace Pharos.Server.Reagents.Application.Commands;

internal class SendScannedCarrierCommandHandler(IMediator mediator) : IRequestHandler<SendScannedCarrierCommand, ScannedCarrierResponse>
{
    public async Task<ScannedCarrierResponse> Handle(SendScannedCarrierCommand request, CancellationToken cancellationToken)
    {
        var scannedCarrier = request.Message;

        var scannedCarrierDto = await mediator.Send(new GetCarrierContentQuery(scannedCarrier.ReagentBarcode, scannedCarrier.ReagentUdiBarcode), cancellationToken);

        var result = new ScannedCarrierResponse
        {
            Content = scannedCarrierDto != null
                ? scannedCarrierDto.ToReagentCarrierContent()
                : new ReagentCarrierContent()
        };

        result.Content.ReagentUdiBarcode = scannedCarrier.ReagentUdiBarcode;
        result.UnknownCarrier = scannedCarrierDto == null;

        return result;
    }
}
