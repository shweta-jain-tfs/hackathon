using Serilog.Core;
using Serilog.Events;

namespace Pharos.Server.Infrastructure.Logging;

internal sealed class SensitivePropertyRedactionEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        foreach (var propertyName in logEvent.Properties
                     .Where(static property => property.Key.IsSensitive() && !IsEmpty(property.Value))
                     .Select(static property => property.Key)
                     .ToArray())
        {
            logEvent.AddOrUpdateProperty(new LogEventProperty(propertyName, new ScalarValue(PropertyRedactionExtensions.RedactedValue)));
        }
    }

    private static bool IsEmpty(LogEventPropertyValue propertyValue)
    {
        return propertyValue is ScalarValue { Value: null }
               || (propertyValue is ScalarValue { Value: string value } && string.IsNullOrWhiteSpace(value));
    }
}
