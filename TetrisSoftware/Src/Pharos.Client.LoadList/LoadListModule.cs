using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Client.App.LoadList.Models;
using Pharos.Client.App.LoadList.ViewModels;
using Pharos.Client.Common.Models;

namespace Pharos.Client.App.LoadList;

public class LoadListModule : IClientAppModule
{
    private LoadListModule()
    {
    }

    public static IClientAppModule Instance { get; } = new LoadListModule();

    public Assembly Assembly => Assembly.GetExecutingAssembly();

    public void AddServices(IServiceCollection services)
    {
        //DataServices
        services.AddScoped<ILoadListDataSource, LoadListDataSource>();

        //Notifications
        services.AddScoped<ILoadListNotificationProvider, LoadListNotificationProvider>();

        //ViewModels
        services.AddTransient<LoadListPageViewModel>();
    }
}