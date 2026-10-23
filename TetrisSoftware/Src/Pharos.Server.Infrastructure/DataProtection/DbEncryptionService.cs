using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Pharos.Server.Domain.Common;
using Pharos.Server.Infrastructure.Services;
using Pharos.Server.Infrastructure.Settings;

namespace Pharos.Server.Infrastructure.DataProtection;

/// <summary>
/// Provides functionality to enable and manage database encryption using SQL Server's "Always Encrypted with Secure Enclaves" feature.
/// </summary>
internal class DbEncryptionService(
    IConfiguration configuration,
    ICertificateStore certificateStore,
    ILogger<DbEncryptionService> logger) : IDbEncryptionService
{
    private readonly List<string> _connectionStrings = new();

    /// <summary>
    /// Enables "Always Encrypted with Secure Enclaves" for the specified <see cref="DbContext"/>.
    /// This method identifies columns marked with <see cref="ProtectedDataAttribute"/> in the DbContext and ensures they are encrypted in the database.
    /// </summary>
    /// <param name="context">The <see cref="DbContext"/> to analyze for protected columns.</param>
    /// <param name="schemaName">The schema name to filter database columns. Defaults to "dbo" if not provided.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task UseAlwaysEncryptedWithSecureEnclaves(DbContext context, string? schemaName = null)
    {
        if (string.IsNullOrWhiteSpace(schemaName))
        {
            schemaName = "dbo";
        }

        var dataProtectionSettings = new DataProtectionSettings();
        configuration.GetSection(DataProtectionSettings.DataProtectionSection).Bind(dataProtectionSettings);

        if (!dataProtectionSettings.EnableDataProtection)
        {
            // Data protection is disabled, so no action is taken.
            return;
        }

        var connectionString = context.Database.GetDbConnection().ConnectionString;
        if (!_connectionStrings.Contains(connectionString))
        {
            // Make sure that column encryption keys are created.
            // We need to do this before checking that the columns are encrypted
            // To cover the case when the database is restored on the new computer.
            // The application startup must fail if the master column certificate has not been imported.
            // When troubleshooting the client database one must turn the data protection off to start the app.
            await context.Database.GetDbConnection().EnsureAlwaysEncryptKeysCreated(certificateStore, logger);

            _connectionStrings.Add(connectionString);
        }

        var contextName = context.GetType().Name;

        // Retrieve protected columns from the database and the DbContext
        logger.LogInformation("Checking if database encryption is needed for {DbContext}.", contextName);
        var protectedColumnsDatabase = await GetProtectedColumnsInDatabase(context, schemaName);
        var protectedColumnsDbContext = GetProtectedColumnsInDbContext(context);

        if (protectedColumnsDatabase.Count == 0 && protectedColumnsDbContext.Count == 0)
        {
            // no database encryption needed 
            return;
        }


        logger.LogInformation("Setting up database encryption for {DbContext}.", contextName);

        // Check if Always Encrypted with Secure Enclaves is enabled in SQL Server
        if (!await IsAlwaysEncryptedWithSecureEnclavesEnabled(context))
        {
            logger.LogError("'Always Encrypted with Secure Enclaves' is disabled in the SQL Server instance. The data will not be protected. For instructions on how to enable the feature, see https://learn.microsoft.com/en-us/sql/relational-databases/security/tutorial-getting-started-with-always-encrypted-enclaves");

            logger.LogInformation("Trying to enable 'Always Encrypted with Secure Enclaves' in SQL Server instance.");
            await context.Database.ExecuteSqlRawAsync("EXEC sys.sp_configure 'column encryption enclave type', 1;RECONFIGURE;");
            logger.LogInformation("'Always Encrypted with Secure Enclaves' is enabled. Restart the SQL Server instance.");
            return;
        }

        // Check if the database has encrypted columns that should not be encrypted
        var encryptedColumnsWhichShouldNotBe = protectedColumnsDatabase
            .Where(c => !protectedColumnsDbContext.Any(db => db.TableName == c.TableName && db.ColumnName == c.ColumnName))
            .ToList();
        if (encryptedColumnsWhichShouldNotBe.Count > 0)
        {
            // Un-encrypting a column is not a trivial task, that requires manual data and schema migration.
            // If you are early in development, you can drop migrations for the DbContext and re-create the initial migration.
            logger.LogWarning("The following columns are encrypted in the database but not in the DbContext: {EncryptedColumns}",
                encryptedColumnsWhichShouldNotBe.Select(x => $"[{schemaName}].[{x.TableName}].[{x.ColumnName}]").ToList());
        }

        var columnsToEncrypt = protectedColumnsDbContext
            .Where(c => !protectedColumnsDatabase.Any(db => db.TableName == c.TableName && db.ColumnName == c.ColumnName))
            .ToList();

        if (columnsToEncrypt.Count == 0)
        {
            // no columns to encrypt
            return;
        }

        // Check if Column Encryption Setting is enabled in the connection string
        CheckColumnEncryptionEnabledForConnection(context);

        // Encrypt the columns in the database
        await context.EncryptColumns(schemaName, columnsToEncrypt, logger);

        logger.LogInformation("Database encryption setup for {DbContext} is complete.", contextName);
    }

    /// <summary>
    /// Checks if the "Column Encryption Setting" is enabled in the connection string of the DbContext.
    /// </summary>
    private static void CheckColumnEncryptionEnabledForConnection(DbContext context)
    {
        var connectionString = context.Database.GetDbConnection().ConnectionString;

        // Parse the connection string and check for "Column Encryption Setting=Enabled"
        var builder = new SqlConnectionStringBuilder(connectionString);

        if (builder.ColumnEncryptionSetting != SqlConnectionColumnEncryptionSetting.Enabled || builder.AttestationProtocol != SqlConnectionAttestationProtocol.None)
        {
            throw new InvalidOperationException($"Cannot encrypt columns. Make sure that connection string for {context.GetType().Name} includes following parameters: Column Encryption Setting=Enabled;Attestation Protocol=None");
        }
    }

    /// <summary>
    /// Checks if the "Always Encrypted with Secure Enclaves" feature is enabled in the SQL Server instance.
    /// </summary>
    /// <param name="context">The <see cref="DbContext"/> to use for database access.</param>
    /// <returns>A task that returns true if the feature is enabled, otherwise false.</returns>
    private static async Task<bool> IsAlwaysEncryptedWithSecureEnclavesEnabled(DbContext context)
    {
        const string sql = "SELECT [value_in_use] FROM sys.configurations WHERE [name] = 'column encryption enclave type';";

        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;

        // Open the connection if it's not already open
        if (command.Connection!.State != System.Data.ConnectionState.Open)
        {
            await command.Connection.OpenAsync();
        }

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result, CultureInfo.InvariantCulture) == 1;
    }


    /// <summary>
    /// Retrieves a list of columns in the database that are encrypted and belong to the specified schema.
    /// </summary>
    /// <param name="context">The <see cref="DbContext"/> to use for database access.</param>
    /// <param name="schemaName">The schema name to filter columns.</param>
    /// <returns>A task that returns a list of <see cref="ProtectedColumnInfo"/> objects representing encrypted columns in the database.</returns>
    private static async Task<List<ProtectedColumnInfo>> GetProtectedColumnsInDatabase(DbContext context, string schemaName)
    {
        const string sql = @"
        SELECT 
            t.name AS TableName,
            c.name AS ColumnName
        FROM 
            sys.columns AS c
        JOIN 
            sys.tables AS t ON c.object_id = t.object_id
        JOIN 
            sys.schemas AS s ON t.schema_id = s.schema_id
        LEFT JOIN 
            sys.column_encryption_keys AS cek ON c.column_encryption_key_id = cek.column_encryption_key_id
        WHERE 
            c.encryption_type IS NOT NULL AND s.name = @schemaName
        ORDER BY
            TableName, ColumnName";

        var protectedColumns = new List<ProtectedColumnInfo>();

        // Use ADO.NET to execute the query and read the results
        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        command.Parameters.Add(new SqlParameter("@schemaName", schemaName));

        // Open the connection if it's not already open
        if (command.Connection!.State != System.Data.ConnectionState.Open)
        {
            await command.Connection.OpenAsync();
        }

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            protectedColumns.Add(new ProtectedColumnInfo
            {
                TableName = reader.GetString(0), // First column: TableName
                ColumnName = reader.GetString(1) // Second column: ColumnName
            });
        }

        return protectedColumns;
    }

    /// <summary>
    /// Retrieves a list of columns in the DbContext that are marked with the <see cref="ProtectedDataAttribute"/>.
    /// </summary>
    /// <param name="context">The <see cref="DbContext"/> to analyze.</param>
    /// <returns>A list of <see cref="ProtectedColumnInfo"/> objects representing protected columns in the DbContext.</returns>
    private static List<ProtectedColumnInfo> GetProtectedColumnsInDbContext(DbContext context)
    {
        var protectedColumns = new List<ProtectedColumnInfo>();

        // Get all DbSet properties in the DbContext
        var dbSetProperties = context.GetType()
            .GetProperties()
            .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>));

        foreach (var dbSetProperty in dbSetProperties)
        {
            // Get the entity type for the DbSet
            var entityType = context.Model.FindEntityType(dbSetProperty.PropertyType.GetGenericArguments()[0]);
            if (entityType == null)
            {
                continue;
            }

            var tableName = entityType.GetTableName();
            var schemaName = entityType.GetSchema();

            if (string.IsNullOrEmpty(tableName) || string.IsNullOrEmpty(schemaName))
            {
                continue;
            }

            // Retrieve only properties marked with [ProtectedDataAttribute]
            var columns = entityType.ClrType.GetProperties()
                .Where(prop => Attribute.IsDefined(prop, typeof(ProtectedDataAttribute))
                               || Attribute.IsDefined(prop, typeof(ProtectedPersonalDataAttribute)))
                .Select(prop =>
                {
                    var columnName = entityType.FindProperty(prop.Name)?.GetColumnName(StoreObjectIdentifier.Table(tableName, schemaName));
                    return new ProtectedColumnInfo
                    {
                        TableName = tableName,
                        ColumnName = columnName!
                    };
                })
                .Where(info => !string.IsNullOrEmpty(info.ColumnName));

            protectedColumns.AddRange(columns);
        }

        return protectedColumns.OrderBy(x => x.TableName).ThenBy(x => x.ColumnName).ToList();
    }
}

/// <summary>
/// Represents information about a protected column in the database.
/// </summary>
internal class ProtectedColumnInfo
{
    /// <summary>
    /// Gets or sets the name of the table containing the protected column.
    /// </summary>
    public string TableName { get; set; } = default!;

    /// <summary>
    /// Gets or sets the name of the protected column.
    /// </summary>
    public string ColumnName { get; set; } = default!;
}
