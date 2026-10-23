using Pharos.Client.Common.App.Interfaces;

namespace Pharos.Client.Common.App.Services;

public class DateTimeService : IDateTime
{
    public DateTimeOffset Now => DateTimeOffset.Now;
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}