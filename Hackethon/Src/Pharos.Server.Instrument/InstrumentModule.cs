using System.Reflection;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Pharos.Contracts.Notifications;
using Pharos.Server.Application;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Settings;
using Pharos.Server.Application.Settings.Instrument;
using Pharos.Server.Application.Settings.Reagents;
using Pharos.Server.Grpc;
using Pharos.Server.Infrastructure;
using Pharos.Server.Infrastructure.Database;
using Pharos.Server.Infrastructure.Services;
using Pharos.Server.Instrument.Application.Interfaces;
using Pharos.Server.Instrument.Application.Services;
using Pharos.Server.Instrument.Application.Services.Pairing;
using Pharos.Server.Instrument.Application.Validators;
using Pharos.Server.Instrument.Infrastructure.Database;
using Pharos.Server.Instrument.Infrastructure.Grpc;
using Pharos.Server.Instrument.Infrastructure.Grpc.Interceptors;
using Pharos.Server.Instrument.Infrastructure.Hubs;
using Pharos.Server.Instrument.Infrastructure.Kestrel;
using Tetris.Protocol.Pharos;
using Tetris.Protocol.Pharos.Storage;

namespace Pharos.Server.Instrument;

public class InstrumentModule : IServerAppModule
{
    private InstrumentModule()
    {
    }

    public static IServerAppModule Instance { get; } = new InstrumentModule();

    public Assembly Assembly => Assembly.GetExecutingAssembly();

    public void AddServices(IServiceCollection services, IConfiguration configuration, object? mvcBuilder)
    {
        services.AddOptions<GrpcSettings>().BindConfiguration(GrpcSettings.GrpcSettingsSectionName);
        services.ConfigureSettings<InstrumentSettings>(configuration);
        services.ConfigureSettings<ReagentsSettings>(configuration);

        services.AddDbContext<InstrumentDbContext>();
        services.AddScoped<IInstrumentDbContext>(provider => provider.GetRequiredService<InstrumentDbContext>());
        services.AddScoped<InstrumentDbContextInitializer>();

        services.AddSingleton<ITetrisProtocolInformation, TetrisProtocolInformation>();
        services.AddSingleton<IInstrumentConnectionFactory, InstrumentConnectionFactory>();

        services.AddHostedServiceAsSingleton<ICommunicationSessionManager, CommunicationSessionManager>();
        services.AddHostedServiceAsSingleton<IInstrumentManager, InstrumentManager>();
        services.AddKeyedTransient<IPageListEntryProvider, InstrumentEntryProvider>(InstrumentSettingsProviderKeys.InstrumentEntryProvider);
        services.AddSingleton<IAutomaticPowerSaveScheduler, AutomaticPowerSaveScheduler>();

        services.AddScoped<ICurrentInstrumentService, CurrentInstrumentService>();

        // add gRPC services
        services.AddSingleton<IConfigureOptions<KestrelServerOptions>, ConfigureKestrel>();
        services.AddSingleton<ClientCertificateCache>();
        services
            .AddGrpc(o =>
            {
                o.Interceptors.Add<ServerLoggingInterceptor>();
                o.MaxReceiveMessageSize = 10 * 1024 * 1024; // 10 MB
            })
            .AddServiceOptions<PairingServiceImplementation>(o =>
            {
                o.Interceptors.Add<CheckPairingModeInterceptor>();
            })
            .AddServiceOptions<PharosServiceImplementation>(o =>
            {
                o.Interceptors.Add<AuthInterceptor>();
                o.Interceptors.Add<CallSynchronizationInterceptor>();
                o.Interceptors.Add<CheckSessionInterceptor>();
                o.Interceptors.Add<ExceptionInterceptor>();
            });

        services.AddSingleton<IInstrumentPairingService, InstrumentPairingService>();
        services.AddSingleton<IRemoteCallSynchronizer, RemoteCallSynchronizer>();
        services.AddTransient<IValidator<InstrumentStorage>, InstrumentStorageValidator>();
        services.AddTransient<IValidator<TestOrderStatusChange>, TestOrderStatusChangeValidator>();

        // register web api controllers from this assembly
        (mvcBuilder as IMvcBuilder)?.AddApplicationPart(Assembly);

        // register SignalR hubs
        services.AddScoped<IInstrumentPairingHub, PairingHub>();
        services.AddScoped<ICurrentInstrumentServerHub, CurrentInstrumentHub>();
        services.AddScoped<IInstrumentHub, InstrumentHub>();
        services.AddScoped<IDrawerAuthorizationService, DrawerAuthorizationService>();
    }

    public void UseModule(IHost webApplication, object endpointConventionBuilder)
    {
        var app = (WebApplication)webApplication;
        app.MapHub<PairingHub>(HubRoutes.Pairing);
        app.MapHub<CurrentInstrumentHub>(HubRoutes.CurrentInstrument);
        app.MapHub<InstrumentHub>(HubRoutes.Instrument);

        var grpcSettings = webApplication.Services.GetRequiredService<IOptions<GrpcSettings>>().Value;
        app.MapGrpcService<PairingServiceImplementation>()
            .RequireHost($"*:{grpcSettings.PairingService.Port}");
        app.MapGrpcService<PharosServiceImplementation>()
            .RequireHost($"*:{grpcSettings.MainService.Port}");
    }

    public IDbContextInitializer GetDbContextInitializer(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<InstrumentDbContextInitializer>();
    }
}

