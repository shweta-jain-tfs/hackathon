namespace InstallerBootstrapper;

public interface IProcessHandle
{
    int ExitCode { get; }
    void WaitForExit();
}