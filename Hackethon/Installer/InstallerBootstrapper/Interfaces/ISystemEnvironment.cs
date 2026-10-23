using System;

namespace InstallerBootstrapper;

public interface ISystemEnvironment
{
    bool IsAdministrator();
    string BaseDirectory { get; }
    DateTime UtcNow { get; }
    DateTime NowLocal { get; }
}