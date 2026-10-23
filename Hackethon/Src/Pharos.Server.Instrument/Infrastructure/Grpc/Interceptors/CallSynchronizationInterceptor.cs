using Grpc.Core;
using Grpc.Core.Interceptors;
using Pharos.Server.Instrument.Application.Interfaces;

namespace Pharos.Server.Instrument.Infrastructure.Grpc.Interceptors;

internal class CallSynchronizationInterceptor(ICommunicationSessionManager sessionManager, IRemoteCallSynchronizer remoteCallSynchronizer) : Interceptor
{
    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        string? instrumentNumber = null;
        var sessionId = context.Options.Headers?.Get(CommunicationSession.SessionIdHeader)?.Value;
        if (sessionId != null)
        {
            var session = sessionManager.GetSession(Guid.Parse(sessionId));
            if (session != null)
            {
                instrumentNumber = session.InstrumentNumber;
            }
        }
        if (instrumentNumber == null)
        {
            throw new InvalidOperationException("Failed to get the instrument number. Probably the communication session is not established.");
        }

        var acquired = remoteCallSynchronizer.WaitForExecutionContext(instrumentNumber, TimeSpan.FromSeconds(30));
        if (!acquired)
        {
            throw new RpcException(new Status(StatusCode.DeadlineExceeded, "Timeout waiting for execution context. Check that you are not making a gRPC call from within a gRPC handler (it is prohibited)."));
        }

        var innerCall = continuation(request, context);

        // Wrap ResponseAsync so that we release when the response completes (or faults)
        async Task<TResponse> WrappedResponseAsync()
        {
            try
            {
                return await innerCall.ResponseAsync.ConfigureAwait(false);
            }
            finally
            {
                remoteCallSynchronizer.ReleaseExecutionContext(instrumentNumber);
            }
        }

        // Forward the other members and only wrap ResponseAsync
        return new AsyncUnaryCall<TResponse>(
            WrappedResponseAsync(),
            innerCall.ResponseHeadersAsync,
            innerCall.GetStatus,
            innerCall.GetTrailers,
            innerCall.Dispose);
    }

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var instrumentNumber = context.GetInstrumentSerial();
        var acquired = await remoteCallSynchronizer.WaitForExecutionContextAsync(instrumentNumber, TimeSpan.FromSeconds(30));
        if (!acquired)
        {
            throw new RpcException(new Status(StatusCode.DeadlineExceeded, "Timeout waiting for execution context."));
        }

        try
        {
            return await continuation(request, context).ConfigureAwait(false);
        }
        finally
        {
            remoteCallSynchronizer.ReleaseExecutionContext(instrumentNumber);
        }
    }
}