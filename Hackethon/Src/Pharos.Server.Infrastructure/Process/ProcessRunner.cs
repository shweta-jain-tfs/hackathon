
namespace Pharos.Server.Infrastructure.Process;

using System.Diagnostics;

internal class ProcessRunner : IProcessRunner
{
    public IStartedProcess? Start(string fileName, string arguments)
    {
        Process? process = null;
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments
            };

            process = Process.Start(startInfo);
            if (process is null)
            {
                return null;
            }

            return new StartedProcess(process);
        }
        catch
        {
            process?.Dispose();
            throw;
        }
    }
}
