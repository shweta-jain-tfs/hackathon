using System.IO.Abstractions;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pharos.Contracts.Notifications;
using Pharos.Server.Infrastructure;
using Pharos.Server.Infrastructure.Database;
using Pharos.Server.Infrastructure.Events;
using Pharos.Server.SystemUpdate.Application.Events;
using Pharos.Server.SystemUpdate.Application.Interfaces;
using Pharos.Server.SystemUpdate.Application.Services;
using Pharos.Server.SystemUpdate.Domain;
using Pharos.Server.SystemUpdate.Infrastructure.Database;
using Pharos.Server.SystemUpdate.Infrastructure.Hubs;

namespace Pharos.Server.SystemUpdate;

public class SystemUpdateModule : IServerAppModule
{
    private SystemUpdateModule() { }
    public static IServerAppModule Instance { get; } = new SystemUpdateModule();
    public Assembly Assembly { get; } = typeof(SystemUpdateModule).Assembly;
    public IEnumerable<Type>? MappingProfiles { get; }

    public void AddServices(IServiceCollection services, IConfiguration configuration, object? mvcBuilder)
    {
        services.AddScoped<IUpdatePackageAccessor, UpdatePackageAccessor>();

        services.AddKeyedTransient<IUpdateService, OfflineUpdateService>(nameof(UpdateSource.Usb));
        services.AddKeyedTransient<IUpdateService, RemoteUpdateService>(nameof(UpdateSource.Remote));

        services.AddScoped<IUpdateServiceFactory, UpdateServiceFactory>();
        services.AddSingleton<IUpdateMediumStateStore, InMemoryUpdateMediumStateStore>();

        services.AddScoped<IDriveInsertedHandler, UsbInsertedHandler>();
        services.AddScoped<IDriveRemovedHandler, UsbRemovedHandler>();

        services.AddScoped<ISystemUpdateHub, SystemUpdateHub>();

        services.AddScoped<IFileSystem, FileSystem>();

        services.AddDbContext<SystemUpdateDbContext>();
        services.AddScoped<ISystemUpdateDbContext>(provider => provider.GetRequiredService<SystemUpdateDbContext>());
        services.AddScoped<SystemUpdateDbContextInitializer>();

        services.AddScoped<IUpdateOutcomeEvaluator, UpdateOutcomeEvaluator>();
        services.AddScoped<IUpdateMediumValidator, UpdateMediumValidator>();
    }

    public void UseModule(IHost webApplication, object endpointConventionBuilder)
    {
        var app = (WebApplication)webApplication;
        app.MapHub<SystemUpdateHub>(HubRoutes.SystemUpdate);
    }

    public IDbContextInitializer? GetDbContextInitializer(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<SystemUpdateDbContextInitializer>();
    }
}
