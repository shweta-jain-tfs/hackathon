namespace Pharos.Server.Infrastructure.Process;

public interface IStartedProcess : IDisposable
{
    int Id { get; }
    Task WaitForExitAsync(CancellationToken cancellationToken = default);
    public int ExitCode { get; }
}
