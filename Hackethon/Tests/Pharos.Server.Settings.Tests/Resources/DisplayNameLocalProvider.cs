using System.Reflection;
using Microsoft.IdentityModel.Tokens;
using Pharos.Server.Settings.Application.Interfaces;
using Pharos.Server.Settings.Infrastructure.ConfigurationProvider;

namespace Pharos.Server.Settings.Resources;

/// <summary>
/// Provider for collecting missing localization keys for display names in settings pages. Used to test that all display names have corresponding translations.
/// </summary>
internal class DisplayNameLocalProvider : BaseSettingsPagesProvider
{
    private readonly HashSet<string> _missingResourceKeys = new();

    private DisplayNameLocalProvider(IValueConverter valueConverter, ISettingsStringLocalizer localizer)
        : base(valueConverter, localizer)
    {
    }

    private static DisplayNameLocalProvider CreateProvider(ISettingsStringLocalizer localizer)
    {
        return new(new ValueConverter(), localizer);
    }

    public static HashSet<string> GetAllPagesAndCollectMissingKeys(ISettingsStringLocalizer localizer, Func<Assembly, IEnumerable<Type>>? getConfigSections = null)
    {
        if (!_assemblies.Any())
        {
            return [];
        }
        var provider = CreateProvider(localizer);
        provider.VisitAssembliesAndGetPages(getConfigSections);
        return provider._missingResourceKeys;
    }

    /// <summary>
    /// Retrieves the localized text corresponding to the specified resource key. If the resource key is not found, it is added to an internal collection of missing resource
    /// keys  for tracking purposes.
    /// </summary>
    /// <param name="resourceKey">The key identifying the resource to retrieve.</param>
    /// <returns></returns>
    protected override string GetLocalizedText(string resourceKey)
    {
        if (_localizer is null)
        {
            throw new ArgumentException("Localizer can't be null when testing localized settings");
        }

        var localizedValue = _localizer[resourceKey];
        if (!resourceKey.IsNullOrEmpty() && localizedValue.ResourceNotFound)
        {
            _missingResourceKeys.Add(resourceKey);
        }
        return localizedValue;
    }

}