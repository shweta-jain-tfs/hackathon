using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pharos.Server.Infrastructure;
using Pharos.Server.Infrastructure.Database;
using Pharos.Server.Lis.Application.Interfaces;
using Pharos.Server.Lis.HL7.Application.Interfaces;
using Pharos.Server.Lis.HL7.Application.Services;

namespace Pharos.Server.Lis.HL7;

public class LisHL7Module : IServerAppModule
{
    private LisHL7Module()
    {
    }

    public static IServerAppModule Instance { get; } = new LisHL7Module();

    public Assembly Assembly => Assembly.GetExecutingAssembly();

    public void AddServices(IServiceCollection services, IConfiguration configuration, object? mvcBuilder)
    {
        services.AddSingleton<IHL7DataLinkFactory, HL7DataLinkFactory>();
        services.AddKeyedSingleton<ILisConnectionFactory, HL7ConnectionFactory>(LisServiceKeys.HL7ConnectionFactory);
        services.AddKeyedSingleton<ILisMessageBuilder, HL7MessageBuilder>(LisServiceKeys.HL7MessageBuilder);
    }

    public void UseModule(IHost webApplication, object endpointConventionBuilder)
    {

    }

    public IDbContextInitializer? GetDbContextInitializer(IServiceProvider serviceProvider)
    {
        return null;
    }
}
