using Microsoft.Extensions.Localization;
using Pharos.Server.Settings.Application.Interfaces;
using Pharos.Server.Settings.Infrastructure.ConfigurationProvider;
using Pharos.Server.Localization;

namespace Pharos.Server.Settings.Application.Services;

internal class SettingsStringLocalizer : ISettingsStringLocalizer
{
    private readonly IStringLocalizer[] _localizers;

    public SettingsStringLocalizer(IStringLocalizerFactory factory)
    {
        ResourceType = EntityConfigurationProvider.StringLocalizerResourceType
            ?? throw new InvalidOperationException($"{nameof(EntityConfigurationProvider.StringLocalizerResourceType)} is not initialized.");

        _localizers =
        [
            factory.Create(ResourceType),
            factory.Create(typeof(Translations))
        ];
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        return _localizers
            .SelectMany(localizer => localizer.GetAllStrings(includeParentCultures))
            .GroupBy(localizedString => localizedString.Name, StringComparer.Ordinal)
            .Select(group => group.First());
    }

    public LocalizedString this[string name] => GetLocalizedString(name, static (localizer, key) => localizer[key]);

    public LocalizedString this[string name, params object[] arguments] => GetLocalizedString(name, (localizer, key) => localizer[key, arguments]);

    public Type ResourceType { get; }

    private LocalizedString GetLocalizedString(string name, Func<IStringLocalizer, string, LocalizedString> getLocalizedString)
    {
        foreach (var localizer in _localizers)
        {
            var localizedString = getLocalizedString(localizer, name);
            if (!localizedString.ResourceNotFound)
            {
                return localizedString;
            }
        }

        return getLocalizedString(_localizers[0], name);
    }
}

