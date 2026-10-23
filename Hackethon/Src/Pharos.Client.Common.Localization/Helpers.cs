namespace Pharos.Client.Common.Localization;

public static class Helpers
{
    public static string ToLocalizedString(this Enum value)
    {
        return value.ToLocalizedString(null, false);
    }

    public static string ToLocalizedString(this Enum value, string? className, bool throwIfNotFound = false)
    {
        // Build up the key to find in the resource (translation) file.
        var enumValueString = value.ToString();
        var prefix = string.IsNullOrEmpty(className) ? string.Empty : $"{className}_";
        var enumNameAndValue = $"{value.GetType().Name}_{enumValueString}";
        var resourceKey = $"{prefix}{enumNameAndValue}";

        // Get the translated text for the enum value.
        var result = Translations.ResourceManager.GetString(resourceKey);
        if (result != null)
        {
            return result;
        }

        if (throwIfNotFound)
        {
            throw new KeyNotFoundException($"Resource key {resourceKey} is not found.");
        }

        return $"[[{enumValueString}]]";
    }
}