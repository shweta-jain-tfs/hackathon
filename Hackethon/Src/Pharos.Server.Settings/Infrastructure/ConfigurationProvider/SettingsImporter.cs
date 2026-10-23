using System.Globalization;
using Microsoft.IdentityModel.Tokens;
using Pharos.Server.Settings.Application.Commands;
using Pharos.Server.Settings.Application.Metadata;
using Pharos.Server.Settings.Application.Services;

namespace Pharos.Server.Settings.Infrastructure.ConfigurationProvider;

/// <summary>
/// Helper class to parse settings imported from a CSV file.
/// </summary>
internal static class SettingsImporter
{
    private static readonly string[] _csvLineSeparators = ["\r", "\n"];

    /// <summary>
    /// Parses the CSV content to create a Page object containing settings as its Children.
    /// Invalid settings are considered as errors, returned as a list which includes the key, value and type of error.
    /// Unrecognized settings are ignored and returned as a list including key and value.
    /// The import is successful if there is NO invalid setting (key,value) found in the CSV content, fails otherwise. Unrecognized settings do not count as invalid.
    /// </summary>
    /// <param name="csvContent"></param>
    /// <param name="options"></param>
    /// <returns>(true when no invalid setting, page with found settings, required settings, list of errors, list of unrecognized settings)</returns>
    internal static (bool importStatus, Page page, List<FailedSettingImportError> importErrors, List<UnrecognizedSettingImportInfo> unrecognizedSettings) ParseCsvToSettings(string csvContent, IDictionary<string, Option> options)
    {
        return ParseCsvToSettings(csvContent, options, BaseDefaultSettingsProvider.GetImportExportExclusions());
    }

    internal static (bool importStatus, Page page, List<FailedSettingImportError> importErrors, List<UnrecognizedSettingImportInfo> unrecognizedSettings) ParseCsvToSettings(string csvContent, IDictionary<string, Option> options, ImportExportExclusions exclusions)
    {
        var page = new Page();
        var requiredSettings = new HashSet<string>();
        var importErrors = new List<FailedSettingImportError>();
        var unrecognizedSettings = new List<UnrecognizedSettingImportInfo>();

        var lines = csvContent.Split(_csvLineSeparators, StringSplitOptions.RemoveEmptyEntries);
        var settingsLines = lines
            .SkipWhile(line => !line.StartsWith(PageHelper.SettingsAsteriskLineAfterHeader))
            .Skip(1); // skip the delimiter line itself

        var validLines = 0;

        foreach (var line in settingsLines)
        {
            var values = line.Split(',');

            // After header, any line without a comma is a malformed setting row
            if (values.Length < 2)
            {
                importErrors.Add(new FailedSettingImportError(line, string.Empty, SettingImportError.MissingKeyValueDelimiter));
                continue;
            }

            validLines++;

            var key = values[0];
            var value = values[1];

            if (exclusions.IsExcluded(key))
            {
                unrecognizedSettings.Add(new UnrecognizedSettingImportInfo(key, value));
                continue;
            }

            var (option, importError) = GetOption(options, key, value);
            if (option == null)
            {
                if (importError == SettingImportError.UnrecognizedSetting)
                {
                    unrecognizedSettings.Add(new UnrecognizedSettingImportInfo(key, value));
                }
                else
                {
                    importErrors.Add(new FailedSettingImportError(key, value, importError));
                }
                continue;
            }

            page.Children.Add(option);
        }

        // remove duplicate keys, if any
        var duplicateKeysGroups = page.Children
            .OfType<Option>()
            .GroupBy(option => option.Key)
            .Where(g => g.Count() > 1);
        foreach (var duplicateKeyGroup in duplicateKeysGroups)
        {
            var duplicateKey = duplicateKeyGroup.Key;
            var values = duplicateKeyGroup.Select(g => g.Value);
            page.Children.RemoveAll(o => o.Key == duplicateKey);
            importErrors.AddRange(values.Select(value => new FailedSettingImportError(duplicateKey, value ?? string.Empty, SettingImportError.DuplicateKey)));
        }

        if (validLines != 0)
        {
            return (importErrors.Count == 0, page, importErrors, unrecognizedSettings);
        }

        importErrors.Add(new FailedSettingImportError(string.Empty, string.Empty, SettingImportError.MalformedFile));
        return (false, page, importErrors, unrecognizedSettings);
    }

    private static (Option?, SettingImportError) GetOption(IDictionary<string, Option> defaultOptions, string key, string value, string? originalKey = null)
    {
        originalKey ??= key;
        if (defaultOptions.TryGetValue(key, out var option))
        {
            if (!IsValidOptionValue(value, option.Type, option))
            {
                if (option.Type == OptionType.Choice)
                {
                    return (null, SettingImportError.InvalidEnumValue);
                }
                return (null, SettingImportError.InvalidValue);
            }

            // update the value from the exported setting
            var settingsKeyPrefix = originalKey == key ? string.Empty : GetParentKey(originalKey);
            option = (Option)option.DeepCopy(settingsKeyPrefix, string.Empty);
            option.Value = NormalizeValue(value, option.Type);
            option.Key = originalKey;

            return (option, SettingImportError.None);
        }

        // Remove numeric segment and try again so initialized page entries can be matched
        var pageEntryKey = RemoveNumericSegment(key);
        if (key != pageEntryKey)
        {
            return GetOption(defaultOptions, pageEntryKey, value, originalKey);
        }
        return (null, SettingImportError.UnrecognizedSetting);
    }

    private static string GetParentKey(string key)
    {
        var delimiterIndex = key.LastIndexOf(PageHelper.SettingsKeyDeLimiter, StringComparison.Ordinal);
        return delimiterIndex < 0 ? string.Empty : key[..delimiterIndex];
    }

    private static string RemoveNumericSegment(string key)
    {
        var parts = key.Split(':').ToList();

        var index = parts.FindIndex(p => int.TryParse(p, out _));

        if (index != -1)
        {
            parts.RemoveAt(index); // remove only the first numeric segment
        }

        var result = string.Join(":", parts);

        return result;
    }

    private static bool IsValidOptionValue(string? value, OptionType optionType, Option option)
    {
        switch (optionType)
        {
            case OptionType.Boolean:
                var upperValue = value?.Trim().ToUpperInvariant();
                return upperValue is "TRUE" or "FALSE" or "1" or "0";
            case OptionType.Integer:
                return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
            case OptionType.Double:
                return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
            case OptionType.String:
                return true;
            case OptionType.Date:
                return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
            case OptionType.Choice:
                return IsValidEnum(value, option);
            case OptionType.Time:
                return TimeOnly.TryParse(value, out _);
            default:
                return false;
        }
    }

    private static string? NormalizeValue(string? value, OptionType optionType)
    {
        if (optionType != OptionType.Boolean || value is null)
        {
            return value;
        }

        var trimmed = value.Trim();
        if (trimmed.Equals("true", StringComparison.OrdinalIgnoreCase) || trimmed == "1")
        {
            return "True";
        }

        if (trimmed.Equals("false", StringComparison.OrdinalIgnoreCase) || trimmed == "0")
        {
            return "False";
        }

        return value;
    }

    private static bool IsValidEnum(string? value, Option option)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }
        var enumNames = option.Choices.Select(c => c.Value);

        // In case of dynamic dropdown, there is no way to check the validity of the value
        if (!option.DynamicDropdownOrigin.IsNullOrEmpty() || option.DynamicDropdownProviderType != null)
        {
            return true;
        }
        return enumNames.Contains(value);
    }
}
