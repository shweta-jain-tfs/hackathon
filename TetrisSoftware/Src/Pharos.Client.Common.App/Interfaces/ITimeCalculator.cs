namespace Pharos.Client.Common.App.Interfaces;

public interface ITimeCalculator
{
    string? GetDifference(DateTimeOffset now, DateTimeOffset? expiresAt);
}
