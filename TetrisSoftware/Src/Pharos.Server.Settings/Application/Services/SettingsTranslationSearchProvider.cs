using Pharos.Server.Settings.Application.Interfaces;

namespace Pharos.Server.Settings.Application.Services;

internal class SettingsTranslationSearchProvider(ISettingsStringLocalizer localizer) : ITranslationSearchProvider
{
    public List<TranslationSearchMatch> GetMatchingTranslations(string searchString)
    {
        return localizer
            .GetAllStrings(includeParentCultures: true)
            .Where(localizedString =>
                !string.IsNullOrWhiteSpace(localizedString.Name) &&
                !string.IsNullOrWhiteSpace(localizedString.Value) &&
                localizedString.Value.Contains(searchString, StringComparison.OrdinalIgnoreCase))
            .GroupBy(localizedString => localizedString.Name, StringComparer.Ordinal)
            .Select(group => new TranslationSearchMatch(group.Key, group.First().Value))
            .ToList();
    }
}