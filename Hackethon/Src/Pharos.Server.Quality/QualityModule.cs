using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pharos.Server.Application.Quality;
using Pharos.Server.Infrastructure;
using Pharos.Server.Infrastructure.Database;
using Pharos.Server.Quality.Application.Services;

namespace Pharos.Server.Quality;

public class QualityModule : IServerAppModule
{
    private QualityModule()
    {
    }

    public static IServerAppModule Instance { get; } = new QualityModule();

    public Assembly Assembly => Assembly.GetExecutingAssembly();

    public void AddServices(IServiceCollection services, IConfiguration configuration, object? mvcBuilder)
    {
        // register web api controllers from this assembly
        (mvcBuilder as IMvcBuilder)?.AddApplicationPart(Assembly);

        // register QC data matrix service
        services.AddScoped<IQcCartridgeInfoService, QcCartridgeInfoService>();
    }

    public void UseModule(IHost webApplication, object endpointConventionBuilder)
    {
    }

    public IDbContextInitializer? GetDbContextInitializer(IServiceProvider serviceProvider)
    {
        return null;
    }
}
