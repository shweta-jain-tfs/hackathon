using Microsoft.AspNetCore.Builder;
using Serilog;

namespace Pharos.Server.Infrastructure.Logging;

public static class WebApplicationBuilderExtensions
{
    public static ConfigureHostBuilder ConfigureSerilog(this ConfigureHostBuilder host, Action<LoggerConfiguration>? configure = null)
    {
        host.UseSerilog((context, configuration) =>
        {
            var redactSensitiveData = context.Configuration.IsRedactionEnabled();

            var logConfig = configuration.ReadFrom.Configuration(context.Configuration)
                .Enrich.With<CleanupLogEventEnricher>();

            if (redactSensitiveData)
            {
                logConfig.Enrich.With(new SensitivePropertyRedactionEnricher());
            }

            logConfig.Destructure.With(new CustomDestructuringPolicy(redactSensitiveData));
            configure?.Invoke(configuration);
        });
        return host;
    }
}

