using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pharos.Contracts.Notifications;
using Pharos.Server.Application.Reagents;
using Pharos.Server.Infrastructure;
using Pharos.Server.Reagents.Application.Interfaces;
using Pharos.Server.Reagents.Application.Services;
using Pharos.Server.Reagents.Application.Services.ReagentBarcodeService;
using Pharos.Server.Reagents.Infrastructure.Database;
using Pharos.Server.Reagents.Infrastructure.Hubs;

namespace Pharos.Server.Reagents;

public class ReagentsModule : IServerAppModule
{
    private ReagentsModule()
    {
    }

    public static IServerAppModule Instance { get; } = new ReagentsModule();

    public Assembly Assembly => Assembly.GetExecutingAssembly();

    public void AddServices(IServiceCollection services, IConfiguration configuration, object? mvcBuilder)
    {
        services.AddDbContext<ReagentsDbContext>();
        services.AddScoped<IReagentsDbContext>(provider => provider.GetRequiredService<ReagentsDbContext>());
        services.AddScoped<ReagentsDbContextInitializer>();

        services.AddSingleton<InstrumentReagentStorage>();
        services.AddSingleton<IInstrumentReagentStorage>(provider => provider.GetRequiredService<InstrumentReagentStorage>());
        services.AddSingleton<IInstrumentReagentStorageUtilization>(provider => provider.GetRequiredService<InstrumentReagentStorage>());
        services.AddScoped<IReagentBarcodeService, ReagentBarcodeService>();
        services.AddScoped<ICheckDrawerTaskCardService, CheckDrawerTaskCardService>();

        services.AddScoped<IInventoryHub, InventoryHub>();

        // register web api controllers from this assembly
        (mvcBuilder as IMvcBuilder)?.AddApplicationPart(Assembly);
    }

    public void UseModule(IHost webApplication, object endpointConventionBuilder)
    {
        var app = (WebApplication)webApplication;
        app.MapHub<InventoryHub>(HubRoutes.Inventory);
    }

    public IDbContextInitializer GetDbContextInitializer(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<ReagentsDbContextInitializer>();
    }
}
