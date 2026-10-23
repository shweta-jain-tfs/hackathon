namespace Pharos.Server.Application.Settings.General;

/// <summary>
/// Provides timezone options from the operating system
/// </summary>
public class TimeZoneDropdownProvider : IDropdownValueProvider
{
    public Task<List<DropdownOption>> GetOptionsAsync(CancellationToken cancellationToken = default)
    {
        var timeZones = TimeZoneInfo.GetSystemTimeZones()
            .Select(tz => new DropdownOption
            {
                Value = tz.Id,
                DisplayName = tz.DisplayName
            })
            .ToList();

        return Task.FromResult(timeZones);
    }
}
