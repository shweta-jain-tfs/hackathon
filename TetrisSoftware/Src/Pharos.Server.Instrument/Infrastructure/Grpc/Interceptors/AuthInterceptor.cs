using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Instrument.Infrastructure.Kestrel;

namespace Pharos.Server.Instrument.Infrastructure.Grpc.Interceptors;

internal class AuthInterceptor(
    IInstrumentManager instrumentManager,
    ClientCertificateCache certificateCache,
    IOptions<GrpcSettings> grpcSettings) : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var instrumentNumber = context.GetInstrumentSerial();

        if (grpcSettings.Value.MainService.RequireClientCertificate)
        {
            var clientCertificate = context.GetHttpContext().Connection.ClientCertificate;

            if (clientCertificate == null || string.IsNullOrWhiteSpace(instrumentNumber)
                || !certificateCache.TryGetInstrumentNumber(clientCertificate.Thumbprint, out var cachedInstrumentNumber)
                || !instrumentNumber.Equals(cachedInstrumentNumber, StringComparison.OrdinalIgnoreCase))
            {
                throw new RpcException(new Status(StatusCode.Unauthenticated, $"The instrument serial number in the request header does not match the client certificate root."));
            }
        }

        var instrument = instrumentManager.FindInstrument(instrumentNumber);

        if (instrument is { Enabled: false })
        // existing instrument, check if it is disabled
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, "The call is not allowed because the instrument is disabled."));
        }

        return await continuation(request, context);
    }
}
