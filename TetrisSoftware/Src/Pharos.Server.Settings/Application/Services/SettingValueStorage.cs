using Pharos.Server.Settings.Application.Metadata;
using Pharos.Server.Settings.Domain;

namespace Pharos.Server.Settings.Application.Services;

internal static class SettingValueStorage
{
    public static Dictionary<string, string?> ToSettingsDictionary(
        IEnumerable<Setting> settings,
        IDictionary<string, Option> options)
    {
        return settings.ToDictionary(
            setting => setting.Key,
            setting => GetEffectiveValue(setting, options),
            StringComparer.Ordinal);
    }

    public static string? GetEffectiveValue(
        Setting setting,
        IDictionary<string, Option> options)
    {
        return GetEffectiveValue(setting, FindOption(options, setting.Key));
    }

    public static string? GetEffectiveValue(
        Setting setting,
        Option? option)
    {
        if (option?.IsEncrypted == true)
        {
            return setting.ProtectedValue;
        }

        return setting.Value;
    }

    public static Setting CreateSetting(Option option)
    {
        var setting = new Setting { Key = option.Key };
        SetStoredValue(setting, option, option.Value);
        return setting;
    }

    public static Setting CreateSetting(string key, string? value, IDictionary<string, Option> options)
    {
        var setting = new Setting { Key = key };
        SetStoredValue(setting, FindOption(options, key), value);
        return setting;
    }

    public static void SetStoredValue(Setting setting, Option? option, string? value)
    {
        if (option?.IsEncrypted == true)
        {
            setting.ProtectedValue = value;
            setting.Value = null;
            return;
        }

        setting.Value = value;
        setting.ProtectedValue = null;
    }

    public static bool NeedsStorageMigration(Setting setting, Option? option)
    {
        return option?.IsEncrypted == true
            ? setting.Value is not null
            : setting.ProtectedValue is not null;
    }

    public static Option? FindOption(IDictionary<string, Option> options, string key)
    {
        if (options.TryGetValue(key, out var option))
        {
            return option;
        }

        var keyWithoutIndices = RemoveNumericSegments(key);
        return options.TryGetValue(keyWithoutIndices, out option) ? option : null;
    }

    private static string RemoveNumericSegments(string key)
    {
        var segments = key.Split(PageHelper.SettingsKeyDeLimiter);
        return string.Join(
            PageHelper.SettingsKeyDeLimiter,
            segments.Where(segment => !int.TryParse(segment, out _)));
    }
}
