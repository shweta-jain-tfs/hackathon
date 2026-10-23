using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pharos.Server.Infrastructure;
using Pharos.Server.Infrastructure.Database;
using Pharos.Server.LogDump.Application.Interfaces;
using Pharos.Server.LogDump.Application.Services;

namespace Pharos.Server.LogDump;

public class LogDumpModule : IServerAppModule
{
    private LogDumpModule()
    {

    }

    public static IServerAppModule Instance { get; } = new LogDumpModule();

    public Assembly Assembly => Assembly.GetExecutingAssembly();

    public void AddServices(IServiceCollection services, IConfiguration configuration, object? mvcBuilder)
    {
        services.AddSingleton<LogDumpResultStore>();
        services.AddSingleton<ILogDumpResultStore>(provider => provider.GetRequiredService<LogDumpResultStore>());

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