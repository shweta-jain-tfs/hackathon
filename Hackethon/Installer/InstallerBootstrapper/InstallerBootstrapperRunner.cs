using System;
using System.Linq;

namespace InstallerBootstrapper
{
    public class InstallerBootstrapperRunner
    {
        public const string LogFolder = @"C:\ProgramData\Thermo Fisher Scientific\Pharos\InstallationLogs";
        public const string PharosLogsFolder = @"C:\ProgramData\Thermo Fisher Scientific\Pharos\Logs";

        private readonly ISystemEnvironment _env;
        private readonly IFileSystem _fs;
        private readonly IProcessRunner _proc;
        private readonly IUserNotifier _ui;

        public InstallerBootstrapperRunner(
            ISystemEnvironment env,
            IFileSystem fs,
            IProcessRunner proc,
            IUserNotifier ui)
        {
            _env = env;
            _fs = fs;
            _proc = proc;
            _ui = ui;
        }

        public int Execute(string[] args)
        {
            if (!_env.IsAdministrator())
            {
                ShowFailure("This installer must be run with Administrator privileges.\r\n\r\nRight‑click the executable and choose 'Run as administrator', or start it from an elevated command prompt.");
                return 1;
            }

            var options = ParseArgs(args);

            if (options.ShowHelp)
            {
                PrintUsage();
                return 0;
            }
            if (options.ParseError)
            {
                PrintUsage();
                return 2;
            }

            var msiPath = FindFirstMsi();
            if (msiPath == null)
            {
                ShowFailure("No .msi found in the executable directory.");
                return 3;
            }

            string logPath = null;

            try
            {
                _fs.CreateDirectory(LogFolder);
                var timestamp = _env.NowLocal.ToString("yyyyMMdd_HHmmss");
                logPath = System.IO.Path.Combine(LogFolder, $"Pharos-install-{timestamp}.log");

                var msiexecArgs = BuildMsiexecArgs(msiPath, logPath, options);
                _ui.WriteLine("Launching msiexec...");
                _ui.WriteLine(msiexecArgs);
                _ui.WriteLine(string.Empty);

                var handle = _proc.Start("msiexec.exe", msiexecArgs);
                if (handle == null)
                {
                    ShowFailure("Failed to start msiexec.");
                    return 4;
                }

                handle.WaitForExit();

                if (handle.ExitCode == 0)
                {
                    _ui.WriteLine("Installation succeeded.");
                    _ui.ShowMessage("Pharos installation completed successfully.", "Pharos Installation Succeeded");
                }
                else if (options.TestRollback)
                {
                    _ui.WriteLine($"Installation failed as expected in rollback test mode. Exit code {handle.ExitCode}. Log: {logPath}");
                }
                else
                {
                    ShowDetailedFailureInfo(logPath, null);
                }

                return handle.ExitCode;
            }
            catch (Exception ex)
            {
                _ui.WriteLine("Fatal error: " + ex.Message);
                ShowDetailedFailureInfo(logPath ?? "Unknown", ex.Message);
                return 100;
            }
        }

        internal string BuildMsiexecArgs(string msiPath, string logPath, BootstrapperOptions o)
        {
            var args =
                $"/i \"{msiPath}\" " +
                "/passive " +
                $"/lewimo! \"{logPath}\" " +
                $"secureenclavesenabled={o.SecureEnclavesEnabled.ToString().ToLowerInvariant()} ";

            if (o.TestRollback)
            {
                args += "testrollback=true ";
            }

            return args.TrimEnd();
        }

