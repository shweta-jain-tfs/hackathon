using System.Globalization;
using System.Text.RegularExpressions;

namespace Pharos.Client.Common.App.Extensions;

public static partial class DecimalStringExtensions
{
    private static readonly Regex _decimalSeparatorRegex = DecimalSeparatorRegex();

    /// <summary>
    /// Replaces the first ',' or '.' decimal separator in a persisted decimal string with the current culture's decimal separator.
    /// </summary>
    public static string FormatDecimalForDisplay(this string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var separator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;

        return _decimalSeparatorRegex.Replace(value, separator, 1);
    }

    [GeneratedRegex("[.,]")]
    private static partial Regex DecimalSeparatorRegex();
}