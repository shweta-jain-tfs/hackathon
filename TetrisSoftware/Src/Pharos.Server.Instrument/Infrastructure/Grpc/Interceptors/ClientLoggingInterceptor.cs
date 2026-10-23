using Google.Protobuf;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Pharos.Server.Instrument.Application.Interfaces;
using Pharos.Server.Instrument.Infrastructure.Grpc.Interceptors;
using Tetris.Protocol.Instrument;

// Make namespace shorter for the sake of the log file size
// ReSharper disable CheckNamespace
#pragma warning disable IDE0130
namespace Pharos.Server.Grpc;

internal class ClientLoggingInterceptor(string instrumentNumber, IConfiguration configuration, ILogger<ClientLoggingInterceptor> logger) : Interceptor
{
    private static readonly string _methodNamePrefix = $"/{typeof(InstrumentService).Namespace}.";

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        var sessionId = context.Options.Headers?.Get(CommunicationSession.SessionIdHeader)?.Value ?? "";
        var method = context.Method.FullName.Replace(_methodNamePrefix, "");

        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["Instrument"] = instrumentNumber,
            ["InstrSession"] = sessionId
        }))
        {
            try
            {
                var section = configuration.GetSection("Logging:Grpc");
                var binaryGrpcMessages = section.GetValue<bool>("BinaryMessages");

                using (logger.BeginScope(new Dictionary<string, object>
                {
                    ["@Request"] = binaryGrpcMessages && request is IMessage protoRequest ? new BinaryGrpcMessage(protoRequest) : request
                }))
                {
                    logger.LogInformation("Started {Method} gRPC call.", method);
                }

                var asyncUnaryCall = continuation(request, context);

                var response = asyncUnaryCall.ResponseAsync.Result;

                using (logger.BeginScope(new Dictionary<string, object>
                {
                    ["@Response"] = binaryGrpcMessages && response is IMessage protoResponse ? new BinaryGrpcMessage(protoResponse) : response
                }))
                {
                    logger.LogInformation("Ended {Method} gRPC call.", method);
                }

                return asyncUnaryCall;
            }
            catch (RpcException e)
            {
                // we only log remote exceptions here
                logger.LogError(e, "Error occured during {Method} gRPC call.", method);
                throw;
            }
        }
    }
}