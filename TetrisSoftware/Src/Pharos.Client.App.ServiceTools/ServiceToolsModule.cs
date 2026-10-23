using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Client.App.ServiceTools.Models;
using Pharos.Client.App.ServiceTools.Services;
using Pharos.Client.App.ServiceTools.ViewModels;
using Pharos.Client.App.ServiceTools.ViewModels.RequestGenerator;

namespace Pharos.Client.App.ServiceTools;

public class ServiceToolsModule : IClientAppModule
{
    private ServiceToolsModule() { }

    public static IClientAppModule Instance { get; } = new ServiceToolsModule();

    public Assembly Assembly => Assembly.GetExecutingAssembly();

    void IClientAppModule.AddServices(IServiceCollection services)
    {
        // Data Services
        services.AddScoped<IRequestGeneratorDataService, RequestGeneratorDataService>();
        services.AddScoped<ILogDumpDataService, LogDumpDataService>();
        services.AddScoped<IMethodTestDataSource, MethodTestDataSource>();
        services.AddScoped<IRequestGeneratorStateService, RequestGeneratorStateService>();

        // Notification providers
        services.AddScoped<PairingNotificationProvider>();

        // ViewModels
        services.AddTransient<FlrImportViewModel>();
        services.AddTransient<AddTestViewModel>();
        services.AddTransient<EditTestViewModel>();
        services.AddTransient<TestListViewModel>();
        services.AddTransient<GeneratorViewModel>();
        services.AddTransient<LoadCmrViewModel>();
        services.AddTransient<CollectLogFilesViewModel>();
        services.AddTransient<PairingViewModel>();
        services.AddTransient<LogDumpViewModel>();
    }
}
