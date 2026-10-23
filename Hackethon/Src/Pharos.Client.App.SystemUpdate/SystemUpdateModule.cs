using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Client.App.SystemUpdate.Models;
using Pharos.Client.App.SystemUpdate.Notifications;
using Pharos.Client.App.SystemUpdate.Services;
using Pharos.Client.Common.Models;

namespace Pharos.Client.App.SystemUpdate;

public class SystemUpdateModule : IClientAppModule
{
    private SystemUpdateModule() { }

    public static IClientAppModule Instance { get; } = new SystemUpdateModule();

    public Assembly Assembly => Assembly.GetExecutingAssembly();

    public void AddServices(IServiceCollection services)
    {
        services.AddScoped<ISystemUpdateDataService, SystemUpdateDataService>();
        services.AddScoped<ISystemUpdateNotificationProvider, SystemUpdateNotificationProvider>();

        services.AddScoped<IAppNavigationService, AppNavigationService>();

        services.AddScoped<UpdateOutcomeChecker>();
    }
}
