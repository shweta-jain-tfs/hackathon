namespace InstallerBootstrapper;

public interface IProcessRunner
{
    IProcessHandle Start(string fileName, string arguments);
}