using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Client.App.Maintenance.Models;
using Pharos.Client.App.Maintenance.ViewModels;
using Pharos.Client.Common.Models;
using Pharos.Contracts;

namespace Pharos.Client.App.Maintenance;

public class MaintenanceModule : IClientAppModule
{
    private MaintenanceModule()
    {

    }

    public static IClientAppModule Instance { get; } = new MaintenanceModule();
    public bool IsDeviceSpecific => true;
    public ClientDeviceType? DeviceType => ClientDeviceType.Instrument;
    public Assembly Assembly => Assembly.GetExecutingAssembly();

    void IClientAppModule.AddServices(IServiceCollection services)
    {
        services.AddScoped<MaintenancePageViewModel>();

        services.AddScoped<MaintenanceItemsSource>();
        services.AddScoped<IMaintenanceItemsSource>(x => x.GetRequiredService<MaintenanceItemsSource>());
        services.AddScoped<IMaintenanceService>(x => x.GetRequiredService<MaintenanceItemsSource>());

        services.AddScoped<IMaintenanceRefreshNotificationProvider, MaintenanceRefreshNotificationProvider>();
        services.AddScoped<IActivityStepFactory, ActivityStepFactory>();
    }
}