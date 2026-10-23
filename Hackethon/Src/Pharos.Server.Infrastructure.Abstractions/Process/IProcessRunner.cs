namespace Pharos.Server.Infrastructure.Process;

public interface IProcessRunner
{
    IStartedProcess? Start(string fileName, string arguments);
}
