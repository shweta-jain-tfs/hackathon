
namespace Pharos.Server.Infrastructure.Process;

using System.Diagnostics;

internal class StartedProcess : IStartedProcess
{
    private readonly Process _process;

    public StartedProcess(Process process)
    {
        _process = process;
    }

    public int Id => _process.Id;

    public Task WaitForExitAsync(CancellationToken cancellationToken = default)
    {
        return _process.WaitForExitAsync(cancellationToken);
    }

    public int ExitCode => _process.ExitCode;

    public void Dispose()
    {
        _process.Dispose();
    }
}
