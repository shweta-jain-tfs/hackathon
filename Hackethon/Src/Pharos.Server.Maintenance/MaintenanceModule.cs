using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pharos.Contracts.Notifications;
using Pharos.Server.Infrastructure;
using Pharos.Server.Infrastructure.Database;
using Pharos.Server.Maintenance.Application.Interfaces;
using Pharos.Server.Maintenance.Infrastructure.Database;
using Pharos.Server.Maintenance.Infrastructure.Hubs;
using Pharos.Server.Maintenance.Services;

namespace Pharos.Server.Maintenance;

public class MaintenanceModule : IServerAppModule
{
    private MaintenanceModule()
    {
    }

    public static IServerAppModule Instance { get; } = new MaintenanceModule();

    public Assembly Assembly => Assembly.GetExecutingAssembly();

    public void AddServices(IServiceCollection services, IConfiguration configuration, object? mvcBuilder)
    {
        services.AddSingleton<IMaintenanceDefinitionProvider, MaintenanceDefinitionProvider>();

        services.AddDbContext<MaintenanceDbContext>();
        services.AddScoped<IMaintenanceDbContext>(provider => provider.GetRequiredService<MaintenanceDbContext>());
        services.AddScoped<MaintenanceDbContextInitializer>();

        // register web api controllers from this assembly
        (mvcBuilder as IMvcBuilder)?.AddApplicationPart(Assembly);

        services.AddScoped<IMaintenanceActivityHub, MaintenanceActivityHub>();
    }

    public void UseModule(IHost webApplication, object endpointConventionBuilder)
    {
        var app = (WebApplication)webApplication;
        app.MapHub<MaintenanceActivityHub>(HubRoutes.Maintenance);
    }

    public IDbContextInitializer GetDbContextInitializer(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<MaintenanceDbContextInitializer>();
    }
}
