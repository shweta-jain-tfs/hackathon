using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pharos.Server.Application.Settings;

namespace Pharos.Server.Settings.Infrastructure.ConfigurationProvider;

/// <summary>
/// Monitors changes of settings in a configuration from the specified provider
/// and publishes <see cref="SettingsChangedEvent{TSettingsGroup}"/> events.
/// </summary>
internal class SettingsMonitor : BackgroundService
{
    private readonly IConfigurationProvider _provider;
    private readonly IServiceCollection _services;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SettingsMonitor> _logger;

    private record MonitoredConfigSection(string Path, Type SettingsType, Type NotificationType);
    private readonly Dictionary<string, MonitoredConfigSection> _monitoredConfigSections = []; // Key: Configuration section path
    private IConfiguration? _configurationCache;
    private IDisposable? _changeTokenRegistration;


    public SettingsMonitor(IConfigurationProvider provider, IServiceCollection services, IServiceScopeFactory scopeFactory, ILogger<SettingsMonitor> logger)
    {
        _provider = provider;
        _services = services;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public IReadOnlyCollection<Type> MonitoredSettings => _monitoredConfigSections.Values.Select(x => x.SettingsType).ToList();

    public override void Dispose()
    {
        _changeTokenRegistration?.Dispose();
        base.Dispose();
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting settings monitor.");

        DiscoverMonitoredSettings();
        UpdateConfigurationCache();
        RegisterChangeCallback();

        _logger.LogDebug("Settings monitor started.");

        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        _logger.LogInformation("Settings monitor stopped.");
    }

    private void DiscoverMonitoredSettings()
    {
        using var scope = _scopeFactory.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>(); // needed to get section paths

        foreach (var descriptor in _services)
        {
            if (descriptor.Lifetime == ServiceLifetime.Singleton)
            {
                continue;
            }

            var serviceType = descriptor.ServiceType;
            if (!serviceType.IsGenericType || serviceType.GetGenericTypeDefinition() != typeof(INotificationHandler<>))
            {
                continue;
            }

            var notificationType = serviceType.GetGenericArguments().First();
            if (!notificationType.IsGenericType || notificationType.GetGenericTypeDefinition() != typeof(SettingsChangedEvent<>))
            {
                continue;
            }

            var settingsType = notificationType.GetGenericArguments().First();
            if (!settingsType.IsClass || settingsType.IsAbstract)
            {
                continue;
            }

            _logger.LogDebug("Discovered a notification handler for {Settings} settings type", settingsType);

            var getSectionPathMethod = typeof(SettingsServiceCollectionExtensions)
                .GetMethod(nameof(SettingsServiceCollectionExtensions.GetSectionPath))!
                .MakeGenericMethod(settingsType);

            var sectionPath = (string)getSectionPathMethod.Invoke(null, [configuration])!;

            _monitoredConfigSections[sectionPath] = new MonitoredConfigSection(sectionPath, settingsType, notificationType);
        }
    }

    private void UpdateConfigurationCache()
    {
        var allKeys = GetAllKeysRecursive(_provider, null, new HashSet<string>());

        // Filter to only include keys that are relevant to monitored sections
        var neededPaths = _monitoredConfigSections.Keys;

        var relevantKeysAndValues = allKeys
            .Where(key => neededPaths.Any(path => key.StartsWith(path, StringComparison.OrdinalIgnoreCase) &&
                                                  (key.Length == path.Length || key[path.Length] == ':')))
            .Select(key => new KeyValuePair<string, string?>(key, _provider.TryGet(key, out var value) ? value : null));

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(relevantKeysAndValues);

        _configurationCache = configurationBuilder.Build();
    }

    private static IEnumerable<string> GetAllKeysRecursive(
        IConfigurationProvider provider,
        string? parentPath,
        HashSet<string> visitedPaths)
    {
        var childKeys = provider.GetChildKeys(Enumerable.Empty<string>(), parentPath);

        foreach (var childKey in childKeys)
        {
            var fullPath = string.IsNullOrEmpty(parentPath) ? childKey : $"{parentPath}:{childKey}";

            if (!visitedPaths.Add(fullPath))
            {
                continue; // Already processed this path
            }

            // Check if this path has a value (is a leaf key)
            if (provider.TryGet(fullPath, out _))
            {
                yield return fullPath;
            }

            // Recursively get child keys
            foreach (var nestedKey in GetAllKeysRecursive(provider, fullPath, visitedPaths))
            {
                yield return nestedKey;
            }
        }
    }

    private void RegisterChangeCallback()
    {
        var changeToken = _provider.GetReloadToken();
        _changeTokenRegistration = changeToken.RegisterChangeCallback(_ => Task.Run(ProcessConfigurationChange), null);
    }

    private async Task ProcessConfigurationChange()
    {
        // Dispose the current registration first
        _changeTokenRegistration?.Dispose();
        _changeTokenRegistration = null;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

            var newConfiguration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            var needUpdateCache = false;
            foreach (var section in _monitoredConfigSections.Values)
            {
                var currentSection = newConfiguration.GetSection(section.Path);
                var cachedSection = _configurationCache!.GetSection(section.Path);

                if (!ConfigurationSectionsEqual(currentSection, cachedSection))
                {
                    needUpdateCache = true;

                    // Create an instance of the settings type and bind the configuration to it
                    var oldSettings = Activator.CreateInstance(section.SettingsType)!;
                    cachedSection.Bind(oldSettings);

                    var newSettings = Activator.CreateInstance(section.SettingsType)!;
                    currentSection.Bind(newSettings);

                    // Create the generic SettingsChangedEvent<T> instance
                    var settingsChangedEvent = (INotification)Activator.CreateInstance(section.NotificationType, oldSettings, newSettings)!;

                    // HandleNotification the event
                    await publisher.Publish(settingsChangedEvent);
                }
            }

            if (needUpdateCache)
            {
                UpdateConfigurationCache();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing settings changes.");
        }
        finally
        {
            // Re-register for the next change
            RegisterChangeCallback();
        }
    }

    private static bool ConfigurationSectionsEqual(IConfigurationSection current, IConfigurationSection cached)
    {
        // Compare values
        if (current.Value != cached.Value)
        {
            return false;
        }

        // Get children and compare
        var currentChildren = current.GetChildren().ToList();
        var cachedChildren = cached.GetChildren().ToList();

        if (currentChildren.Count != cachedChildren.Count)
        {
            return false;
        }

        // Compare each child recursively
        foreach (var currentChild in currentChildren)
        {
            var cachedChild = cachedChildren.FirstOrDefault(c => c.Key == currentChild.Key);
            if (cachedChild == null || !ConfigurationSectionsEqual(currentChild, cachedChild))
            {
                return false;
            }
        }

        return true;
    }
}