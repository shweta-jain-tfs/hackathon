using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pharos.Server.Application.LoadList;
using Pharos.Server.Infrastructure;
using Pharos.Server.Infrastructure.Database;
using Pharos.Server.LoadList.Application.Services;
using Pharos.Server.LoadList.Infrastructure.Database;
using Pharos.Server.LoadList.Infrastructure.Hubs;
using LoadListConfigurationInitializerService = Pharos.Server.LoadList.Application.Services.LoadListConfigurationInitializerService;

namespace Pharos.Server.LoadList;

public class LoadListModule : IServerAppModule
{
    private LoadListModule()
    {
    }

    public static IServerAppModule Instance { get; } = new LoadListModule();
    public Assembly Assembly => Assembly.GetExecutingAssembly();

    public void AddServices(IServiceCollection services, IConfiguration configuration, object? mvcBuilder)
    {
        services.AddDbContext<LoadListDbContext>();
        services.AddScoped<ILoadListDbContext>(provider => provider.GetRequiredService<LoadListDbContext>());
        services.AddScoped<LoadListDbContextInitializer>();

        (mvcBuilder as IMvcBuilder)?.AddApplicationPart(Assembly);
        services.AddSingleton<ILoadListService, LoadListService>();

        services.AddScoped<ILoadListHub, LoadListHub>();

        services.AddScoped<IArticleUsageService, ArticleUsageService>();
        services.AddScoped<ILoadListConfigurationInitializerService, LoadListConfigurationInitializerService>();

        services.AddScoped<LoadListKeyService>();
        services.AddScoped<ILoadListKeyService>(provider => provider.GetRequiredService<LoadListKeyService>());
        services.AddScoped<IStorageToLoadListKeyService>(provider => provider.GetRequiredService<LoadListKeyService>());
    }

    public void UseModule(IHost webApplication, object endpointConventionBuilder)
    {
        var app = (WebApplication)webApplication;
        app.MapHub<LoadListHub>(HubRoutes.LoadList);
    }

    public IDbContextInitializer? GetDbContextInitializer(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<LoadListDbContextInitializer>();
    }
}