using Grpc.Core;
using Grpc.Core.Interceptors;
using Pharos.Server.Instrument.Application.Interfaces;

namespace Pharos.Server.Instrument.Infrastructure.Grpc.Interceptors;

internal class CheckPairingModeInterceptor(IInstrumentPairingService pairingService) : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
    {
        if (!pairingService.PairingEnabled)
        {
            throw new RpcException(new Status(StatusCode.Unavailable, "The instrument pairing mode is disabled."));
        }

        return await continuation(request, context);
    }
}
