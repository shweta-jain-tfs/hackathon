using Serilog.Core;
using Serilog.Events;

namespace Pharos.Server.Infrastructure.Logging;

internal class CleanupLogEventEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        logEvent.RemovePropertyIfPresent("RequestId");
        logEvent.RemovePropertyIfPresent("ConnectionId");

        // Remove redundant RequestPath properties from Pharos.Server log records
        // RequestPath is logged by Serilog.AspNetCore.RequestLoggingMiddleware
        if (logEvent.Properties.TryGetValue("SourceContext", out var property))
        {
            if (property is ScalarValue scalarValue)
            {
                var value = scalarValue.Value as string;

                const string prefix = "Pharos.Server.";

                if (value?.StartsWith(prefix) ?? false)
                {
                    logEvent.RemovePropertyIfPresent("RequestPath");
                }
            }
        }
    }
}
