using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Pharos.Server.Infrastructure.DataProtection;

internal static class ColumnEncryptionHelper
{
    public static async Task EncryptColumns(this DbContext context, string schemaName, List<ProtectedColumnInfo> columnsToEncrypt, ILogger logger)
    {
        foreach (var column in columnsToEncrypt)
        {
            logger.LogInformation("Encrypting column {SchemaName}.{TableName}.{ColumnName}.", schemaName, column.TableName, column.ColumnName);

            var typeDefinition = await context.GetColumnTypeDefinition(schemaName, column.TableName, column.ColumnName);

            // Generate the SQL to alter the column and apply encryption
            // ReSharper disable once StringLiteralTypo
            var alterColumnSql = $@"
            ALTER TABLE [{schemaName}].[{column.TableName}]
            ALTER COLUMN [{column.ColumnName}] {typeDefinition}
            ENCRYPTED WITH (
                COLUMN_ENCRYPTION_KEY = [{AlwaysEncryptedKeysHelper.ColumnEncryptionKeyName}],
                ENCRYPTION_TYPE = Randomized,
                ALGORITHM = 'AEAD_AES_256_CBC_HMAC_SHA_256'
            )";

            // Execute the SQL to encrypt the column
            await context.Database.ExecuteSqlRawAsync(alterColumnSql);
        }

        await context.Database.ExecuteSqlRawAsync("ALTER DATABASE SCOPED CONFIGURATION CLEAR PROCEDURE_CACHE");
    }

    public static async Task<string> GetColumnTypeDefinition(this DbContext context, string schemaName, string tableName, string columnName)
    {
        var query = @"
            SELECT 
                DATA_TYPE +
                CASE 
                    WHEN DATA_TYPE IN ('char', 'varchar', 'nchar', 'nvarchar', 'binary', 'varbinary') THEN 
                        '(' + CASE WHEN CHARACTER_MAXIMUM_LENGTH = -1 THEN 'MAX' ELSE CAST(CHARACTER_MAXIMUM_LENGTH AS VARCHAR) END + ') COLLATE Latin1_General_BIN2'
                    WHEN DATA_TYPE IN ('decimal', 'numeric') THEN 
                        '(' + CAST(NUMERIC_PRECISION AS VARCHAR) + ',' + CAST(NUMERIC_SCALE AS VARCHAR) + ')'
                    ELSE ''
                END AS ColumnTypeDefinition
            FROM 
                INFORMATION_SCHEMA.COLUMNS
            WHERE 
                TABLE_SCHEMA = @p0
                AND TABLE_NAME = @p1
                AND COLUMN_NAME = @p2";

        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = query;
        command.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@p0", schemaName));
        command.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@p1", tableName));
        command.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@p2", columnName));

        // Open the connection if it's not already open
        if (command.Connection!.State != System.Data.ConnectionState.Open)
        {
            await command.Connection.OpenAsync();
        }

        var result = await command.ExecuteScalarAsync();
        return result?.ToString() ?? throw new InvalidOperationException($"Column {schemaName}.{tableName}.{columnName} not found.");
    }
}
