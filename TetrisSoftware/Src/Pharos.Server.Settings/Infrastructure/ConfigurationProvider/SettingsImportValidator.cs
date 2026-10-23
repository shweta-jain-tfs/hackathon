using System.Globalization;
using System.Text.RegularExpressions;
using Pharos.Server.Settings.Application.Metadata;
using Pharos.Server.Settings.Application.Services;
using Pharos.Server.Settings.Domain;

namespace Pharos.Server.Settings.Infrastructure.ConfigurationProvider;

/// <summary>
/// Helper class to validate the imported settings and record all the errors.
/// </summary>
internal static partial class SettingsImportValidator
{
    private const string _idPattern = @":[0-9]+:";

    /// <summary>
    /// Validates a single option against its constraints (type and attributes).
    /// </summary>
    /// <param name="option"></param>
    /// <param name="allSettings"></param>
    /// <param name="validateRequired">Whether the option's required constraint should be validated.</param>
    /// <returns></returns>
    internal static SettingImportError ValidateOption(Option option, List<Setting>? allSettings, bool validateRequired = true)
    {
        if (option.Type == OptionType.Integer && !IsValidNumber(option.Value, option.MinValue, option.MaxValue, int.Parse))
        {
            return SettingImportError.InvalidValue;
        }
        if (option.Type == OptionType.Double && !IsValidNumber(option.Value, option.MinValue, option.MaxValue, double.Parse))
        {
            return SettingImportError.InvalidValue;
        }
        if (option.Type == OptionType.String && option.Value?.Length > option.MaxLength)
        {
            return SettingImportError.InvalidValue;
        }
        if (option.Type == OptionType.String
            && !string.IsNullOrEmpty(option.Value)
            && !string.IsNullOrEmpty(option.DisallowedCharactersPattern)
            && Regex.IsMatch(option.Value, option.DisallowedCharactersPattern))
        {
            return SettingImportError.InvalidValue;
        }
        if (option.Type == OptionType.Time && !TimeOnly.TryParse(option.Value, out _))
        {
            return SettingImportError.InvalidValue;
        }
        if (validateRequired && option.Required && string.IsNullOrWhiteSpace(option.Value))
        {
            return SettingImportError.RequiredSettingNotDefined;
        }
        if (option.IsAbsoluteDirectoryPath && !ValueValidator.IsValidAbsoluteDirectoryPath(option.Value ?? ""))
        {
            return SettingImportError.InvalidValue;
        }
        if (option.Unique && IsUniqueValueFound(option, allSettings))
        {
            return SettingImportError.DuplicateValue;
        }

        return SettingImportError.None;
    }

    private static bool IsValidNumber<T>(string? value, string minValue, string maxValue, Func<string, IFormatProvider, T> parse) where T : IComparable<T>
    {
        var parsedValue = parse(value!, CultureInfo.InvariantCulture);
        var min = parse(minValue, CultureInfo.InvariantCulture);
        var max = parse(maxValue, CultureInfo.InvariantCulture);

        return parsedValue.CompareTo(min) >= 0 && parsedValue.CompareTo(max) <= 0;
    }

    private static bool IsUniqueValueFound(Option option, List<Setting>? allSettings)
    {
        var itemRegex = GetDuplicateValueRegex(option.Key);
        var comparison = option.UniqueIsCaseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return allSettings != null &&
               allSettings.Any(s =>
                       itemRegex.IsMatch(s.Key) &&
                       s.Key != option.Key &&
                       string.Equals(
                       SettingValueStorage.GetEffectiveValue(s, option),
                       option.Value,
                       comparison));
    }

    private static Regex GetDuplicateValueRegex(string key)
    {
        var (foundId, idIndex) = GetIdValueAndIndex(key);

        var prefixSegments = key[..idIndex];
        var attributeSegment = key[(idIndex + foundId.Length)..];

        var itemPattern = $@"^{prefixSegments}{_idPattern}{attributeSegment}$";
        return new Regex(itemPattern);
    }

    private static (string foundId, int idIndex) GetIdValueAndIndex(string key)
    {
        var idRegex = GetIdRegex();
        var idMatch = idRegex.Match(key);
        return (idMatch.Value, idMatch.Index);
    }

    [GeneratedRegex(_idPattern)]
    private static partial Regex GetIdRegex();
}
