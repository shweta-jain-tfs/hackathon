using Grpc.Core;
using Pharos.Server.Instrument.Application.Interfaces;
using Pharos.Server.Instrument.Infrastructure.Grpc;
using Tetris.Protocol.Pharos.Pairing;

namespace Pharos.Server.Instrument.Application.Services.Pairing;

internal sealed class PairingServiceImplementation(IInstrumentPairingService pairingService) : PairingService.PairingServiceBase
{
    public override Task<StartPairingResponse> StartPairing(StartPairingRequest request, ServerCallContext context)
    {
        return pairingService.StartPairing(request);
    }

    public override Task<PairingStateResponse> GetPairingState(PairingStateRequest request, ServerCallContext context)
    {
        return Task.FromResult(new PairingStateResponse { State = pairingService.GetPairingState(request.PairingToken) });
    }

    public override async Task<ConfirmPairingResponse> ConfirmPairing(ConfirmPairingRequest request, ServerCallContext context)
    {
        var pairingState = pairingService.GetPairingState(request.PairingToken);

        if (pairingState != EnumPairingState.WaitingForSignature)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, $"Pairing operation is not in a valid state: {pairingState}"));
        }

        pairingState = await pairingService.ConfirmPairing(request, context.GetInstrumentIp());
        return new ConfirmPairingResponse { State = pairingState };
    }
}
