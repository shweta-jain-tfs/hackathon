using Google.Protobuf;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Pharos.Server.Instrument.Application.Interfaces;
using Pharos.Server.Instrument.Infrastructure.Grpc.Interceptors;
using Tetris.Protocol.Pharos;

// Make namespace shorter for the sake of the log file size
// ReSharper disable CheckNamespace
#pragma warning disable IDE0130
namespace Pharos.Server.Grpc;

internal class ServerLoggingInterceptor(IConfiguration configuration, ILogger<ServerLoggingInterceptor> logger) : Interceptor
{
    private static readonly string _methodNamePrefix = $"/{typeof(PharosService).Namespace}.";

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var sessionId = context.RequestHeaders.Get(CommunicationSession.SessionIdHeader)?.Value;
        var instrumentNumber = context.RequestHeaders.Get(CommunicationSession.InstrumentSerialHeader)?.Value;
        var method = context.Method.Replace(_methodNamePrefix, "");

        IDisposable? loggerScope = null;
        if (sessionId != null || instrumentNumber != null)
        {
            var scopeProperties = new Dictionary<string, object>();
            if (sessionId != null)
            {
                scopeProperties["InstrSession"] = sessionId;
            }
            if (instrumentNumber != null)
            {
                scopeProperties["Instrument"] = instrumentNumber;
            }
            loggerScope = logger.BeginScope(scopeProperties);
        }
        try
        {
            var section = configuration.GetSection("Logging:Grpc");
            var binaryGrpcMessages = section.GetValue<bool>("BinaryMessages");

            using (logger.BeginScope(new Dictionary<string, object>
            {
                ["@Request"] = binaryGrpcMessages && request is IMessage protoRequest ? new BinaryGrpcMessage(protoRequest) : request
            }))
            {
                logger.LogInformation("Started {Method} gRPC handler.", method);
            }

            var response = await continuation(request, context).ConfigureAwait(false);

            using (logger.BeginScope(new Dictionary<string, object>
            {
                ["@Response"] = binaryGrpcMessages && response is IMessage protoResponse ? new BinaryGrpcMessage(protoResponse) : response
            }))
            {
                logger.LogInformation("Ended {Method} gRPC handler.", method);
            }

            return response;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error occured in {Method} gRPC handler.", method);
            throw;
        }
        finally
        {
            loggerScope?.Dispose();
        }
    }
}