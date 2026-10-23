using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Settings.Infrastructure.ConfigurationProvider;
internal class ValueValidator
{
    public static bool IsValidValue(PropertyInfo? property, object? value)
    {
        if (value == null)
        {
            return true;
        }

        if (value is bool boolValue)
        {
            return true;
        }

        else if (value is DateTimeOffset offset)
        {
            return true;
        }
        else if (value is Enum enumValue)
        {
            return true;
        }
        else if (property?.PropertyType == typeof(int) || property?.PropertyType == typeof(int?))
        {
            var optionValue = ValueToString(value);
            var minValue = (property.GetCustomAttribute<SettingsRangeAttribute>()?.Min as int? ?? int.MinValue);
            var maxValue = (property.GetCustomAttribute<SettingsRangeAttribute>()?.Max as int? ?? int.MaxValue);
            if (int.TryParse(optionValue, CultureInfo.InvariantCulture, out var parsedValue))
            {
                if (parsedValue < minValue || parsedValue > maxValue)
                {
                    return false;
                }
                return true;
            }
            else
            {
                return false;
            }
        }
        else if (property?.PropertyType == typeof(double) || property?.PropertyType == typeof(double?))
        {
            var optionValue = ValueToString(value);
            var minValue = (property.GetCustomAttribute<SettingsRangeAttribute>()?.Min as double? ?? double.MinValue);
            var maxValue = (property.GetCustomAttribute<SettingsRangeAttribute>()?.Max as double? ?? double.MaxValue);
            if (double.TryParse(optionValue, CultureInfo.InvariantCulture, out var parsedValue))
            {
                if (parsedValue < minValue || parsedValue > maxValue)
                {
                    return false;
                }
                return true;
            }
            else
            {
                return false;
            }
        }
        else if (property?.PropertyType == typeof(string))
        {
            // If property is defined as a DynamicDropdown, it is changed to Choice type option
            if (property.CustomAttributes.Any(x => x.AttributeType == typeof(DynamicDropdown)))
            {
                return true;
            }
            else
            {
                var optionValue = ValueToString(value) ?? "";
                var maxLength = property.GetCustomAttribute<MaxLengthAttribute>()?.Length ?? int.MaxValue;
                if (optionValue.Length > maxLength)
                {
                    return false;
                }

                var disallowedCharactersPattern = property.GetCustomAttribute<DisallowedCharactersAttribute>()?.Pattern;
                if (!string.IsNullOrEmpty(optionValue)
                    && !string.IsNullOrEmpty(disallowedCharactersPattern)
                    && Regex.IsMatch(optionValue, disallowedCharactersPattern))
                {
                    return false;
                }

                if (property.GetCustomAttribute<AbsoluteDirectoryPathAttribute>() != null)
                {
                    return IsValidAbsoluteDirectoryPath(optionValue);
                }

                return true;
            }
        }
        else if (property?.PropertyType == typeof(DateTimeOffset?))
        {
            return true;
        }
        else if (property?.PropertyType == typeof(TimeOnly))
        {
            return true;
        }
        else
        {
            throw new ArgumentException($"Type [{property?.PropertyType}] of value [{value}] is not supported");
        }

    }

    internal static bool IsValidAbsoluteDirectoryPath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return true;
        }

        var expanded = Environment.ExpandEnvironmentVariables(path);
        return Path.IsPathFullyQualified(expanded);
    }

    public static string? ValueToString(object? value)
    {
        return ValueFormattingHelper.ValueToString(value);
    }
}
