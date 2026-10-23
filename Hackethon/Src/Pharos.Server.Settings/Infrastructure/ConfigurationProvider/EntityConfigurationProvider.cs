using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Server.Infrastructure.Database;
using Pharos.Server.Settings.Application.Interfaces;
using Pharos.Server.Settings.Application.Services;
using Pharos.Server.Settings.Infrastructure.Database;

namespace Pharos.Server.Settings.Infrastructure.ConfigurationProvider;

internal sealed class EntityConfigurationProvider : Microsoft.Extensions.Configuration.ConfigurationProvider, ISaveChangesInterceptor
{
    private readonly IDictionary<string, string?> _defaultSettings;

    private EntityConfigurationProvider(IDictionary<string, string?> defaultSettings)
    {
        Data = defaultSettings;
        _defaultSettings = defaultSettings;
    }

    public static EntityConfigurationProvider? Instance { get; internal set; }

    public static Type? StringLocalizerResourceType { get; set; }

    public IServiceProvider? ServiceProvider
    {
        get;
        set
        {
            field = value;
            if (field != null)
            {
                // now we can get the database context and reload the settings
                Reload();
            }
        }
    }

    public static EntityConfigurationProvider Create(IDictionary<string, string?> defaultSettings)
    {
        if (Instance != null)
        {
            throw new InvalidOperationException($"Only one instance of {nameof(EntityConfigurationProvider)} is allowed.");
        }
        Instance = new EntityConfigurationProvider(defaultSettings);
        return Instance;
    }

    public override void Load()
    {
        if (ServiceProvider == null)
        {
            return;
        }

        var configuration = ServiceProvider.GetRequiredService<IConfiguration>();
        var interceptor = ServiceProvider.GetService<IAuditableEntitySaveChangesInterceptor>();
        using var dbContext = new SettingsDbContext(configuration, interceptor, null);

        var task = dbContext.Settings.ToListAsync();
        task.Wait();
        var storedSettings = task.Result;

        Data = _defaultSettings.ToDictionary();

        var options = BaseDefaultSettingsProvider.GetDefaultOptions();

        foreach (var setting in storedSettings)
        {
            Data[setting.Key] = SettingValueStorage.GetEffectiveValue(setting, options);
        }
    }

    public int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        if (ServiceProvider != null)
        {
            Reload();
        }
        return result;
    }

    public ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        if (ServiceProvider != null)
        {
            Reload();
        }
        return new ValueTask<int>(result);
    }

    public override void Set(string key, string? value)
    {
        if (Data.ContainsKey(key))
        {
            throw new NotSupportedException($"You are not allowed to modify Pharos settings via IConfiguration.Item[String] property. Use {nameof(ISettingsDbContext)} service.");
        }
    }

    private void Reload()
    {
        Load();
        OnReload();
    }
}
