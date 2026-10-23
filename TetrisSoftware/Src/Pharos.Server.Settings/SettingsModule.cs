using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pharos.Contracts.Notifications;
using Pharos.Server.Application;
using Pharos.Server.Application.Settings;
using Pharos.Server.Application.Settings.General;
using Pharos.Server.Infrastructure;
using Pharos.Server.Infrastructure.Database;
using Pharos.Server.Settings.Application.Interfaces;
using Pharos.Server.Settings.Application.Services;
using Pharos.Server.Settings.Application.Validation;
using Pharos.Server.Settings.Infrastructure.ConfigurationProvider;
using Pharos.Server.Settings.Infrastructure.Database;
using Pharos.Server.Settings.Infrastructure.Hubs;

namespace Pharos.Server.Settings;

public sealed class SettingsModule : IServerAppModule
{
    private SettingsModule()
    {
    }
    public static IServerAppModule Instance { get; } = new SettingsModule();

    public Assembly Assembly => GetType().Assembly;

    public void AddServices(IServiceCollection services, IConfiguration configuration, object? mvcBuilder)
    {
        services.AddDbContext<SettingsDbContext>();
        services.AddScoped<ISettingsDbContext>(p => p.GetRequiredService<SettingsDbContext>());
        services.AddScoped<SettingsDbContextInitializer>();


        services.AddScoped<ISettingsStringLocalizer, SettingsStringLocalizer>();
        services.AddScoped<ITranslationSearchProvider, SettingsTranslationSearchProvider>();
        services.AddScoped<ISearchProvider, SettingsSearch>();
        services.AddScoped<ISettingsPermissionHelper, SettingsPermissionHelper>();
        services.AddScoped<ISettingsPagesLoader, SettingsPagesLoader>();
        services.AddScoped<IPageTreeNavigator, PageTreeNavigator>();
        services.AddScoped<ISettingsCopy, SettingsCopy>();
        services.AddScoped<IPageListMaxItemsValidator, PageListMaxItemsValidator>();
        services.AddScoped<IAbsoluteDirectoryWriteAccessValidator, AbsoluteDirectoryWriteAccessValidator>();
        services.AddScoped<ISettingsCrossValidator, NonOverlappingRangesSettingsValidator>();
        services.AddScoped<ISettingsHub, SettingsHub>();

        services.AddTransient<IDropdownValueProvider, TimeZoneDropdownProvider>();

        services.AddHostedService(sp =>
        {
            var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
            var logger = sp.GetRequiredService<ILogger<SettingsMonitor>>();
            return new SettingsMonitor(GetConfigurationProvider(), services, scopeFactory, logger);
        });
    }

    public void UseModule(IHost webApplication, object endpointConventionBuilder)
    {
        var configProvider = GetConfigurationProvider();
        configProvider.ServiceProvider = webApplication.Services;

        var app = (WebApplication)webApplication;
        app.MapHub<SettingsHub>(HubRoutes.Settings);
    }

    internal static EntityConfigurationProvider GetConfigurationProvider()
    {
        var configProvider = EntityConfigurationProvider.Instance;
        if (configProvider == null)
        {
            throw new InvalidOperationException($"{nameof(EntityConfigurationProvider)} is not initialized. The bootstrap method {nameof(ConfigurationManagerExtensions.AddEntityConfiguration)} must be called first.");
        }

        return configProvider;
    }

    public IDbContextInitializer GetDbContextInitializer(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<SettingsDbContextInitializer>();
    }
}
