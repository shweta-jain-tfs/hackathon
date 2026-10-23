using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pharos.Server.Infrastructure;
using Pharos.Server.Infrastructure.Database;

namespace Pharos.Server.Search;

public class SearchModule : IServerAppModule
{
    private SearchModule()
    {

    }

    public static IServerAppModule Instance { get; } = new SearchModule();
    public Assembly Assembly => Assembly.GetExecutingAssembly();

    public void AddServices(IServiceCollection services, IConfiguration configuration, object? mvcBuilder)
    {
        (mvcBuilder as IMvcBuilder)?.AddApplicationPart(Assembly);
    }

    public IDbContextInitializer? GetDbContextInitializer(IServiceProvider serviceProvider)
    {
        return null;
    }

    public void UseModule(IHost webApplication, object endpointConventionBuilder)
    {

    }
}