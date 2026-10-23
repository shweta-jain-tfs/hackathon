using System.Globalization;

namespace Pharos.Server.Parameters.Application.Services;

internal static class ParameterStreamWriterExtensions
{
    public static void AddParameter(this StreamWriter writer, string key, string? value, bool newLine = true)
    {
        if (newLine)
        {
            writer.WriteLine($"{key}={value}");
        }
        else
        {
            writer.Write($"{key}={value}");
        }
    }

    public static void AddParameter(this StreamWriter writer, string key, bool value, bool newLine = true)
    {
        var stringValue = Convert(value);

        AddParameter(writer, key, stringValue, newLine);
    }

    public static void AddParameter(this StreamWriter writer, string key, double? value, int nrDecimals, bool newLine = true)
    {
        var formattedValue = "";

        if (value.HasValue)
        {
            formattedValue = FormatDouble(value, nrDecimals);
        }

        AddParameter(writer, key, formattedValue, newLine);
    }
    public static void AddParameter(this StreamWriter writer, string key, int? value, bool newLine = true)
    {
        var stringValue = "";

        if (value.HasValue)
        {
            stringValue = value.Value.ToString(CultureInfo.InvariantCulture);
        }

        AddParameter(writer, key, stringValue, newLine);
    }
    public static void AddParameter(this StreamWriter writer, string value)
    {
        writer.WriteLine($"{value}");
    }

    public static void AddSection(this StreamWriter writer, string section)
    {
        writer.WriteLine($"[{section}]");
    }

    public static void AddParameterAtPos(this StreamWriter writer, string? value, bool newLine = false)
    {
        if (newLine)
        {
            writer.WriteLine($";{value}");
        }
        else
        {
            writer.Write($";{value}");
        }
    }

    public static void AddParameterAtPos(this StreamWriter writer, int? value, bool newLine = false)
    {
        var stringValue = "";

        if (value.HasValue)
        {
            stringValue = value.Value.ToString(CultureInfo.InvariantCulture);
        }
        AddParameterAtPos(writer, stringValue, newLine);
    }

    public static void AddParameterAtPos(this StreamWriter writer, bool? value, bool newLine = false)
    {
        var stringValue = "";

        if (value.HasValue)
        {
            stringValue = Convert(value.Value);
        }

        AddParameterAtPos(writer, stringValue, newLine);
    }
    public static void AddParameterAtPos(this StreamWriter writer, double? value, int nrDecimals, bool newLine = false)
    {
        var formattedValue = "";

        if (value.HasValue)
        {
            formattedValue = FormatDouble(value, nrDecimals);
        }
        AddParameterAtPos(writer, formattedValue, newLine);
    }

    public static string FormatDouble(double? value, int nrDecimals)
    {
        var result = "";
        if (!value.HasValue)
        {
            return result;
        }

        var formatter = $"0.{new string('0', nrDecimals)}";
        result = value.Value.ToString(formatter, CultureInfo.InvariantCulture);
        return result;
    }
    public static string Convert(bool value)
    {
        var result = value ? 1 : 0;
        return result.ToString(CultureInfo.InvariantCulture);
    }
}