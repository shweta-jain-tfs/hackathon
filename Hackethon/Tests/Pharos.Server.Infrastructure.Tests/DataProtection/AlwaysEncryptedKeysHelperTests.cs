using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Pharos.Server.Infrastructure.DataProtection;
using Pharos.Server.Infrastructure.Services;
using Pharos.Server.Infrastructure.Settings;
using Xunit.Sdk;

namespace Pharos.Server.Infrastructure.Tests.DataProtection;

[CollectionDefinition(nameof(AlwaysEncryptedKeysHelperTestCollection), DisableParallelization = true)]
public sealed class AlwaysEncryptedKeysHelperTestCollection : ICollectionFixture<AlwaysEncryptedKeysHelperFixture>;

[Collection(nameof(AlwaysEncryptedKeysHelperTestCollection))]
public sealed class AlwaysEncryptedKeysHelperTests(AlwaysEncryptedKeysHelperFixture helperFixture) : IAsyncLifetime
{
    private const string _tableName = "EncryptedValues";
    private const string _value = "Sensitive test value";
    private const string _secondDatabaseName = "PharosColumnEncryptionKeysTestsSecond";

    public ValueTask InitializeAsync()
    {
        return helperFixture.ResetDatabaseAsync(TestContext.Current.CancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task EnsureAlwaysEncryptKeysCreated_WhenNoKeysExist_EncryptsExistingColumnAndReadsData()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await CreateTableAndInsertValueAsync(cancellationToken);

        await using (var connection = helperFixture.CreateConnection())
        {
            await connection.EnsureAlwaysEncryptKeysCreated(helperFixture.CertificateStore, NullLogger.Instance);
            await EncryptValueColumnAsync(connection, cancellationToken);
        }

        (await ReadValueAsync(cancellationToken)).Should().Be(_value);
    }

    [Fact]
    public async Task EnsureAlwaysEncryptKeysCreated_WhenCertificateIsMissing_Throws()
    {
        string certificateThumbprint;
        await using (var connection = helperFixture.CreateConnection())
        {
            await connection.EnsureAlwaysEncryptKeysCreated(helperFixture.CertificateStore, NullLogger.Instance);
            certificateThumbprint = (await connection.GetColumnMasterKey(helperFixture.CertificateStore, NullLogger.Instance))!.CertificateThumbprint;
        }

        helperFixture.RemoveCertificate(certificateThumbprint);

        await using var missingCertificateConnection = helperFixture.CreateConnection();
        // ReSharper disable once AccessToDisposedClosure
        var ensureKeys = () => missingCertificateConnection.EnsureAlwaysEncryptKeysCreated(helperFixture.CertificateStore, NullLogger.Instance);

        await ensureKeys.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*could not be found*");
    }

    [Fact]
    public async Task EnsureAlwaysEncryptKeysCreated_WhenCertificateHasExpired_RotatesKeyAndReadsData()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await CreateTableAndInsertValueAsync(cancellationToken);

        await using (var connection = helperFixture.CreateConnection())
        {
            await connection.OpenAsync(cancellationToken);
            var store = helperFixture.CertificateStore;
            store.CreateEncryptionCertificate(createExpired: true);
            var expiredKey = await connection.CreateColumnMasterKey(store);
            expiredKey.RequiresRotation.Should().BeTrue();
            await connection.EnsureAlwaysEncryptKeysCreated(store, NullLogger.Instance);
            await EncryptValueColumnAsync(connection, cancellationToken);

            (await ReadValueAsync(cancellationToken)).Should().Be(_value);

            await connection.EnsureAlwaysEncryptKeysCreated(store, NullLogger.Instance);
            var mk = await connection.GetColumnMasterKey(store, NullLogger.Instance);
            mk.Should().NotBeNull();
            mk.RequiresRotation.Should().BeFalse();

            // cert removal is done in ColumnMasterKeyRotationJob
            store.GetCertificate(store.StoreLocation, store.StoreName, expiredKey.CertificateThumbprint).Should().NotBeNull();

            // remove to test that the data can still be decrypted
            helperFixture.RemoveCertificate(expiredKey.CertificateThumbprint);
        }

        (await ReadValueAsync(cancellationToken)).Should().Be(_value);
    }

    [Fact]
    public async Task EnsureAlwaysEncryptKeysCreated_ForMultipleDatabases_UsesTheSameCertificate()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using (var connection = helperFixture.CreateConnection("master"))
        {
            await connection.OpenAsync(cancellationToken);
            await ExecuteNonQueryAsync(connection, $"CREATE DATABASE [{_secondDatabaseName}];", cancellationToken);
        }

