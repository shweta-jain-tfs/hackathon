using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Client.App.TaskCard.Models;
using Pharos.Client.App.TaskCard.ViewModels;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.App.Services;

namespace Pharos.Client.App.TaskCard;

public class TaskCardModule : IClientAppModule
{
    private TaskCardModule()
    {

    }

    public static IClientAppModule Instance { get; } = new TaskCardModule();

    public Assembly Assembly => Assembly.GetExecutingAssembly();

    void IClientAppModule.AddServices(IServiceCollection services)
    {
        //DataServices
        services.AddScoped<ICurrentInstrumentTaskCardItemSource, CurrentInstrumentTaskCardItemSource>();
        services.AddScoped<ILockScreenTaskCardItemSource, LockScreenTaskCardItemSource>();

        //Notifications
        services.AddScoped<ITaskCardRefreshNotificationProvider, TaskCardRefreshNotificationProvider>();

        //Services
        services.AddSingleton<ITaskCardConnectedPage, TaskCardConnectedPage>();
        services.AddSingleton<ITaskCardTranslator, TaskCardTranslator>();
        services.AddSingleton<ITimeCalculator, TimeCalculator>();

        //ViewModels
        services.AddTransient<TaskCardsPageViewModel>();
        services.AddTransient<TaskCardsLockScreenViewModel>();
    }
}