        internal BootstrapperOptions ParseArgs(System.Collections.Generic.IEnumerable<string> args)
        {
            var opt = new BootstrapperOptions();

            foreach (var raw in args ?? Array.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(raw))
                    continue;

                var token = raw.Trim();

                if (IsHelpToken(token))
                {
                    opt.ShowHelp = true;
                    return opt;
                }

                var eq = token.IndexOf('=');
                if (eq <= 0 || eq == token.Length - 1)
                {
                    _ui.WriteLine($"Invalid argument format: {raw}");
                    opt.ParseError = true;
                    return opt;
                }

                var key = token.Substring(0, eq).Trim().ToLowerInvariant();
                var value = token.Substring(eq + 1).Trim();

                switch (key)
                {
                    case "secureenclavesenabled":
                        if (!bool.TryParse(value, out var sen))
                        {
                            _ui.WriteLine("secureenclavesenabled must be true or false.");
                            opt.ParseError = true;
                            return opt;
                        }
                        opt.SecureEnclavesEnabled = sen;
                        break;
                    case "testrollback":
                        if (!bool.TryParse(value, out var tr))
                        {
                            _ui.WriteLine("testrollback must be true or false.");
                            opt.ParseError = true;
                            return opt;
                        }
                        opt.TestRollback = tr;
                        break;
                    default:
                        _ui.WriteLine($"Unknown argument key: {key}");
                        opt.ParseError = true;
                        return opt;
                }
            }

            return opt;
        }

        private bool IsHelpToken(string token) =>
            token.Equals("help", StringComparison.OrdinalIgnoreCase) ||
            token.Equals("/?", StringComparison.OrdinalIgnoreCase) ||
            token.Equals("--help", StringComparison.OrdinalIgnoreCase);

        private string FindFirstMsi()
        {
            var files = _fs.GetFiles(_env.BaseDirectory, "*.msi");
            return files.FirstOrDefault();
        }

        private void ShowDetailedFailureInfo(string installerLogPath, string extraError)
        {
            var latestPharosLog = GetLatestPharosLogFile();
            var pharosLogLine = latestPharosLog != null
                ? $"Latest Pharos log:\r\n  {latestPharosLog}"
                : $"No Pharos log file detected. Check folder:\r\n  {PharosLogsFolder}";

            var msg =
                "Pharos installation failed.\r\n\r\n" +
                (extraError != null ? $"Error: {extraError}\r\n\r\n" : "") +
                "Review logs to diagnose the issue:\r\n\r\n" +
                $"Installer log:\r\n  {installerLogPath}\r\n\r\n" +
                pharosLogLine + "\r\n\r\n" +
                "Installation can also fail if the Pharos service / application failed to start properly.\r\n";

            ShowFailure(msg, "Pharos Installation Failed");
        }

        private string GetLatestPharosLogFile()
        {
            try
            {
                if (!_fs.DirectoryExists(PharosLogsFolder))
                    return null;

                var files = _fs.GetFiles(PharosLogsFolder, "*.txt");
                if (files.Length == 0)
                    return null;

                return files
                    .OrderByDescending(f => _fs.GetLastWriteUtc(f))
                    .First();
            }
            catch
            {
                return null;
            }
        }

        private void ShowFailure(string message, string caption = "Pharos Installer Bootstrapper")
        {
            _ui.ShowMessage(message, caption);
            _ui.WriteLine(message);
        }

        private void PrintUsage()
        {
            _ui.WriteLine("Pharos Installer Bootstrapper");
            _ui.WriteLine("Usage:");
            _ui.WriteLine("  PharosInstaller.exe [secureenclavesenabled=true|false] [testrollback=true|false]");
            _ui.WriteLine("");
            _ui.WriteLine("Defaults:");
            _ui.WriteLine("  secureenclavesenabled=true");
            _ui.WriteLine("  testrollback=false");
            _ui.WriteLine($"  executes .msi in a following way by default: PharosInstaller.msi /passive /l*vx {LogFolder}\\Pharos-install-timestamp.log secureenclavesenabled=true");
            _ui.WriteLine("");
            _ui.WriteLine("Examples:");
            _ui.WriteLine("  PharosInstaller.exe");
            _ui.WriteLine("  PharosInstaller.exe secureenclavesenabled=false");
            _ui.WriteLine("  PharosInstaller.exe testrollback=true");
        }
    }
}