using Microsoft.Extensions.DependencyInjection;
using Pharos.Server.Localization;
using Pharos.Server.Settings.Application.Interfaces;
using Pharos.Server.Settings.Application.Services;
using Pharos.Server.Settings.Infrastructure.ConfigurationProvider;

namespace Pharos.Server.Settings.Resources;

public class DisplayNameLocalProviderTest : IDisposable
{
    public void Dispose()
    {
        BaseSettingsPagesProvider.Initialize([]);
    }

    /// <summary>
    /// Ensures that all settings have corresponding localization entries defined in the resource file.
    /// </summary>
    [Fact]
    public void Settings_Should_Have_NoMissingTranslationKeys()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLocalization();
        services.AddTransient<ISettingsStringLocalizer, SettingsStringLocalizer>();
        EntityConfigurationProvider.StringLocalizerResourceType = typeof(Translations);

        var serviceProvider = services.BuildServiceProvider();

        var localizer = serviceProvider.GetRequiredService<ISettingsStringLocalizer>();

        var configModules = Modules.ModuleList.Select(m => m.Assembly);
        Infrastructure.ConfigurationProvider.BaseSettingsPagesProvider.Initialize(configModules);

        // Act + Assert
        var missingKeys = DisplayNameLocalProvider.GetAllPagesAndCollectMissingKeys(localizer);

        Assert.Empty(missingKeys);
    }

}

