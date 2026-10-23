using System.Reflection;
using Pharos.Client.App.Help;
using Pharos.Client.App.Identity;
using Pharos.Client.App.InstrumentManagement;
using Pharos.Client.App.LoadList;
using Pharos.Client.App.Logs;
using Pharos.Client.App.Maintenance;
using Pharos.Client.App.Parameters;
using Pharos.Client.App.Quality;
using Pharos.Client.App.Reagents;
using Pharos.Client.App.Request;
using Pharos.Client.App.Result;
using Pharos.Client.App.Sample;
using Pharos.Client.App.Search;
using Pharos.Client.App.ServiceTools;
using Pharos.Client.App.Settings;
using Pharos.Client.App.SystemUpdate;
using Pharos.Client.App.TaskCard;
namespace Pharos.Client;

public static class Modules
{
    private static readonly IClientAppModule[] _moduleList =
    [
        LogsModule.Instance,
        HelpModule.Instance,
        IdentityModule.Instance,
        InstrumentManagementModule.Instance,
        MaintenanceModule.Instance,
        ParametersModule.Instance,
        QualityModule.Instance,
        ReagentsModule.Instance,
        RequestModule.Instance,
        ResultModule.Instance,
        SampleModule.Instance,
        ServiceToolsModule.Instance,
        SettingsModule.Instance,
        SearchModule.Instance,
        TaskCardModule.Instance,
        LoadListModule.Instance,
        SystemUpdateModule.Instance
    ];

    public static IServiceCollection AddModuleServices(this IServiceCollection services)
    {
        // configure services for individual application modules
        foreach (var module in _moduleList)
        {
            module.AddServices(services);
        }

        return services;
    }

    /// <summary>
    /// Gets collection of additional assemblies that should be searched for components that can match URIs.
    /// </summary>
    public static Assembly[] GetAdditionalAssemblies(ClientDeviceType deviceType)
    {
        var assemblies = new List<Assembly>();
        foreach (var module in _moduleList)
        {
            if (module.IsDeviceSpecific && module.DeviceType != deviceType)
            {
                continue; // Skip modules not applicable to the current device type
            }
            assemblies.Add(module.Assembly);
        }
        assemblies.Add(typeof(Modules).Assembly);
        return assemblies.ToArray();
    }

    public static Assembly[] GetAllAssemblies()
    {
        var assemblies = new List<Assembly>();
        foreach (var module in _moduleList)
        {
            assemblies.Add(module.Assembly);
        }
        assemblies.Add(typeof(Modules).Assembly);
        return assemblies.ToArray();
    }
}
