using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pharos.Contracts.Notifications;
using Pharos.Server.Application.Settings;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Infrastructure;
using Pharos.Server.Infrastructure.Database;
using Pharos.Server.Infrastructure.Services;
using Pharos.Server.Lis.Application.Interfaces;
using Pharos.Server.Lis.Application.Services;
using Pharos.Server.Lis.Infrastructure.Database;
using Pharos.Server.Lis.Infrastructure.Hubs;

namespace Pharos.Server.Lis;

public class LisModule : IServerAppModule
{
    private LisModule()
    {
    }

    public static IServerAppModule Instance { get; } = new LisModule();

    public Assembly Assembly => Assembly.GetExecutingAssembly();

    public void AddServices(IServiceCollection services, IConfiguration configuration, object? mvcBuilder)
    {
        services.ConfigureSettings<LisSettings>(configuration);

        services.AddDbContext<LisDbContext>();
        services.AddScoped<ILisDbContext>(provider => provider.GetRequiredService<LisDbContext>());
        services.AddScoped<LisDbContextInitializer>();

        services.AddSingleton<ILisTransportConnectionFactory, LisTransportConnectionFactory>();
        services.AddSingleton<ILisMessageBuilder, LisMessageBuilder>();
        services.AddSingleton<ILisConnectionFactory, LisConnectionFactory>();
        services.AddSingleton<ILisRequestPolling, LisRequestPolling>();

        services.AddScoped<ILisMessageQueue, LisMessageQueue>();

        services.AddHostedServiceAsSingleton<ILisConnectionPool, LisConnectionPool>();
        services.AddHostedServiceAsSingleton<ILisMessageDispatcher, LisMessageDispatcher>();

        // register web api controllers from this assembly
        (mvcBuilder as IMvcBuilder)?.AddApplicationPart(Assembly);

        services.AddScoped<ILisConnectionHub, LisConnectionHub>();
    }

    public void UseModule(IHost webApplication, object endpointConventionBuilder)
    {
        var app = (WebApplication)webApplication;
        app.MapHub<LisConnectionHub>(HubRoutes.Lis);
    }

    public IDbContextInitializer GetDbContextInitializer(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<LisDbContextInitializer>();
    }
}
