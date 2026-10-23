using System.Globalization;
using Google.Protobuf.WellKnownTypes;

namespace Pharos.Server.Result.Flr.Application.Parsers;

internal static class FlrEntryValueParser
{
    public static string GetString(this string[] columnValues, IReadOnlyDictionary<string, FlrEntryColumn> columns, string columnName, bool canBeEmpty = true, string defaultValue = "")
    {
        if (columns.TryGetValue(columnName, out var colInfo))
        {
            var result = columnValues[colInfo.Index];
            if (string.IsNullOrWhiteSpace(result) && !canBeEmpty)
            {
                throw new FormatException($"{columnName} column must have a value.");
            }

            return result;
        }

        if (canBeEmpty)
        {
            return defaultValue;
        }

        // This indicates a problem either in the calling code or the FLR file spec (e.g., a field must be essential).
        throw new ArgumentException($"{columnName} is missing from the FLR file as non-essential, but is required to have non-empty value.");
    }

    public static int GetInt(this string[] columnValues, IReadOnlyDictionary<string, FlrEntryColumn> columns, string columnName, bool canBeEmpty = true, int defaultValue = 0)
    {
        var i = columnValues.TryGetInt(columns, columnName);
        if (i == null && !canBeEmpty)
        {
            throw new FormatException($"{columnName} column must have a value.");
        }
        return i ?? defaultValue;
    }

    public static int? TryGetInt(this string[] columnValues, IReadOnlyDictionary<string, FlrEntryColumn> columns, string columnName, int? defaultValue = null)
    {
        int? result = null;

        var s = columnValues.GetString(columns, columnName);
        if (int.TryParse(s, out var value))
        {
            result = value;
        }

        result ??= defaultValue;

        return result;
    }

    public static double GetDouble(this string[] columnValues, IReadOnlyDictionary<string, FlrEntryColumn> columns, string columnName, double defaultValue = 0.0)
    {
        var s = columnValues.GetString(columns, columnName);
        s = s.Replace(',', '.');
        return double.TryParse(s, CultureInfo.InvariantCulture, out var result) ? result : defaultValue;
    }

    public static bool GetBool(this string[] columnValues, IReadOnlyDictionary<string, FlrEntryColumn> columns, string columnName, bool defaultValue = false)
    {
        var s = columnValues.GetString(columns, columnName);
        if (string.IsNullOrEmpty(s))
        {
            return defaultValue;
        }

        // New format: 0/1, old format: True/False
        s = s.ToUpper();
        return s switch
        {
            "TRUE" or "1" => true,
            _ => false
        };
    }

    public static Timestamp? GetTimestamp(this string[] columnValues, IReadOnlyDictionary<string, FlrEntryColumn> columns, string columnName)
    {
        var s = columnValues.GetString(columns, columnName);
        if (string.IsNullOrEmpty(s))
        {
            return null;
        }
        return DateTimeOffset.Parse(s, CultureInfo.InvariantCulture).ToUniversalTime().ToTimestamp();
    }

    public static (int Row, int Spoke) GetWheelPosition(this string[] columnValues, IReadOnlyDictionary<string, FlrEntryColumn> columns, string columnName, int maxRow, int maxSpoke)
    {
        var position = columnValues.GetString(columns, columnName);
        if (string.IsNullOrWhiteSpace(position))
        {
            return (0, 0);
        }

        var parts = position.Split('_', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var wheelPosition = parts.Length switch
        {
            1 => parts[0],
            2 when parts[0].Equals("ERW", StringComparison.OrdinalIgnoreCase) || parts[0].Equals("IRW", StringComparison.OrdinalIgnoreCase) => parts[1],
            _ => throw new FormatException($"Invalid wheel position format: '{position}'. Expected formats: <IRW|ERW>_<RowLetter><SpokeNumber> or <RowLetter><SpokeNumber>.")
        };

        if (wheelPosition.Length < 2)
        {
            throw new FormatException($"Invalid wheel position format: '{position}'. Expected formats: <IRW|ERW>_<RowLetter><SpokeNumber> or <RowLetter><SpokeNumber>.");
        }

        var rowLetter = char.ToUpperInvariant(wheelPosition[0]);
        var row = rowLetter - 'A' + 1;
        if (row < 1 || row > maxRow)
        {
            throw new FormatException($"Invalid wheel row in position '{position}'. Row must be between A and {(char)('A' + maxRow - 1)}.");
        }

        if (!int.TryParse(wheelPosition[1..], out var spoke) || spoke < 1 || spoke > maxSpoke)
        {
            throw new FormatException($"Invalid wheel spoke in position '{position}'. Spoke must be between 1 and {maxSpoke}.");
        }

        return (row, spoke);
    }

    public static int GetCartridgeCell(this string[] columnValues, IReadOnlyDictionary<string, FlrEntryColumn> columns, string columnName, bool canBeEmpty = true, int defaultValue = 1)
    {
        var location = columnValues.GetString(columns, columnName);
        if (string.IsNullOrWhiteSpace(location))
        {
            if (canBeEmpty)
            {
                return defaultValue;
            }

            throw new FormatException($"{columnName} column must have a value.");
        }

        var locationParts = location.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var cartridgePart = locationParts.Length > 0 ? locationParts[^1] : location;

        var separatorIndex = cartridgePart.LastIndexOf('_');
        if (separatorIndex < 0 || separatorIndex == cartridgePart.Length - 1)
        {
            throw new FormatException($"Invalid cartridge location format: '{location}'. Expected format containing cartridge cell information such as 'C1_1'.");
        }

        if (!int.TryParse(cartridgePart[(separatorIndex + 1)..], out var cell) || cell < 1)
        {
            throw new FormatException($"Invalid cartridge cell in location '{location}'. Cell must be a positive integer.");
        }

        return cell;
    }
}
