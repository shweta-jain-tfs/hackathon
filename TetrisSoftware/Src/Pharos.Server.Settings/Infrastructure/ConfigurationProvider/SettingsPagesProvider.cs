using System.Reflection;
using Microsoft.Extensions.Localization;
using Pharos.Server.Application.Settings;
using Pharos.Server.Application.Settings.Attributes;
using Pharos.Server.Settings.Application.Metadata;

namespace Pharos.Server.Settings.Infrastructure.ConfigurationProvider;

internal sealed class SettingsPagesProvider : BaseSettingsPagesProvider
{
    private SettingsPagesProvider(IValueConverter valueConverter, IStringLocalizer localizer)
        : base(valueConverter, localizer)
    {
    }

    private static SettingsPagesProvider CreateProvider(IStringLocalizer localizer)
    {
        return new(new ValueConverter(), localizer);
    }

    public static Dictionary<string, Page> GetAllPages(IStringLocalizer localizer, Func<Assembly, IEnumerable<Type>>? getConfigSections = null)
    {
        if (!_assemblies.Any())
        {
            return [];
        }
        return CreateProvider(localizer).VisitAssembliesAndGetPages(getConfigSections);
    }

    public static Dictionary<string, Page> GetPage(IStringLocalizer localizer, Func<Assembly, IEnumerable<Type>>? getConfigSections = null)
    {
        if (!_assemblies.Any())
        {
            return [];
        }
        return CreateProvider(localizer).VisitAssembliesAndGetPages(getConfigSections);
    }

    public static async Task<Dictionary<string, string>> GetMenuItemsAsync(IStringLocalizer localizer, Func<string[], Task<bool>> hasPermissions)
    {
        if (!_assemblies.Any())
        {
            return [];
        }

        var provider = CreateProvider(localizer);
        var rootConfigs = provider.GetRootConfigs(_assemblies)
            .OrderBy(t => t.GetCustomAttribute<RootConfigSectionAttribute>()?.SortOrder ?? 0);

        // De-structure rootConfigs and then apply permission validation
        var result = new Dictionary<string, string>();
        foreach (var type in rootConfigs)
        {
            var key = SettingsServiceCollectionExtensions.GetConfigSectionName(type);
            var label = type.GetCustomAttribute<DisplayNameLocalAttribute>()?.ResourceName
                        ?? throw new ArgumentException($"DisplayNameLocalAttribute is missing from root level page '{SettingsServiceCollectionExtensions.GetConfigSectionName(type)}'");
            var permissions = GetPermissionForType(type);

            var effectivePermissions = permissions?
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToArray();

            if (effectivePermissions == null
                || effectivePermissions.Length == 0
                || await hasPermissions(effectivePermissions))
            {
                result[key] = localizer[label];
            }
        }
        return result;
    }

    public static Type? GetRootType(IStringLocalizer localizer, string rootPageName)
    {
        if (!_assemblies.Any())
        {
            return null;
        }

        var provider = CreateProvider(localizer);
        var rootConfigs = provider.GetRootConfigs(_assemblies);
        // TODO: add error here if null
        var type = rootConfigs.First(t => (SettingsServiceCollectionExtensions.GetConfigSectionName(t)) == rootPageName);

        return type;
    }

    public static SettingsPagesProvider? Instance { get; private set; }

    public static void InitializeInstance(IStringLocalizer localizer)
    {
        Instance = CreateProvider(localizer);
    }
}