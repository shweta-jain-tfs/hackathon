using Pharos.Client.Common.App.Extensions;

namespace Pharos.Client.Common.App.Helpers;

/// <summary>
/// Display data for lock screen time information.
/// Contains time components (hours/minutes) with units and secondary information.
/// </summary>
public sealed record LockScreenTimeDisplay(
    int? Hours,
    string? HoursUnit,
    int? Minutes,
    string? MinutesUnit,
    string? SecondaryInfo);

/// <summary>
/// Formats time information for lock screen display.
/// Handles various time ranges and edge cases.
/// </summary>
public static class LockScreenTimeFormatter
{
    /// <summary>
    /// Formats a target time into display components for lock screen.
    /// </summary>
    /// <param name="targetTime">The target time to display.</param>
    /// <param name="loc">String localizer for translations.</param>
    /// <returns>Time display data with hours, minutes, units, and secondary info.</returns>
    public static LockScreenTimeDisplay FormatTime(
        DateTimeOffset targetTime,
        IStringLocalizer<Translations> loc)
    {
        var remaining = targetTime - DateTimeOffset.Now;

        // Edge case 1: Time in past or zero
        if (remaining.TotalSeconds <= 0)
        {
            return CompletedDisplay(loc);
        }

        // Edge case 2: Less than 1 minute
        if (remaining.TotalMinutes < 1)
        {
            return LessThanOneMinuteDisplay(loc, targetTime);
        }

        // Standard case 1: 1-23 hours - show hours and minutes
        if (remaining.TotalHours >= 1)
        {
            return HoursMinutesDisplay(remaining, loc, targetTime);
        }

        // Standard case 2: 1-59 minutes - show minutes only
        return MinutesOnlyDisplay(remaining, loc, targetTime);
    }

    /// <summary>
    /// Returns display for completed tests (no time remaining).
    /// </summary>
    private static LockScreenTimeDisplay CompletedDisplay(IStringLocalizer<Translations> loc)
    {
        return new LockScreenTimeDisplay(
            Hours: null,
            HoursUnit: null,
            Minutes: null,
            MinutesUnit: null,
            SecondaryInfo: loc["LockScreen_TestsFinished"].Value);
    }

    /// <summary>
    /// Returns display for less than 1 minute remaining.
    /// </summary>
    private static LockScreenTimeDisplay LessThanOneMinuteDisplay(
        IStringLocalizer<Translations> loc,
        DateTimeOffset targetTime)
    {
        return new LockScreenTimeDisplay(
            Hours: null,
            HoursUnit: null,
            Minutes: 0,
            MinutesUnit: loc["LockScreen_Time_LessThanOneMin"].Value,
            SecondaryInfo: loc["LockScreen_EndsAt", targetTime.ToShortTimeString()].Value);
    }


    /// <summary>
    /// Returns display for 1-23 hours remaining (shows hours and minutes).
    /// </summary>
    private static LockScreenTimeDisplay HoursMinutesDisplay(
        TimeSpan remaining,
        IStringLocalizer<Translations> loc,
        DateTimeOffset targetTime)
    {
        var hours = (int)remaining.TotalHours;
        var minutes = remaining.Minutes;
        var timeLeft = DateTimeHelper.GetTimeLeft(targetTime);

        return new LockScreenTimeDisplay(
            Hours: hours,
            HoursUnit: loc["LockScreen_Time_Hours_Short"].Value,
            Minutes: minutes,
            MinutesUnit: loc["LockScreen_Time_Minutes_Short"].Value,
            SecondaryInfo: loc["LockScreen_EndsAt", targetTime.ToShortTimeString(), timeLeft].Value);
    }

    /// <summary>
    /// Returns display for 1-59 minutes remaining (shows minutes only).
    /// </summary>
    private static LockScreenTimeDisplay MinutesOnlyDisplay(
        TimeSpan remaining,
        IStringLocalizer<Translations> loc,
        DateTimeOffset targetTime)
    {
        var minutes = (int)remaining.TotalMinutes;
        var timeLeft = DateTimeHelper.GetTimeLeft(targetTime);

        return new LockScreenTimeDisplay(
            Hours: null,
            HoursUnit: null,
            Minutes: minutes,
            MinutesUnit: loc["LockScreen_Time_Minutes_Short"].Value,
            SecondaryInfo: loc["LockScreen_EndsAt", targetTime.ToShortTimeString(), timeLeft].Value);
    }
}
