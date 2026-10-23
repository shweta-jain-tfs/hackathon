using System.Globalization;

namespace Pharos.Server.Settings.Infrastructure.ConfigurationProvider;

/// <summary>
/// Provides common value formatting functionality for settings providers.
/// </summary>
internal static class ValueFormattingHelper
{
    /// <summary>
    /// Converts a value to its string representation.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The string representation of the value, or null if the value is null.</returns>
    public static string? ValueToString(object? value)
    {
        if (value == null)
        {
            return null;
        }

        if (value is bool boolValue)
        {
            return boolValue.ToString(CultureInfo.InvariantCulture);
        }

        if (value is int numberInt)
        {
            return numberInt.ToString(CultureInfo.InvariantCulture);
        }

        if (value is double numberDouble)
        {
            return numberDouble.ToString(CultureInfo.InvariantCulture);
        }

        if (value is DateTimeOffset offset)
        {
            return offset.ToString(DefaultSettingsProvider.DateFormat, CultureInfo.InvariantCulture);
        }

        if (value.GetType() == typeof(DateTimeOffset?))
        {
            return ((DateTimeOffset?)value).Value.ToString(DefaultSettingsProvider.DateFormat, CultureInfo.InvariantCulture);
        }

        if (value is Enum enumValue)
        {
            return enumValue.ToString();
        }

        if (value is TimeOnly timeOnly)
        {
            return timeOnly.ToString(DefaultSettingsProvider.TimeFormat, CultureInfo.InvariantCulture);
        }

        return value.ToString();
    }
}