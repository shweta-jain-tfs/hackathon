using Pharos.Client.Common.App.Interfaces;

namespace Pharos.Client.Common.App.Services;

public class TimeCalculator : ITimeCalculator
{
    private readonly IStringLocalizer<Translations> _localization;

    public TimeCalculator(IStringLocalizer<Translations> localization)
    {
        _localization = localization;
    }

    public string? GetDifference(DateTimeOffset now, DateTimeOffset? expiresAt)
    {
        if (!expiresAt.HasValue)
        {
            return null;
        }

        if (expiresAt.Value < now)
        {
            return null;
        }

        var timeSpan = expiresAt.Value - now;

        if (timeSpan.Days > 0)
        {
            return Format(timeSpan.Days, "TaskCard_ShortTimeUnit_Day");
        }

        if (timeSpan.Hours > 0)
        {
            return Format(timeSpan.Hours, "TaskCard_ShortTimeUnit_Hour");
        }

        if (timeSpan.Minutes > 0)
        {
            return Format(timeSpan.Minutes, "TaskCard_ShortTimeUnit_Minute");
        }

        return null;
    }

    private string Format(int value, string unitKey)
    {
        return $"{value} {_localization[unitKey]}";
    }
}
