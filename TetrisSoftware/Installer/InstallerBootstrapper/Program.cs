using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Windows.Forms;

namespace InstallerBootstrapper
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            var runner = new InstallerBootstrapperRunner(
                new SystemEnvironment(),
                new FileSystem(),
                new ProcessRunner(),
                new UserNotifier());

            return runner.Execute(args);
        }

        #region Real dependency implementations

        private sealed class ProcessHandle : IProcessHandle
        {
            private readonly Process _proc;
            public ProcessHandle(Process proc) => _proc = proc;
            public int ExitCode => _proc.ExitCode;
            public void WaitForExit() => _proc.WaitForExit();
        }

        private sealed class ProcessRunner : IProcessRunner
        {
            public IProcessHandle Start(string fileName, string arguments)
            {
                var proc = Process.Start(new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false
                });
                return proc == null ? null : new ProcessHandle(proc);
            }
        }

        private sealed class SystemEnvironment : ISystemEnvironment
        {
            public bool IsAdministrator()
            {
                try
                {
                    using (var identity = WindowsIdentity.GetCurrent())
                    {
                        var principal = new WindowsPrincipal(identity);
                        return principal.IsInRole(WindowsBuiltInRole.Administrator);
                    }
                }
                catch
                {
                    return false;
                }
            }

            public string BaseDirectory => AppDomain.CurrentDomain.BaseDirectory;
            public DateTime UtcNow => DateTime.UtcNow;
            public DateTime NowLocal => DateTime.Now;
        }

        private sealed class FileSystem : IFileSystem
        {
            public void CreateDirectory(string path) => Directory.CreateDirectory(path);
            public bool DirectoryExists(string path) => Directory.Exists(path);
            public string[] GetFiles(string path, string searchPattern) =>
                Directory.GetFiles(path, searchPattern, SearchOption.TopDirectoryOnly);
            public DateTime GetLastWriteUtc(string path) => new FileInfo(path).LastWriteTimeUtc;
        }

        private sealed class UserNotifier : IUserNotifier
        {
            public void ShowMessage(string message, string caption)
            {
                try
                {
                    MessageBox.Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch
                {
                    // ignore (non-interactive session)
                }
            }

            public void WriteLine(string line) => Console.WriteLine(line);
        }

        #endregion
    }
}