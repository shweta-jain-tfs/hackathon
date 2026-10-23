namespace CustomAction.CustomActionLogic.Interfaces.Database;

public interface IDatabaseInstance
{
    bool BackupIfExists(string databaseName, string backupFullName);
    void AddWindowsUserAsLogin(string fullUserName);
    void AddUserToRole(string fullUserName, string role);
    bool IsSecureEnclaveEnabled();
    void EnableSecureEnclave();
    void RestartSqlServerService();
    bool RestoreDatabaseBackup(string databaseName, string backupFolder);
}