using System.Diagnostics;
using CustomAction.Wrappers.Interfaces;

namespace CustomAction.Wrappers;

public class ProcessRunner : IProcessRunner
{
    public int Run(string fileName, string arguments, string workingDirectory)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            return 1;
        }

        process.WaitForExit();
        return process.ExitCode;
    }
}