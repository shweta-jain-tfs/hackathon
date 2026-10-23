using System;
using System.IO;
using CustomAction.Wrappers.Interfaces;
using WixToolset.Dtf.WindowsInstaller;

namespace CustomAction.CustomActionLogic;

public class ApplicationInitializationActions
{
    private readonly ISessionWrapper _session;
    private readonly IFileSystem _fileSystem;
    private readonly IProcessRunner _processRunner;

    public ApplicationInitializationActions(ISessionWrapper session, IFileSystem fileSystem, IProcessRunner processRunner)
    {
        _session = session;
        _fileSystem = fileSystem;
        _processRunner = processRunner;
    }

    public ActionResult RunInitDb(string installFolder)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(installFolder))
            {
                _session.Log("RunInitDb: INSTALLFOLDER was not provided.");
                _session.Message(InstallMessage.Error, new Record { FormatString = "INSTALLFOLDER was not provided." });
                return ActionResult.Failure;
            }

            var exePath = Path.Combine(installFolder, "Pharos.Server.exe");
            _session.Log($"RunInitDb: exePath='{exePath}', workingDir='{installFolder}'");

            if (!_fileSystem.Exists(exePath))
            {
                _session.Log($"RunInitDb: Pharos.Server.exe not found in {installFolder}.");
                _session.Message(InstallMessage.Error, new Record { FormatString = $"Pharos.Server.exe not found in {installFolder}." });
                return ActionResult.Failure;
            }

            var exitCode = _processRunner.Run(exePath, "--init-db", installFolder);
            _session.Log($"RunInitDb: exitCode={exitCode}");

            if (exitCode != 0)
            {
                _session.Message(InstallMessage.Error, new Record { FormatString = $"Database initialization failed (exit code {exitCode})." });
                return ActionResult.Failure;
            }

            return ActionResult.Success;
        }
        catch (Exception e)
        {
            _session.Log(e.ToString());
            _session.Message(InstallMessage.Error, new Record { FormatString = e.Message });
            return ActionResult.Failure;
        }
    }
}