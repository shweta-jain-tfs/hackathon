using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Client.App.Help.ViewModels;
using Pharos.Client.Common.Models;

namespace Pharos.Client.App.Help;

public class HelpModule : IClientAppModule
{
    private HelpModule()
    {
    }

    public static IClientAppModule Instance { get; } = new HelpModule();

    public Assembly Assembly => Assembly.GetExecutingAssembly();

    void IClientAppModule.AddServices(IServiceCollection services)
    {
        services.AddTransient<HelpViewModel>();
    }
}