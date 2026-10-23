using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Client.App.InstrumentManagement.Models.AssayScheduler;
using Pharos.Client.App.InstrumentManagement.Models.Dashboard;
using Pharos.Client.App.InstrumentManagement.Models.Instruments;
using Pharos.Client.App.InstrumentManagement.Models.TaskCards;
using Pharos.Client.App.InstrumentManagement.ViewModels.AssayScheduler;
using Pharos.Client.App.InstrumentManagement.ViewModels.Dashboard;
using Pharos.Client.App.InstrumentManagement.ViewModels.Instruments;
using Pharos.Client.App.InstrumentManagement.ViewModels.TaskCards;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.App.Services;
using Pharos.Client.Common.Models;
using Pharos.Client.Common.Models.DataServices;
using Pharos.Client.Common.Models.Notifications;

namespace Pharos.Client.App.InstrumentManagement;

public class InstrumentManagementModule : IClientAppModule
{
    private InstrumentManagementModule()
    {

    }

    public static IClientAppModule Instance { get; } = new InstrumentManagementModule();

    public Assembly Assembly => Assembly.GetExecutingAssembly();

    void IClientAppModule.AddServices(IServiceCollection services)
    {
        // Dashboard - DataServices
        services.AddScoped<IEntityDataSource<ProcessListItem>, ProcessListDataSource>();
        services.AddScoped<IEntityDataService<InstrumentMethod>, InstrumentMethodDataSource>();
        services.AddScoped<InstrumentStateDataSource>();
        services.AddScoped<ILisConnectionDataService, LisConnectionDataService>();
        services.AddScoped<LabCommunityConnectionDataService>();
        services.AddScoped<CurrentInstrumentEnvironmentDataSource>();

        // Dashboard - Notifications
        services.AddScoped<ILisNotificationProvider, LisConnectionNotificationProvider>();
        services.AddScoped<ILabCommunityNotificationProvider, LabCommunityNotificationProvider>();

        // Dashboard - ViewModels
        services.AddTransient<ConnectionsViewModel>();
        services.AddTransient<TemperaturesViewModel>();
        services.AddTransient<HumidityViewModel>();
        services.AddTransient<MethodSettingsViewModel>();
        services.AddTransient<ProcessListViewModel>();
        services.AddTransient<LisConnectionViewModel>();

        // Instruments - DataServices
        services.AddScoped<IEntityDataService<AssayScheduler>, AssaySchedulerDataService>();
        services.AddScoped<AssaySchedulerDataServiceForInstrument>();
        services.AddScoped<AssaySchedulerDayDataService>();
        services.AddScoped<IEntityDataSource<Instrument>, InstrumentsDataSource>();
        services.AddScoped<IEntityDataService<Instrument>, InstrumentDataService>();
        services.AddScoped<IInstrumentTechnologyDataService, InstrumentTechnologyDataService>();
        services.AddScoped<ITestResultDataService, TestResultDataService>();

        // Instruments - Notifications
        services.AddScoped<IEntityNotificationProvider<AssayScheduler>, AssaySchedulerListNotificationProvider>();
        services.AddScoped<IEntityNotificationProvider<Instrument>, InstrumentListNotificationProvider>();
        services.AddScoped<IInstrumentNotificationProvider, InstrumentNotificationProvider>();

        // Instruments - ViewModels
        services.AddTransient<AssaySchedulerViewModel>();
        services.AddTransient<InstrumentsViewModel>();

        // TaskCards - DataServices
        services.AddScoped<IPagedDataSource<TaskCardListItem, int>, TaskCardDataSource>();
        services.AddScoped<ITaskCardRefreshNotificationProvider, TaskCardRefreshNotificationProvider>();

        // TaskCards - Services
        services.AddSingleton<ITaskCardTranslator, TaskCardTranslator>();
        services.AddSingleton<ITimeCalculator, TimeCalculator>();

        // TaskCards - ViewModels
        services.AddTransient<TaskCardsListViewModel>();
    }
}
