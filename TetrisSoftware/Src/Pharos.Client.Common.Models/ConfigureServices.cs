using Microsoft.Extensions.DependencyInjection;
using Pharos.Client.Common.Models.Notifications;

namespace Pharos.Client.Common.Models;

public static class ConfigureServices
{
    public static IServiceCollection AddNotificationProviders(this IServiceCollection services)
    {
        services.AddTransient<IHubConnectionFactory, HubConnectionFactory>();

        // We need this as a singleton because there are two scopes involved:
        // Blazor WASM components use the app root scope.
        // HttpClientFactory creates its own internal scope when building the handler pipeline.
        services.AddSingleton<IConnectionHealthService, ConnectionHealthService>();

        services.AddScoped<ICurrentInstrumentNotificationProvider, CurrentInstrumentNotificationProvider>();
        services.AddScoped<IJobNotificationProvider, JobNotificationProvider>();
        services.AddScoped<ConnectivityHttpMessageHandler>();

        return services;
    }
}