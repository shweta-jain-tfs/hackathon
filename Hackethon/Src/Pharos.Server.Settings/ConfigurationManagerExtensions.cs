using System.Reflection;
using Microsoft.Extensions.Configuration;
using Pharos.Server.Application.Settings.Attributes;
using Pharos.Server.Settings.Infrastructure.ConfigurationProvider;

namespace Pharos.Server.Settings;

public static class ConfigurationManagerExtensions
{
    /// <summary>
    /// Adds Pharos settings which are stored in the database to the .NET configuration system.
    /// </summary>
    /// <param name="manager">The configuration manager.</param>
    /// <param name="assemblies">
    /// A collection of assemblies to scan for classes representing groups of settings.
    /// The settings classes must be decorated with <see cref="RootConfigSectionAttribute"/>.
    /// </param>
    /// <param name="stringLocalizerResourceType">The type of the resource used for localization.</param>
    /// <returns>The configuration manager instance.</returns>
    public static ConfigurationManager AddEntityConfiguration(
        this ConfigurationManager manager, IReadOnlyCollection<Assembly> assemblies, Type stringLocalizerResourceType)
    {
        IConfigurationBuilder configBuilder = manager;
        AssembliesWithSettings = assemblies;
        configBuilder.Add(new EntityConfigurationSource(assemblies, stringLocalizerResourceType));
        return manager;
    }

    public static void InitializeSettingsPages(IEnumerable<Assembly> assemblies)
    {
        SettingsPagesProvider.Initialize(assemblies);
        SettingsSearchPagesProvider.Initialize(assemblies);
        SettingsDisplayNameResourceKeyProvider.ResetCache();
        SettingsDisplayNameResourceKeyProvider.PreloadDisplayNameResourceKeys();
    }

    internal static IReadOnlyCollection<Assembly>? AssembliesWithSettings { get; set; }
}