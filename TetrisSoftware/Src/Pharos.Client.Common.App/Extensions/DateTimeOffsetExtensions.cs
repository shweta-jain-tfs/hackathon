using System.Globalization;
using Pharos.Client.Common.App.Constants;

namespace Pharos.Client.Common.App.Extensions;
public static class DateTimeOffsetExtensions
{
    /// <summary>
    /// Converts a DateTimeOffset to a short date and short time string using the current culture.
    /// </summary>
    /// <param name="dateTime">The DateTimeOffset to convert.</param>
    /// <returns>A short date and short time string.</returns>
    public static string ToShortDateTimeString(this DateTimeOffset dateTime)
    {
        return dateTime.ToLocalTime().ToString(FormatStrings.ShortDateTime, CultureInfo.CurrentCulture);
    }

    /// <summary>
    /// Converts a DateTimeOffset to a short date string using the current culture.
    /// </summary>
    /// <param name="dateTime">The DateTimeOffset to convert.</param>
    /// <returns>A short date and short time string.</returns>
    public static string ToShortDateString(this DateTimeOffset dateTime)
    {
        return dateTime.ToLocalTime().ToString(FormatStrings.ShortDate, CultureInfo.CurrentCulture);
    }

    /// <summary>
    /// Converts a DateTimeOffset to a short time string using the current culture.
    /// </summary>
    /// <param name="dateTime">The DateTimeOffset to convert.</param>
    /// <returns>A short date and short time string.</returns>
    public static string ToShortTimeString(this DateTimeOffset dateTime)
    {
        return dateTime.ToLocalTime().ToString(FormatStrings.ShortTime, CultureInfo.CurrentCulture);
    }

    /// <summary>
    /// Converts a DateTimeOffset to a sortable date and time string using the current culture.
    /// </summary>
    /// <param name="dateTime">The DateTimeOffset to convert.</param>
    /// <returns>A sortable date and time string.</returns>
    public static string ToSortableDateTimeString(this DateTimeOffset dateTime)
    {
        return dateTime.ToLocalTime().ToString(FormatStrings.SortableDateTime, CultureInfo.CurrentCulture);
    }
}