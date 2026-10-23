using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pharos.Server.Infrastructure;
using Pharos.Server.Infrastructure.Database;

namespace Pharos.Server.Result.Flr;

public class ResultFlrModule : IServerAppModule
{
    private ResultFlrModule()
    {
    }

    public static IServerAppModule Instance { get; } = new ResultFlrModule();

    public Assembly Assembly => Assembly.GetExecutingAssembly();

    public void AddServices(IServiceCollection services, IConfiguration configuration, object? mvcBuilder)
    {
        // register web api controllers from this assembly
        (mvcBuilder as IMvcBuilder)?.AddApplicationPart(Assembly);
    }

    public void UseModule(IHost webApplication, object endpointConventionBuilder)
    {
    }

    public IDbContextInitializer? GetDbContextInitializer(IServiceProvider serviceProvider)
    {
        return null;
    }
}
