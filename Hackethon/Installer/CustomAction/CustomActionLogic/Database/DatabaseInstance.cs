using System;
using System.Globalization;
using System.ServiceProcess;
using CustomAction.CustomActionLogic.Interfaces.Database;
using CustomAction.Wrappers.Interfaces;

namespace CustomAction.CustomActionLogic.Database;

public class DatabaseInstance : IDatabaseInstance
{
    private readonly IDbConnectionFactory _dbConnectionFactory;
    private readonly IDbCommandFactory _dbCommandFactory;
    private readonly ISessionWrapper _session;
    private readonly string _connectionString;

    public DatabaseInstance(IDbConnectionFactory dbConnectionFactory,
        IDbCommandFactory dbCommandFactory,
        ISessionWrapper session,
        string connectionString)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _dbCommandFactory = dbCommandFactory;
        _session = session;
        _connectionString = connectionString;
    }


    public bool BackupIfExists(string databaseName, string backupFullName)
    {
        using var connection = _dbConnectionFactory.CreateConnection(_connectionString);
        connection.Open();

        var query =
            @"
                    IF EXISTS(SELECT 1 FROM master.dbo.sysdatabases where name=@dbName)
                    BEGIN
                       SELECT 1
                    BACKUP DATABASE @dbName
                    TO DISK = @path
                    WITH
                        INIT,
                        NAME = @dbName
                    END
                    ELSE
                    BEGIN
                        SELECT 0
                    END;
                ";
        using var cmd = _dbCommandFactory.CreateCommand(query, connection);

        cmd.SetCommandTimeout(300);
        cmd.AddParameter("dbName", databaseName);
        cmd.AddParameter("path", backupFullName);

        var succeed = cmd.ExecuteScalar();
        connection.Close();

        return succeed;
    }

    public bool RestoreDatabaseBackup(string databaseName, string backupFullName)
    {
        using var connection = _dbConnectionFactory.CreateConnection(_connectionString);
        connection.Open();

        var query = @"
            IF EXISTS (SELECT 1 FROM master.dbo.sysdatabases WHERE name = @dbName)
            BEGIN
                RESTORE DATABASE @dbName
                FROM DISK = @path
                WITH REPLACE;
                SELECT 1;
            END
            ELSE
            BEGIN
                SELECT 0;
            END";

        using var cmd = _dbCommandFactory.CreateCommand(query, connection);

        cmd.SetCommandTimeout(300);
        cmd.AddParameter("dbName", databaseName);
        cmd.AddParameter("path", backupFullName);

        var succeed = cmd.ExecuteScalar();
        connection.Close();

        return succeed;
    }

    public void AddWindowsUserAsLogin(string serviceAccount)
    {
        ValidateSqlIdentifier(serviceAccount);

        using var connection = _dbConnectionFactory.CreateConnection(_connectionString);
        connection.Open();

        var userNameIdentifier = '[' + serviceAccount + ']';

        var query = $@"
        IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = @userName)
        BEGIN
            EXEC sp_executesql N'CREATE LOGIN {userNameIdentifier} FROM WINDOWS';
        END;";

        using var cmd = _dbCommandFactory.CreateCommand(query, connection);

        cmd.AddParameter("userName", serviceAccount);

        cmd.ExecuteNonQuery();
        connection.Close();
    }

    public void AddUserToRole(string serviceAccount, string role)
    {
        ValidateSqlIdentifier(serviceAccount);
        ValidateSqlIdentifier(role);

        using var connection = _dbConnectionFactory.CreateConnection(_connectionString);
        connection.Open();

        var userNameIdentifier = '[' + serviceAccount + ']';

        var query = $@"
        IF EXISTS (SELECT 1 FROM sys.server_principals WHERE name = @userName)
           AND NOT EXISTS (
                SELECT 1
                FROM sys.server_role_members rm
                JOIN sys.server_principals r ON rm.role_principal_id = r.principal_id
                JOIN sys.server_principals m ON rm.member_principal_id = m.principal_id
                WHERE r.name = @role AND m.name = @userName
           )
        BEGIN
            EXEC sp_executesql N'ALTER SERVER ROLE {role} ADD MEMBER {userNameIdentifier}';
        END;";

        using var cmd = _dbCommandFactory.CreateCommand(query, connection);

        cmd.AddParameter("userName", serviceAccount);
        cmd.AddParameter("role", role);

        cmd.ExecuteNonQuery();
        connection.Close();
    }

    private void ValidateSqlIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new ArgumentException("Identifier must be provided.");
        }

        // Ensure the identifier contains only valid characters (alphanumeric, underscores, backslashes, and spaces)
        if (!System.Text.RegularExpressions.Regex.IsMatch(identifier, @"^[-a-zA-Z0-9_\\ ]+$"))
        {
            throw new ArgumentException("Identifier contains invalid characters.");
        }
    }

    public bool IsSecureEnclaveEnabled()
    {
        const string sql = "SELECT [value_in_use] FROM sys.configurations WHERE [name] = 'column encryption enclave type';";
        using var connection = _dbConnectionFactory.CreateConnection(_connectionString);
        connection.Open();
        using var cmd = _dbCommandFactory.CreateCommand(sql, connection);
        var result = cmd.ExecuteScalar();
        connection.Close();
        return Convert.ToInt32(result, CultureInfo.InvariantCulture) == 1;
    }

    public void EnableSecureEnclave()
    {
        try
        {
            const string sql = "EXEC sp_configure 'column encryption enclave type', 1; RECONFIGURE;";
            using var connection = _dbConnectionFactory.CreateConnection(_connectionString);

            connection.Open();

            using var cmd = _dbCommandFactory.CreateCommand(sql, connection);
            cmd.ExecuteNonQuery();

            connection.Close();

            _session.Log("Secure enclave enabled.");
        }
        catch (Exception ex)
        {
            _session.Log($"Error enabling secure enclave: {ex.Message}");
            throw;
        }
    }

    public void RestartSqlServerService()
    {
        try
        {
            _session.Log("Restarting SQL Server service.");
            using var service = new ServiceController("MSSQLSERVER");
            if (service.Status == ServiceControllerStatus.Running)
            {
                service.Stop();
                service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
            }

            service.Start();
            service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
            _session.Log("SQL Server service restarted successfully.");
        }
        catch (Exception ex)
        {
            _session.Log($"Error restarting SQL Server service: {ex.Message}");
            throw;
        }
    }
}