        try
        {
            string firstCertificateThumbprint;
            await using (var firstDatabaseConnection = helperFixture.CreateConnection())
            {
                await firstDatabaseConnection.EnsureAlwaysEncryptKeysCreated(helperFixture.CertificateStore, NullLogger.Instance);
                firstCertificateThumbprint = (await firstDatabaseConnection.GetColumnMasterKey(helperFixture.CertificateStore, NullLogger.Instance))!.CertificateThumbprint;
            }

            await using var secondDatabaseConnection = helperFixture.CreateConnection(_secondDatabaseName);
            await secondDatabaseConnection.EnsureAlwaysEncryptKeysCreated(helperFixture.CertificateStore, NullLogger.Instance);
            var secondColumnMasterKey = await secondDatabaseConnection.GetColumnMasterKey(helperFixture.CertificateStore, NullLogger.Instance);

            secondColumnMasterKey.Should().NotBeNull();
            secondColumnMasterKey.CertificateThumbprint.Should().Be(firstCertificateThumbprint);
        }
        finally
        {
            await using var connection = helperFixture.CreateConnection("master");
            await connection.OpenAsync(cancellationToken);
            await ExecuteNonQueryAsync(connection, $"IF DB_ID(N'{_secondDatabaseName}') IS NOT NULL BEGIN ALTER DATABASE [{_secondDatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{_secondDatabaseName}]; END;", cancellationToken);
        }
    }

    private async Task CreateTableAndInsertValueAsync(CancellationToken cancellationToken)
    {
        await using var connection = helperFixture.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await ExecuteNonQueryAsync(connection, $"CREATE TABLE [{_tableName}] ([Id] int NOT NULL PRIMARY KEY, [Value] nvarchar(100) NOT NULL); INSERT INTO [{_tableName}] ([Id], [Value]) VALUES (1, N'{_value}');", cancellationToken);
    }

    private static Task EncryptValueColumnAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        return ExecuteNonQueryAsync(connection, $"ALTER TABLE [{_tableName}] ALTER COLUMN [Value] nvarchar(100) COLLATE Latin1_General_BIN2 ENCRYPTED WITH (COLUMN_ENCRYPTION_KEY = [{AlwaysEncryptedKeysHelper.ColumnEncryptionKeyName}], ENCRYPTION_TYPE = RANDOMIZED, ALGORITHM = 'AEAD_AES_256_CBC_HMAC_SHA_256') NOT NULL;", cancellationToken);
    }

    private async Task<string> ReadValueAsync(CancellationToken cancellationToken)
    {
        await using var connection = helperFixture.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand($"SELECT [Value] FROM [{_tableName}] WHERE [Id] = 1;", connection);
        return (string)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    private static async Task ExecuteNonQueryAsync(SqlConnection connection, string commandText, CancellationToken cancellationToken)
    {
#pragma warning disable CA2100
        await using var command = new SqlCommand(commandText, connection);
#pragma warning restore CA2100
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}

public sealed class AlwaysEncryptedKeysHelperFixture : IAsyncLifetime
{
    private const string _databaseName = "PharosColumnEncryptionKeysTests";
    private readonly List<(StoreLocation StoreLocation, StoreName StoreName, string Thumbprint)> _addedCertificates = [];
    private SqlConnectionStringBuilder? _connectionStringBuilder;
    private string? _skipReason;

    internal CertificateStore CertificateStore { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        var cancellationToken = CancellationToken.None;
        string configuredConnectionString;
        try
        {
            configuredConnectionString = ResolveConnectionString();
        }
        catch (SkipException exception)
        {
            _skipReason = exception.Message;
            return;
        }

        _connectionStringBuilder = new SqlConnectionStringBuilder(configuredConnectionString);
        if (_connectionStringBuilder.ColumnEncryptionSetting != SqlConnectionColumnEncryptionSetting.Enabled)
        {
            _skipReason = "Column Encryption Setting must be enabled in the integration-test connection string.";
            return;
        }

        try
        {
            await using var masterConnection = CreateMasterConnection();
            await masterConnection.OpenAsync(cancellationToken);
            await using var enclaveCommand = new SqlCommand("SELECT COUNT(*) FROM sys.dm_column_encryption_enclave;", masterConnection);
            if ((int)(await enclaveCommand.ExecuteScalarAsync(cancellationToken))! == 0)
            {
                _skipReason = "Always Encrypted with secure enclaves is not enabled on the SQL Server.";
                return;
            }

            await DropDatabaseIfExistsAsync(masterConnection, cancellationToken);
            await ExecuteNonQueryAsync(masterConnection, $"CREATE DATABASE [{_databaseName}];", cancellationToken);
        }
        catch (SqlException exception)
        {
            _skipReason = $"Always Encrypted with secure enclaves is unavailable: {exception.Message}";
            return;
        }

        var environment = new Mock<IHostEnvironment>();
        environment.Setup(x => x.EnvironmentName).Returns("XUnit");
        CertificateStore = new CertificateStore(
            environment.Object,
            Options.Create(new CertificateSettings()),
            new DateTimeService())
        {
            CertificateSuffix = "XUnit"
        };
        CertificateStore.CertificateAdded += OnCertificateAdded;
    }

    public async ValueTask DisposeAsync()
    {
        var cancellationToken = CancellationToken.None;
        foreach (var (storeLocation, storeName, thumbprint) in _addedCertificates)
        {
            RemoveCertificate(storeLocation, storeName, thumbprint);
        }

        if (_connectionStringBuilder != null)
        {
            await using var masterConnection = CreateMasterConnection();
            await masterConnection.OpenAsync(cancellationToken);
            await DropDatabaseIfExistsAsync(masterConnection, cancellationToken);
        }
    }

    public SqlConnection CreateConnection()
    {
        return new SqlConnection(_connectionStringBuilder!.ConnectionString);
    }

    public SqlConnection CreateConnection(string databaseName)
    {
        var builder = new SqlConnectionStringBuilder(_connectionStringBuilder!.ConnectionString)
        {
            InitialCatalog = databaseName
        };
        return new SqlConnection(builder.ConnectionString);
    }

    public async ValueTask ResetDatabaseAsync(CancellationToken cancellationToken)
    {
        if (_skipReason != null)
        {
            throw SkipException.ForSkip(_skipReason);
        }

        foreach (var (storeLocation, storeName, thumbprint) in _addedCertificates)
        {
            RemoveCertificate(storeLocation, storeName, thumbprint);
        }
        _addedCertificates.Clear();

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await ExecuteNonQueryAsync(connection, "IF OBJECT_ID(N'[EncryptedValues]', N'U') IS NOT NULL DROP TABLE [EncryptedValues]; IF EXISTS (SELECT 1 FROM sys.column_encryption_keys WHERE name = 'CEK') DROP COLUMN ENCRYPTION KEY [CEK]; DECLARE @sql nvarchar(max) = N''; SELECT @sql += N'DROP COLUMN MASTER KEY [' + REPLACE(name, N']', N']]') + N'];' FROM sys.column_master_keys WHERE name LIKE N'CMK%'; EXEC sp_executesql @sql;", cancellationToken);
    }

    public void RemoveCertificate(string thumbprint)
    {
        RemoveCertificate(CertificateStore.StoreLocation, CertificateStore.StoreName, thumbprint);
    }

    private SqlConnection CreateMasterConnection()
    {
        var builder = new SqlConnectionStringBuilder(_connectionStringBuilder!.ConnectionString)
        {
            InitialCatalog = "master"
        };
        return new SqlConnection(builder.ConnectionString);
    }

    private static string ResolveConnectionString()
    {
        foreach (var key in new[] { "ConnectionStrings__Default", "ConnectionStrings:Default", "Default" })
        {
            var value = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        throw SkipException.ForSkip("The main integration-test connection string is not configured.");
    }

    private void OnCertificateAdded(StoreLocation storeLocation, StoreName storeName, string thumbprint)
    {
        _addedCertificates.Add((storeLocation, storeName, thumbprint));
    }

    private static void RemoveCertificate(StoreLocation storeLocation, StoreName storeName, string thumbprint)
    {
        using var store = new X509Store(storeName, storeLocation);
        store.Open(OpenFlags.ReadWrite);
        foreach (var certificate in store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, validOnly: false))
        {
            store.Remove(certificate);
        }
    }

    private static async Task ExecuteNonQueryAsync(SqlConnection connection, string commandText, CancellationToken cancellationToken)
    {
#pragma warning disable CA2100
        await using var command = new SqlCommand(commandText, connection);
#pragma warning restore CA2100
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static Task DropDatabaseIfExistsAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        return ExecuteNonQueryAsync(connection, $"IF DB_ID(N'{_databaseName}') IS NOT NULL BEGIN ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{_databaseName}]; END;", cancellationToken);
    }
}
