namespace Pharos.Server.Application;

public interface ITetrisProtocolInformation
{
    Version Version { get; }

    string VersionString { get; }
}