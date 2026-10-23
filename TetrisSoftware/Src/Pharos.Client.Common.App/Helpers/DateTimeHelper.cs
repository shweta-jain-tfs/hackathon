namespace Pharos.Client.Common.App.Helpers;

public static class DateTimeHelper
{
    /// <summary>
    /// Calculate time left to next state.
    /// </summary>
    /// <param name="dateTimeOffset"></param>
    /// <returns></returns>
    public static string GetTimeLeft(DateTimeOffset dateTimeOffset)
    {
        var localTime = DateTimeOffset.Now;
        var timeLeftToShow = dateTimeOffset.ToLocalTime();
        var timeLeft = timeLeftToShow - localTime;

        // Make sure we don't have negative time.
        timeLeft = timeLeft.TotalSeconds < 0 ? TimeSpan.Zero : timeLeft;

        return $"{FormatTimeLeft(timeLeft)}";
    }

    /// <summary>
    /// Format time left.
    /// </summary>
    /// <param name="timeLeft"></param>
    /// <returns></returns>
    private static string FormatTimeLeft(TimeSpan timeLeft)
    {
        // Calculate hours and minutes left
        var hours = (int)timeLeft.TotalHours;
        var minutes = timeLeft.Minutes;

        // Build the formatted string
        return hours > 0 ? $"{hours} h {minutes} min" : $"{minutes} min";
    }

    /// <summary>
    /// Get time left in minutes and seconds for display (e.g., "05 min 30 s").
    /// </summary>
    /// <param name="dateTimeOffset"></param>
    /// <returns></returns>
    public static string GetTimeLeftMinutesSeconds(DateTimeOffset dateTimeOffset)
    {
        var timeLeft = dateTimeOffset - DateTimeOffset.Now;
        timeLeft = timeLeft.TotalSeconds < 0 ? TimeSpan.Zero : timeLeft;

        var totalMinutes = (int)timeLeft.TotalMinutes;
        var seconds = timeLeft.Seconds;

        if (totalMinutes == 0)
        {
            return $"{seconds:D2} s";
        }

        return $"{totalMinutes:D2} min {seconds:D2} s";
    }
}