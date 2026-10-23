namespace Pharos.Client.Common.App.Interfaces;

public interface IDateTime
{
    DateTimeOffset Now { get; }
    DateTimeOffset UtcNow { get; }
}