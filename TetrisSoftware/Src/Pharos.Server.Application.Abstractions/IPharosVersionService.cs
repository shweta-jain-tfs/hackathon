namespace Pharos.Server.Application;

public interface IPharosVersionService
{
    string Version { get; }

    DateTimeOffset StartedAt { get; }
}