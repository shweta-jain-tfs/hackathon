using System.Globalization;

namespace Pharos.Client.Common.App.Extensions;

/// <summary>
/// Extension methods to show localized strings for <see cref="System.DayOfWeek"/> and <see cref="Models.DayOfWeek"/>.
/// </summary>
public static class DayOfWeekExtensions
{
    #region System day of week

    /// <summary>
    /// Get the name of the day in the current culture
    /// </summary>
    public static string GetDayName(this System.DayOfWeek value, bool titleCase = true)
    {
        var dayName = DateTimeFormatInfo.CurrentInfo.GetDayName(value);
        if (titleCase)
        {
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(dayName);
        }

        return dayName;
    }

    /// <summary>
    /// Get the abbreviated name of the day in the current culture
    /// </summary>
    public static string GetAbbreviatedDayName(this System.DayOfWeek value, bool titleCase = true)
    {
        var dayName = DateTimeFormatInfo.CurrentInfo.GetAbbreviatedDayName(value);

        if (titleCase)
        {
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(dayName);
        }

        return dayName;
    }

    #endregion

    #region Pharos day of week enum

    /// <summary>
    /// Convert <see cref="Models.DayOfWeek"/> enum, used in the Pharos API, to a day of week to use for localization.
    /// </summary>
    public static System.DayOfWeek ToDay(this Models.DayOfWeek dayOfWeekEnum)
    {
        return (System.DayOfWeek)dayOfWeekEnum;
    }

    /// <summary>
    /// Get the name of the day enum in the current culture
    /// </summary>
    public static string GetDayName(this Models.DayOfWeek dayOfWeekEnum)
    {
        return dayOfWeekEnum.ToDay().GetDayName();
    }

    /// <summary>
    /// Get the abbreviated name of the day enum in the current culture
    /// </summary>
    public static string GetAbbreviatedDayName(this Models.DayOfWeek dayOfWeekEnum)
    {
        return dayOfWeekEnum.ToDay().GetAbbreviatedDayName();
    }

    #endregion
}