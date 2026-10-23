using System.Data;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Pharos.Server.Infrastructure.Services;

namespace Pharos.Server.Infrastructure.Database;

public abstract class DbContextInitializer(
    DbContext context,
    IDbEncryptionService dbEncryptionService) : IDbContextInitializer
{
    protected abstract string SchemaName { get; }

    private const string _naturalSortKeyFunctionSql = """
CREATE FUNCTION dbo.NaturalSortKey
(
    @input nvarchar(32)
)
RETURNS nvarchar(4000)
WITH SCHEMABINDING
AS
BEGIN
    IF @input IS NULL
        RETURN NULL;

    DECLARE @i int = 1;
    DECLARE @len int = LEN(@input);
    DECLARE @result nvarchar(4000) = N'';
    DECLARE @ch nchar(1);

    DECLARE @digitRun nvarchar(32);
    DECLARE @trimmed nvarchar(32);
    DECLARE @digitRunLen int;
    DECLARE @trimmedLen int;
    DECLARE @firstNonZero int;

    WHILE @i <= @len
    BEGIN
        SET @ch = SUBSTRING(@input, @i, 1);

        IF @ch >= N'0' AND @ch <= N'9'
        BEGIN
            SET @digitRun = N'';

            WHILE @i <= @len
              AND SUBSTRING(@input, @i, 1) >= N'0'
              AND SUBSTRING(@input, @i, 1) <= N'9'
            BEGIN
                SET @digitRun += SUBSTRING(@input, @i, 1);
                SET @i += 1;
            END;

            SET @digitRunLen = LEN(@digitRun);
            SET @firstNonZero = PATINDEX(N'%[^0]%', @digitRun);

            IF @firstNonZero = 0
                SET @trimmed = N'0';
            ELSE
                SET @trimmed = SUBSTRING(@digitRun, @firstNonZero, @digitRunLen - @firstNonZero + 1);

            SET @trimmedLen = LEN(@trimmed);

            SET @result +=
                NCHAR(1) +
                RIGHT(N'00' + CONVERT(nvarchar(2), @trimmedLen), 2) +
                N':' +
                @trimmed +
                N':' +
                RIGHT(N'00' + CONVERT(nvarchar(2), @digitRunLen), 2) +
                NCHAR(1);
        END
        ELSE
        BEGIN
            SET @result += UPPER(@ch);
            SET @i += 1;
        END
    END;

    RETURN @result;
END;
""";

    public virtual async Task InitializeAsync(RecreateObjectsMode recreateMode)
    {
        await context.Database.EnsureCreatedAsync();

        await EnsureNaturalSortKeyFunctionAsync();

        if (recreateMode != RecreateObjectsMode.Never)
        {
            var hasPendingMigrations = (await context.Database.GetPendingMigrationsAsync()).Any();

            if (hasPendingMigrations
                && (recreateMode == RecreateObjectsMode.MigrationsPending || (recreateMode == RecreateObjectsMode.MigrationsPendingForBaseline && context.Database.GetMigrations().Count() == 1)))
            {
                await DropTables(SchemaName);
            }
        }

        await context.Database.MigrateAsync();

        await dbEncryptionService.UseAlwaysEncryptedWithSecureEnclaves(context, SchemaName);
    }

    public abstract Task SeedAsync();

    private async Task EnsureNaturalSortKeyFunctionAsync()
    {
        var connection = context.Database.GetDbConnection();
        var closeWhenDone = false;

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
            closeWhenDone = true;
        }

        try
        {
            await using var existsCommand = connection.CreateCommand();
            existsCommand.CommandText = "SELECT OBJECT_ID(N'dbo.NaturalSortKey', N'FN')";
            var existingObjectId = await existsCommand.ExecuteScalarAsync();

            if (existingObjectId is null or DBNull)
            {
                await context.Database.ExecuteSqlRawAsync(_naturalSortKeyFunctionSql);
            }
        }
        finally
        {
            if (closeWhenDone)
            {
                await connection.CloseAsync();
            }
        }
    }

    protected Task DropTables(string schema)
    {
        var sql = $@"
DECLARE @drop NVARCHAR(MAX) = N'';

-- build a concatenated of foreign keys to drop
SELECT @drop += N'
ALTER TABLE ' + QUOTENAME(cs.name) + '.' + QUOTENAME(ct.name) + ' DROP CONSTRAINT ' + QUOTENAME(fk.name) + ';'
FROM sys.foreign_keys AS fk
INNER JOIN sys.tables AS ct
  ON fk.parent_object_id = ct.object_id
INNER JOIN sys.schemas AS cs 
  ON ct.schema_id = cs.schema_id
WHERE cs.name = '{schema}';

-- build a concatenated list of tables to drop
SELECT @drop += N'
DROP TABLE ' + QUOTENAME(cs.name) + '.' + QUOTENAME(ct.name) + ';'
FROM sys.tables AS ct
INNER JOIN sys.schemas AS cs 
  ON ct.schema_id = cs.schema_id
WHERE cs.name = '{schema}';

EXEC sp_executesql @drop";

        return context.Database.ExecuteSqlAsync(FormattableStringFactory.Create(sql));

    }
}
