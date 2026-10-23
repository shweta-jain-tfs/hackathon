using MediatR;
using Microsoft.Extensions.Logging;
using Pharos.Server.Application;

namespace Pharos.Server.Infrastructure.Logging;

public class LoggingPipelineBehavior<TRequest, TResponse>(
    ICurrentUserService currentUser,
    ICurrentInstrumentService currentInstrument,
    ILogger<MediatRLogger> logger) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IBaseRequest
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        // Determine the module name based on the request type.
        const string moduleInterfaceNamespace = "Pharos.Server.Application.";
        var requestFullName = typeof(TRequest).FullName ?? "";
        string? module;
        if (requestFullName.StartsWith(moduleInterfaceNamespace))
        {
            // the request is a part of inter-module interface
            module = requestFullName[moduleInterfaceNamespace.Length..];
            module = module.IndexOf('.') switch
            {
                -1 => module, // No namespace, just the interface name.
                var index => module[..index] // Extract the module name before the first dot.
            };
        }
        else
        {
            // the request is internal to a module
            module = typeof(TRequest).Assembly.GetName().Name?.Replace("Pharos.Server.", string.Empty);
        }

        IDisposable? loggerScope = null;
        var userId = currentUser.UserId;
        var instrumentNumber = currentInstrument.InstrumentNumber;

        if (userId != null || instrumentNumber != null)
        {
            var scopeProperties = new Dictionary<string, object>();
            if (userId != null)
            {
                scopeProperties["UserId"] = userId.Value.ToString();
            }
            if (instrumentNumber != null)
            {
                scopeProperties["Instrument"] = instrumentNumber;
            }
            loggerScope = logger.BeginScope(scopeProperties);
        }

        try
        {
            using (logger.BeginScope(new Dictionary<string, object>
            {
                ["@Command"] = request // We leave the Request property for gRPC calls.
            }))
            {
                logger.LogDebug("Started {RequestName} MediatR request in {Module} module.", requestName, module);
            }

            var response = await next(cancellationToken);
            logger.LogDebug("Ended {RequestName} MediatR request in {Module} module.", requestName, module);
            return response;
        }
        finally
        {
            loggerScope?.Dispose();
        }
    }
}

public class MediatRLogger
{
}