using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pharos.Server.Infrastructure;
using Pharos.Server.Infrastructure.Database;
using Pharos.Server.Lis.Astm.Application.Services;

namespace Pharos.Server.Lis.Astm;

public class LisAstmModule : IServerAppModule
{
    private LisAstmModule()
    {
    }

    public static IServerAppModule Instance { get; } = new LisAstmModule();

    public Assembly Assembly => Assembly.GetExecutingAssembly();

    public void AddServices(IServiceCollection services, IConfiguration configuration, object? mvcBuilder)
    {
        services.AddSingleton<IAstmDataLinkFactory, AstmDataLinkFactory>();
        services.AddKeyedSingleton<ILisConnectionFactory, AstmConnectionFactory>(LisServiceKeys.AstmConnectionFactory);
        services.AddKeyedSingleton<ILisMessageBuilder, AstmMessageBuilder>(LisServiceKeys.AstmMessageBuilder);

    }

    public void UseModule(IHost webApplication, object endpointConventionBuilder)
    {

    }

    public IDbContextInitializer? GetDbContextInitializer(IServiceProvider serviceProvider)
    {
        return null;
    }
}
