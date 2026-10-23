namespace Pharos.Server.Infrastructure;

public interface IDateTime
{
    DateTimeOffset Now { get; }

    DateTimeOffset UtcNow { get; }
}