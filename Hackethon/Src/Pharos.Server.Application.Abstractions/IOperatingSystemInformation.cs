namespace Pharos.Server.Application;

public interface IOperatingSystemInformation
{
    string Version { get; }

    string FriendlyName { get; }
}
