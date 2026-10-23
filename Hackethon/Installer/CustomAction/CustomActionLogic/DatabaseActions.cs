using System;
using System.Data.SqlClient;
using System.IO;
using CustomAction.CustomActionLogic.Interfaces.Database;
using CustomAction.Wrappers.Interfaces;
using WixToolset.Dtf.WindowsInstaller;

namespace CustomAction.CustomActionLogic;

public class DatabaseActions
{
    readonly string _backupFolder;
    readonly string _defaultDbName = "Pharos";
    private readonly ISessionWrapper _session;
    private readonly IFileSystem _fileSystem;
    private readonly IDatabaseInstanceFactory _dbInstanceFactory;
    private readonly IDate _date;
    private readonly IServiceAccountOptions _accountOptions;

    public DatabaseActions(ISessionWrapper session, IFileSystem fileSystem, IEnvironment env, IDatabaseInstanceFactory dbInstanceFactory, IDate date, IServiceAccountOptions accountOptions)
    {
        _session = session;
        _fileSystem = fileSystem;
        _dbInstanceFactory = dbInstanceFactory;
        _date = date;
        _accountOptions = accountOptions;
        _backupFolder = Path.Combine(env.GetCommonAppDataPath(), @"Thermo Fisher Scientific\Pharos\DatabaseBackups");
    }

    public ActionResult BackupDatabase()
    {
        try
        {
            _session.Log("Begin BackupDatabase");

            var dt = _date.Now;
             var fileName = "PharosDBBackup-" + String.Format("{0:yyyy-MM-dd-hhmm}", dt);
            var extension = "bak";
            var fileNameWithSuffix = Path.ChangeExtension(fileName, extension);
            var path = Path.Combine(_backupFolder, fileNameWithSuffix);

            _session.Log(_defaultDbName);
            _session.Log(_backupFolder);
            _session.Log(path);

            if (_fileSystem.DirectoryExists(_backupFolder))
            {
                _fileSystem.DeleteDirectory(_backupFolder);
            }

            _fileSystem.CreateDirectory(_backupFolder);
            _session.Log(_backupFolder + "folder was added.");

            try
            {
                if (_dbInstanceFactory.CreateInstance().BackupIfExists(_defaultDbName, path))
                {
                    _session.Log("Database backup is ready.");
                }
                else
                {
                    _session.Log("Skipping database backup, because database doesn't exist.");
                }
            }
            catch (SqlException ex)
            {
                _session.Log($"Database exception: {ex.Message}");
                _session.Message(InstallMessage.Error, new Record { FormatString = ex.Message });
                return ActionResult.Failure;
            }

            return ActionResult.Success;
        }
        catch (Exception e)
        {
            return HandleFailure(e);
        }
    }

    public ActionResult AddWindowsServiceAccountAsSqlLogin()
    {
        try
        { 
            var serviceAccount = _accountOptions.ServiceAccount;
            _session.Log($"Ensuring SQL login and dbcreator role membership for {serviceAccount}.");

            var dbInstance = _dbInstanceFactory.CreateInstance();

            dbInstance.AddWindowsUserAsLogin(serviceAccount);
            // This is needed so that Pharos can create databases during startup.
            dbInstance.AddUserToRole(serviceAccount, "dbcreator");

            _session.Log($"{serviceAccount} ensured as SQL login and member of dbcreator.");
            return ActionResult.Success;
        }
        catch (Exception e)
        {
            return HandleFailure(e);
        }
    }

    public ActionResult RestoreDatabaseBackup()
    {
        try
        {
            _session.Log("Begin RestoreDatabaseBackup");

            if (!_fileSystem.DirectoryExists(_backupFolder))
            {
                _session.Log("Backup folder does not exist: " + _backupFolder);

                return ActionResult.Failure;
            }
            var files = _fileSystem.GetFiles(_backupFolder, "*.bak");

            if (files.Length == 0)
            {
                _session.Log("No backup files found in the backup folder.");

                return ActionResult.Success;
            }

            var path = files[0];

            _session.Log("Database: " + _defaultDbName);
            _session.Log("Path: " + path);

            var success = _dbInstanceFactory.CreateInstance().RestoreDatabaseBackup(_defaultDbName, path);

            _session.Log(success ? "Backup database restored successfully." : "Failed to restore database from backup.");

            return ActionResult.Success;
        }
        catch (Exception e)
        {
            _session.Log("Error restoring database: " + e.Message);
            return HandleFailure(e);
        }
    }

    private ActionResult HandleFailure(Exception e)
    {
        _session.Log(e.ToString());
        _session.Message(InstallMessage.Error, new Record { FormatString = e.Message });
        return ActionResult.Failure;
    }

    public ActionResult EnableSecureEnclaveIfDisabled()
    {
        try
        {
            _session.Log("Checking if Secure Enclave is enabled.");

            var dbInstance = _dbInstanceFactory.CreateInstance();

            // Check if SQL Server is configured to use Secure Enclave
            if (!dbInstance.IsSecureEnclaveEnabled())
            {
                _session.Log("Always secure enclave is not enabled. Trying to enable it.");
                dbInstance.EnableSecureEnclave();
                dbInstance.RestartSqlServerService();
            }
            return ActionResult.Success;
        }
        catch (Exception e)
        {
            return HandleFailure(e);
        }
    }
}