using System.Reflection;
using LabCommunity.API.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Pharos.Contracts.Notifications;
using Pharos.Server.Application.Settings;
using Pharos.Server.Application.Settings.LabCommunity;
using Pharos.Server.Infrastructure;
using Pharos.Server.Infrastructure.Database;
using Pharos.Server.LabCommunity.Application;
using Pharos.Server.LabCommunity.Application.Interfaces;
using Pharos.Server.LabCommunity.Application.Interfaces.CheckIn;
using Pharos.Server.LabCommunity.Application.Interfaces.GetConfiguration;
using Pharos.Server.LabCommunity.Application.Interfaces.EnrollmentApi;
using Pharos.Server.LabCommunity.Application.Interfaces.InstrumentStatus;
using Pharos.Server.LabCommunity.Application.Interfaces.PharosStatus;
using Pharos.Server.LabCommunity.Application.Jobs;
using Pharos.Server.LabCommunity.Application.Services;
using Pharos.Server.LabCommunity.Application.Services.CheckIn;
using Pharos.Server.LabCommunity.Application.Services.GetConfiguration;
using Pharos.Server.LabCommunity.Application.Services.EnrollmentApi;
using Pharos.Server.LabCommunity.Application.Services.InstrumentStatus;
using Pharos.Server.LabCommunity.Application.Services.PharosStatus;
using Pharos.Server.LabCommunity.Infrastructure.Database;
using Pharos.Server.LabCommunity.Infrastructure.Hubs;
using Pharos.Server.LabCommunity.Infrastructure.Web;

namespace Pharos.Server.LabCommunity;

public class LabCommunityModule : IServerAppModule
{
    private LabCommunityModule() { }
    public static IServerAppModule Instance { get; } = new LabCommunityModule();
    public Assembly Assembly { get; } = typeof(LabCommunityModule).Assembly;
    public void AddServices(IServiceCollection services, IConfiguration configuration, object? mvcBuilder)
    {
        services.ConfigureSettings<LabCommunitySettings>(configuration);
        services.Configure<LabCommunityDefaultConfiguration>(configuration.GetSection(LabCommunityDefaultConfiguration.SectionName));

        services.AddSingleton<IApiKeyService, ApiKeyService>();
        services.AddTransient<AddApiKeyHeaderHttpMessageHandler>();

        services.AddHttpClient<ILabCommunityApiClient, LabCommunityApiClient>()
            .ConfigureHttpClient((sp, client) =>
            {
                var settings = sp.GetRequiredService<IOptionsMonitor<LabCommunitySettings>>().CurrentValue;
                client.BaseAddress = new Uri(settings.ServerAddress);
            })
            .AddHttpMessageHandler<AddApiKeyHeaderHttpMessageHandler>();

        services.AddScoped<ILabCommunityEnrollmentService, LabCommunityEnrollmentService>();
        services.AddScoped<ILabCommunityCheckInService, LabCommunityCheckInService>();

        services.AddScoped<ILabCommunityJobScheduler, LabCommunityJobScheduler<InstrumentErrorExportJob>>();
        services.AddScoped<ILabCommunityJobScheduler, LabCommunityJobScheduler<CalibrationAndQualityResultExportJob>>();
        services.AddScoped<ILabCommunityJobScheduler, LabCommunityJobScheduler<ExportQueueCleanupJob>>();
        services.AddScoped<ILabCommunityJobScheduler, LabCommunityJobScheduler<CheckInJob>>();
        services.AddScoped<ILabCommunityJobScheduler, LabCommunityJobScheduler<GetConfigurationJob>>();
        services.AddScoped<ILabCommunityJobScheduler, LabCommunityJobScheduler<SystemStatusJob>>();
        services.AddScoped<ILabCommunitySchedulerService, LabCommunitySchedulerService>();

        services.AddSingleton<ILabCommunityCheckInState, LabCommunityCheckInState>();
        services.AddScoped<ILabCommunityConfigurationService, LabCommunityConfigurationService>();
        services.AddScoped<ILabCommunityPharosStatusService, LabCommunityPharosStatusService>();
        services.AddScoped<IPharosStatusSnapshotProvider, PharosStatusSnapshotProvider>();
        services.AddScoped<ILabCommunityInstrumentStatusService, LabCommunityInstrumentStatusService>();
        services.AddSingleton<IInstrumentMaintenanceActivityCache, InstrumentMaintenanceActivityCache>();
        services.AddScoped<IInstrumentStatusSnapshotProvider, InstrumentStatusSnapshotProvider>();
        services.AddScoped<ILabCommunityConnectionStatusService, LabCommunityConnectionStatusService>();
        services.AddScoped<ILabCommunityExportQueue, LabCommunityExportQueue>();

        services.AddDbContext<LabCommunityDbContext>();
        services.AddScoped<ILabCommunityDbContext>(provider => provider.GetRequiredService<LabCommunityDbContext>());
        services.AddScoped<LabCommunityDbContextInitializer>();

        services.AddScoped<ILabCommunityConnectionHub, LabCommunityConnectionHub>();
    }

    public void UseModule(IHost webApplication, object endpointConventionBuilder)
    {
        var app = (WebApplication)webApplication;
        app.MapHub<LabCommunityConnectionHub>(HubRoutes.LabCommunity);
    }

    public IDbContextInitializer? GetDbContextInitializer(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<LabCommunityDbContextInitializer>();
    }
}