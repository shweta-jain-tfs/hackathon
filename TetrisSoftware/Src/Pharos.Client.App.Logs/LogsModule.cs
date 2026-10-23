using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Client.App.Logs.Models.AuditTrail;
using Pharos.Client.App.Logs.Models.Errors;
using Pharos.Client.App.Logs.ViewModels;
using Pharos.Client.App.Logs.ViewModels.Errors;
using Pharos.Client.Common.Models.Notifications;

namespace Pharos.Client.App.Logs;

public class LogsModule : IClientAppModule
{
    private LogsModule()
    {
    }

    public static IClientAppModule Instance { get; } = new LogsModule();
    public Assembly Assembly => Assembly.GetExecutingAssembly();

    void IClientAppModule.AddServices(IServiceCollection services)
    {
        // DataServices
        services.AddScoped<AuditTrailDataService>();
        services.AddScoped<IAuditTrailDataService>(x => x.GetRequiredService<AuditTrailDataService>());

        services.AddScoped<IEntityDataSource<SystemErrorSummary>, SystemErrorSummaryDataSource>();
        services.AddScoped<ISystemErrorDetailsDataService, SystemErrorDetailsDataService>();
        services.AddScoped<IEntityDataService<SystemErrorDetails>>(provider => provider.GetRequiredService<ISystemErrorDetailsDataService>());

        // Notifications
        services.AddScoped<IEntityNotificationProvider<SystemErrorSummary>, SystemErrorSummaryNotificationProvider>();
        services.AddScoped<IEntityNotificationProvider<SystemErrorDetails>, SystemErrorDetailsNotificationProvider>();

        // Services
        services.AddScoped<IErrorTranslator, ErrorTranslator>();

        // ViewModels
        services.AddTransient<AuditTrailViewModel>();
        services.AddTransient<SystemErrorsListViewModel>();
        services.AddTransient<SystemErrorDetailsViewModel>();
        services.AddTransient<SystemErrorsInstrumentFilterViewModel>();
    }
}
