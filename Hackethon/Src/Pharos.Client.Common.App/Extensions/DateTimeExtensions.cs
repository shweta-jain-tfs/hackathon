using System.Globalization;
using Pharos.Client.Common.App.Constants;

namespace Pharos.Client.Common.App.Extensions;
public static class DateTimeExtensions
{
    /// <summary>
    /// Converts a DateTime to a short date and short time string using the current culture.
    /// </summary>
    /// <param name="dateTime">The DateTime to convert.</param>
    /// <returns>A short date and short time string.</returns>
    public static string ToShortDateTimeString(this DateTime dateTime)
    {
        return dateTime.ToLocalTime().ToString(FormatStrings.ShortDateTime, CultureInfo.CurrentCulture);
    }
}
