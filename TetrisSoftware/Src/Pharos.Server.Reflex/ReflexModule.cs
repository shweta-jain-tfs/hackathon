using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pharos.Contracts.Notifications;
using Pharos.Server.Infrastructure;
using Pharos.Server.Reflex.Infrastructure.Database;
using Pharos.Server.Reflex.Infrastructure.Hubs;
using Pharos.Server.Reflex.Interfaces;

namespace Pharos.Server.Reflex;

public class ReflexModule : IServerAppModule
{
    private ReflexModule()
    {
    }

    public static IServerAppModule Instance { get; } = new ReflexModule();

    public Assembly Assembly => Assembly.GetExecutingAssembly();

    public void AddServices(IServiceCollection services, IConfiguration configuration, object? mvcBuilder)
    {
        services.AddDbContext<ReflexDbContext>();
        services.AddScoped<IReflexDbContext>(provider => provider.GetRequiredService<ReflexDbContext>());
        services.AddScoped<ReflexDbContextInitializer>();

        // register web api controllers from this assembly
        (mvcBuilder as IMvcBuilder)?.AddApplicationPart(Assembly);

        services.AddScoped<ReflexHub>();
    }

    public void UseModule(IHost webApplication, object endpointConventionBuilder)
    {
        var app = (WebApplication)webApplication;
        app.MapHub<ReflexHub>(HubRoutes.Reflex);
    }

    public IDbContextInitializer? GetDbContextInitializer(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<ReflexDbContextInitializer>();
    }
}
