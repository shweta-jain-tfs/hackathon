using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pharos.Server.Infrastructure.Database;
using Pharos.Server.Infrastructure.Services;
using Pharos.Server.Settings.Application.Interfaces;
using Pharos.Server.Settings.Application.Metadata;
using Pharos.Server.Settings.Application.Services;
using Pharos.Server.Settings.Domain;
using Pharos.Server.Settings.Infrastructure.ConfigurationProvider;

namespace Pharos.Server.Settings.Infrastructure.Database;

internal class SettingsDbContextInitializer : DbContextInitializer
{
    private readonly SettingsDbContext _context;
    private readonly ISettingsStringLocalizer _localizer;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<SettingsDbContextInitializer> _logger;

    public SettingsDbContextInitializer(
        SettingsDbContext context,
        IDbEncryptionService dbEncryptionService,
        ISettingsStringLocalizer localizer,
        IHostEnvironment environment,
        ILogger<SettingsDbContextInitializer> logger)
        : base(context, dbEncryptionService)
    {
        _context = context;
        _localizer = localizer;
        _environment = environment;
        _logger = logger;
    }
    protected override string SchemaName => SettingsDbContext.Schema;

    public override async Task InitializeAsync(RecreateObjectsMode recreateMode)
    {
        await base.InitializeAsync(recreateMode);

        // Run before the early-return below so obsolete settings are also cleaned up for existing
        // databases.
        await RemoveUnrecognizedSettingsFromDb();

        if (await _context.Settings.AnyAsync())
        {
            return;
        }

        await InsertInitialSettingsIntoDb(_environment.IsDevelopment());
        await RemoveDefaultSettingsFromDb();
    }

    private async Task RemoveUnrecognizedSettingsFromDb()
    {
        var assemblies = ConfigurationManagerExtensions.AssembliesWithSettings;
        if (assemblies == null || assemblies.Count == 0)
        {
            return;
        }

        var defaultOptions = BaseDefaultSettingsProvider.GetDefaultOptions();
        if (defaultOptions.Count == 0)
        {
            // Settings metadata is not available, so we cannot tell valid settings from obsolete
            // ones. Skip the cleanup to avoid deleting settings that are still valid.
            return;
        }

        var storedSettings = await _context.Settings
            .ToListAsync()
            .ConfigureAwait(false);

        var unrecognizedSettings = FilterUnrecognizedSettings(storedSettings, defaultOptions);
        if (unrecognizedSettings.Count == 0)
        {
            return;
        }

        foreach (var setting in unrecognizedSettings)
        {
            _logger.LogWarning(
                "Removing obsolete setting {SettingKey} from the database because it no longer maps to a known option.",
                setting.Key);
            _context.Settings.Remove(setting);
        }

        _logger.LogInformation(
            "Removed {Count} obsolete setting(s) from the database that could not be mapped to a known option.",
            unrecognizedSettings.Count);

        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    // Uses FindOption so that indexed list entries (keys with numeric segments) are still matched
    // against their option template and not treated as obsolete.
    internal static IReadOnlyCollection<Setting> FilterUnrecognizedSettings(
        IEnumerable<Setting> storedSettings,
        IDictionary<string, Option> defaultOptions)
    {
        return storedSettings
            .Where(setting => SettingValueStorage.FindOption(defaultOptions, setting.Key) is null)
            .ToList();
    }

    public override Task SeedAsync()
    {
        return Task.CompletedTask;
    }

    private async Task InsertInitialSettingsIntoDb(bool isDevelopment)
    {
        var assemblies = ConfigurationManagerExtensions.AssembliesWithSettings;
        if (assemblies == null || assemblies.Count == 0)
        {
            return;
        }

        var initialSettings = DefaultSettingsProvider.GetDefaultSettings(assemblies, isDevelopment: isDevelopment);
        var defaultOptions = BaseDefaultSettingsProvider.GetDefaultOptions();
        foreach (var setting in initialSettings)
        {
            // Only add if not already present (seeded settings take precedence)
            if (!await _context.Settings.AnyAsync(s => s.Key == setting.Key))
            {
                _context.Settings.Add(SettingValueStorage.CreateSetting(setting.Key, setting.Value, defaultOptions));
            }
        }

        if (_context.HasChanges())
        {
            await _context.SaveChangesAsync();
        }
    }


    private async Task RemoveDefaultSettingsFromDb()
    {
        var storedSettings = await _context.Settings
            .ToListAsync()
            .ConfigureAwait(false);
        var defaultOptions = BaseDefaultSettingsProvider.GetDefaultOptions();
        var settings = SettingValueStorage.ToSettingsDictionary(storedSettings, defaultOptions);
        var pages = SettingsPagesProvider.GetAllPages(_localizer);
        var populatedPages = new Dictionary<string, Page>();

        foreach (var page in pages.Values)
        {
            var populator = PopulatorFactory.GetPopulator(page);
            populator?.Populate(page, settings, ref populatedPages);
        }

        foreach (var options in populatedPages.Select(page => PageHelper.FindOptions(page.Value)))
        {
            var allSettings = await _context.Settings
                .ToListAsync(CancellationToken.None)
                .ConfigureAwait(false);

            var sameKeySettings = allSettings.Where(s => options.ContainsKey(s.Key));

            // update settings if needed
            foreach (var setting in sameKeySettings)
            {
                var matchingSetting = options[setting.Key];
                if (matchingSetting.Value == matchingSetting.DefaultValue)
                {
                    // remove setting from DB because value is default
                    _context.Settings.Remove(setting);
                }
            }
        }

        if (_context.HasChanges())
        {
            await _context.SaveChangesAsync();
        }
    }
}
