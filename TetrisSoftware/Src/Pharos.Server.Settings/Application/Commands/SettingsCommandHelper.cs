using System.Globalization;
using Pharos.Server.Settings.Application.Metadata;
using Pharos.Server.Settings.Application.Services;
using Pharos.Server.Settings.Application.Validation;
using Pharos.Server.Settings.Domain;
using Pharos.Server.Settings.Infrastructure.ConfigurationProvider;

namespace Pharos.Server.Settings.Application.Commands;

internal static class SettingsCommandHelper
{
    public static List<FailedSettingImportError> ValidateOptions(
        Dictionary<string, Option> keyOptions,
        List<Setting>? allSettings,
        IEnumerable<ISettingsCrossValidator>? crossValidators = null)
    {
        var errors = new List<FailedSettingImportError>();
        foreach (var keyOption in keyOptions)
        {
            var validateRequired = IsDependencyEnabled(keyOption.Value, keyOptions);
            var error = SettingsImportValidator.ValidateOption(keyOption.Value, allSettings, validateRequired);
            if (error != SettingImportError.None)
            {
                errors.Add(new FailedSettingImportError(keyOption.Key, keyOption.Value.Value ?? string.Empty, error));
            }
        }

        var dynamicDropdownOptions = keyOptions
            .Where(kv => !string.IsNullOrEmpty(kv.Value.DynamicDropdownOrigin))
            .Select(kv => kv.Value);

        foreach (var dynamicDropdownOption in dynamicDropdownOptions)
        {
            var parts = dynamicDropdownOption.Key.Split(PageHelper.SettingsKeyDeLimiter);
            var parentKey = string.Join(PageHelper.SettingsKeyDeLimiter, parts.Take(parts.Length - 1));

            var targetOptionValues = keyOptions.Where(kv => kv.Key.Contains(parentKey)
                                     && kv.Key.Contains(dynamicDropdownOption.DynamicDropdownOrigin)
                                     && kv.Key.Contains(dynamicDropdownOption.DynamicDropdownValueTarget))
                                    .Select(kv => kv.Value.Value)
                                    .ToList();

            if (!targetOptionValues.Contains(dynamicDropdownOption.Value))
            {
                errors.Add(new FailedSettingImportError(dynamicDropdownOption.Key, dynamicDropdownOption.Value ?? string.Empty, SettingImportError.InvalidValue));
            }
        }

        foreach (var validator in crossValidators ?? [])
        {
            errors.AddRange(validator.Validate(keyOptions));
        }

        return errors;
    }

    public static List<string> GetProviderManagedReadOnlyDisplayNamePageListKeys()
    {
        var pages = BaseDefaultSettingsProvider.GetTopLevelPages();

        if (pages.Count == 0)
        {
            return [];
        }

        var result = new List<string>();
        CollectProviderManagedReadOnlyDisplayNamePageListKeys(pages.Values, result);
        return result;
    }

    public static void RemoveProviderManagedReadOnlyDisplayNameOptions(
        Dictionary<string, Option> options,
        List<string> providerManagedReadOnlyDisplayNamePageListKeys)
    {
        if (providerManagedReadOnlyDisplayNamePageListKeys.Count == 0)
        {
            return;
        }

        var keysToRemove = options.Keys
            .Where(k => IsProviderManagedReadOnlyDisplayNameKey(k, providerManagedReadOnlyDisplayNamePageListKeys))
            .ToList();

        foreach (var key in keysToRemove)
        {
            options.Remove(key);
        }
    }

    public static bool IsProviderManagedReadOnlyDisplayNameKey(
        string key,
        List<string> providerManagedReadOnlyDisplayNamePageListKeys)
    {
        foreach (var pageListKey in providerManagedReadOnlyDisplayNamePageListKeys)
        {
            var prefix = pageListKey + PageHelper.SettingsKeyDeLimiter;
            if (!key.StartsWith(prefix, StringComparison.Ordinal))
            {
                continue;
            }

            var suffix = key[prefix.Length..];
            var delimiterIndex = suffix.IndexOf(PageHelper.SettingsKeyDeLimiter, StringComparison.Ordinal);
            if (delimiterIndex <= 0)
            {
                continue;
            }

            var index = suffix[..delimiterIndex];
            var propertyName = suffix[(delimiterIndex + 1)..];
            if (propertyName == PageList.RequiredTablePropertyName
                && int.TryParse(index, NumberStyles.None, CultureInfo.InvariantCulture, out _))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsDependencyEnabled(
        Option option,
        IDictionary<string, Option> keyOptions)
    {
        if (option.Dependency.Count == 0)
        {
            return true;
        }

        return option.Dependency
            .GroupBy(dependency => dependency.MainOptionKey)
            .All(group => group.Any(dependency => IsDependencyMatched(dependency, keyOptions)));
    }

    private static bool IsDependencyMatched(
        Dependency dependency,
        IDictionary<string, Option> keyOptions)
    {
        if (!keyOptions.TryGetValue(dependency.MainOptionKey, out var dependencyOption))
        {
            return true;
        }

        return MatchesDependencyValue(dependencyOption.Value, dependency.MainOptionValue);
    }

    private static bool MatchesDependencyValue(string? actualValue, string expectedValue)
    {
        if (bool.TryParse(actualValue, out var actualBool) && bool.TryParse(expectedValue, out var expectedBool))
        {
            return actualBool == expectedBool;
        }

        return string.Equals(actualValue, expectedValue, StringComparison.Ordinal);
    }

    private static void CollectProviderManagedReadOnlyDisplayNamePageListKeys(IEnumerable<Page> pages, List<string> result)
    {
        foreach (var page in pages)
        {
            CollectProviderManagedReadOnlyDisplayNamePageListKeys(page.Children, result);
        }
    }

    private static void CollectProviderManagedReadOnlyDisplayNamePageListKeys(IEnumerable<SettingsChildItem> children, List<string> result)
    {
        foreach (var child in children)
        {
            switch (child)
            {
                case PageList pageList:
                    if (IsProviderManagedReadOnlyDisplayNamePageList(pageList))
                    {
                        result.Add(pageList.Key);
                    }

                    if (pageList.PageStructure is Page pageStructure)
                    {
                        CollectProviderManagedReadOnlyDisplayNamePageListKeys(pageStructure.Children, result);
                    }

                    foreach (var pageEntry in pageList.PageEntries.OfType<Page>())
                    {
                        CollectProviderManagedReadOnlyDisplayNamePageListKeys(pageEntry.Children, result);
                    }

                    break;
                case Page page:
                    CollectProviderManagedReadOnlyDisplayNamePageListKeys(page.Children, result);
                    break;
                case Group group:
                    CollectProviderManagedReadOnlyDisplayNamePageListKeys(group.Children, result);
                    break;
            }
        }
    }

    private static bool IsProviderManagedReadOnlyDisplayNamePageList(PageList pageList)
    {
        if (!pageList.IsProviderManaged)
        {
            return false;
        }

        if (pageList.PageStructure is not Page pageStructure)
        {
            throw new InvalidOperationException(
                $"Provider-managed PageList '{pageList.Key}' must have a Page structure.");
        }

        var nameOptionKey = pageList.Key + PageHelper.SettingsKeyDeLimiter + PageList.RequiredTablePropertyName;
        var nameOption = PageHelper.FindOptions(pageStructure)
            .Values
            .FirstOrDefault(o => o.Key == nameOptionKey);

        return nameOption is { IsNameColumn: true, IsReadOnly: true };
    }
}
