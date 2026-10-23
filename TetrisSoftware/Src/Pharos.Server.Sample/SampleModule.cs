using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pharos.Contracts.Notifications;
using Pharos.Server.Application.Settings;
using Pharos.Server.Application.Settings.Sample;
using Pharos.Server.Infrastructure;
using Pharos.Server.Sample.Application.Interfaces;
using Pharos.Server.Sample.Application.Services;
using Pharos.Server.Sample.Infrastructure.Database;
using Pharos.Server.Sample.Infrastructure.Hubs;

namespace Pharos.Server.Sample;

public class SampleModule : IServerAppModule
{
    private SampleModule()
    {
    }

    public static IServerAppModule Instance { get; } = new SampleModule();

    public Assembly Assembly => Assembly.GetExecutingAssembly();

    public void AddServices(IServiceCollection services, IConfiguration configuration, object? mvcBuilder)
    {
        services.ConfigureSettings<SpecialSampleHandlingSettings>(configuration);
        services.ConfigureSettings<TubeBarcodesSettings>(configuration);
        services.ConfigureSettings<SamplesSettings>(configuration);

        services.AddDbContext<SampleDbContext>();
        services.AddScoped<ISampleDbContext>(provider => provider.GetRequiredService<SampleDbContext>());
        services.AddScoped<SampleDbContextInitializer>();
        services.AddScoped<ISampleQcIdProvider, SampleQcIdProvider>();

        services.AddSingleton<IInstrumentSampleStorage, InstrumentSampleStorage>();

        services.AddScoped<ISampleTrayHub, SampleTrayHub>();
        services.AddScoped<ISampleTubeHub, SampleTubeHub>();
        services.AddScoped<IQualityControlTubeHub, QualityControlTubeHub>();

        // register web api controllers from this assembly
        (mvcBuilder as IMvcBuilder)?.AddApplicationPart(Assembly);
    }

    public void UseModule(IHost webApplication, object endpointConventionBuilder)
    {
        var app = (WebApplication)webApplication;
        app.MapHub<SampleTrayHub>(HubRoutes.SampleTray);
        app.MapHub<SampleTubeHub>(HubRoutes.SampleTube);
        app.MapHub<QualityControlTubeHub>(HubRoutes.QualityControlTube);
    }

    public IDbContextInitializer GetDbContextInitializer(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<SampleDbContextInitializer>();
    }
}
