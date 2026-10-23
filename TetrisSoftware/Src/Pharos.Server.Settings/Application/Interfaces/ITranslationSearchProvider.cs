namespace Pharos.Server.Settings.Application.Interfaces;

internal readonly record struct TranslationSearchMatch(string Key, string Value);

internal interface ITranslationSearchProvider
{
    List<TranslationSearchMatch> GetMatchingTranslations(string searchString);
}