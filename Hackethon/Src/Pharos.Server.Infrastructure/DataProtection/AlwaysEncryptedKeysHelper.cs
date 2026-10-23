using System.Data.Common;
using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
// ReSharper disable StringLiteralTypo

namespace Pharos.Server.Infrastructure.DataProtection;

internal static class AlwaysEncryptedKeysHelper
{
    public const string ColumnEncryptionKeyName = "CEK";
    private const string _columnMasterKeyNamePrefix = "CMK";

    private static readonly TimeSpan _transitionPeriod = TimeSpan.FromDays(30);

    internal record ColumnMasterKey(string KeyName, string CertificateThumbprint, bool RequiresRotation);

    public static async Task EnsureAlwaysEncryptKeysCreated(
        this DbConnection connection,
        ICertificateStore certificateStore,
        ILogger logger)
    {
        // Open the connection if it's not already open
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        var columnMasterKey = await connection.GetColumnMasterKey(certificateStore, logger);
        var columnEncryptionKeyExists = await connection.IsColumnEncryptionKeyCreated(logger);

        if (columnMasterKey == null)
        {
            if (columnEncryptionKeyExists)
            {
                // should not happen, means that someone deleted the column master key but left the column encryption key in place
                throw new InvalidOperationException($"Column encryption key '{ColumnEncryptionKeyName}' exists, but no column master key was found. This is an inconsistent state.");
            }

            logger.LogInformation("Column master key does not exist. Creating it.");
            columnMasterKey = await connection.CreateColumnMasterKey(certificateStore);
        }

        if (!columnEncryptionKeyExists)
        {
            await connection.CreateColumnEncryptionKey(certificateStore, columnMasterKey, logger);
        }

        if (columnMasterKey.RequiresRotation)
        {
            await connection.RotateColumnMasterKey(certificateStore, columnMasterKey, logger);
        }
    }

    internal static async Task<ColumnMasterKey?> GetColumnMasterKey(this DbConnection connection, ICertificateStore certificateStore, ILogger logger)
    {
        logger.LogInformation("Checking if a column master key exists in the database.");

        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT name, key_path FROM sys.column_master_keys WHERE name LIKE '{_columnMasterKeyNamePrefix}%'";

        await using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var keyName = reader.GetString(0);
            var keyPath = reader.GetString(1);
            var keyPathParts = keyPath.Split('/', StringSplitOptions.TrimEntries);
            if (keyPathParts.Length != 3 ||
                !Enum.TryParse<StoreLocation>(keyPathParts[0], out var storeLocation) ||
                !Enum.TryParse<StoreName>(keyPathParts[1], out var storeName) ||
                string.IsNullOrWhiteSpace(keyPathParts[2]))
            {
                throw new InvalidOperationException($"The column master key has an invalid key path '{keyPath}'.");
            }

            using var existingCertificate = certificateStore.GetCertificate(storeLocation, storeName, keyPathParts[2]);
            if (existingCertificate == null)
            {
                throw new InvalidOperationException($"Certificate '{keyPathParts[2]}' for the column master key could not be found in the {storeLocation}/{storeName} certificate store.");
            }

            return new ColumnMasterKey(keyName, existingCertificate.Thumbprint, existingCertificate.NeedsRotation());
        }

        return null;
    }

    internal static async Task<ColumnMasterKey> CreateColumnMasterKey(this DbConnection connection, ICertificateStore certificateStore, bool forceCreate = false)
    {
        var certificate = certificateStore.GetEncryptionCertificate();
        if (certificate == null || (certificate.NeedsRotation() && forceCreate))
        {
            certificate = certificateStore.CreateEncryptionCertificate();
        }

        // Generate the signature for enclave computations
        var signature = GenerateEnclaveSignature(certificateStore.StoreLocation, certificateStore.StoreName, certificate.Thumbprint);

        // Create the column master key using the certificate and enclave signature
        var keyName = $"{_columnMasterKeyNamePrefix}_{certificate.NotBefore:yy_MM_dd}";
        var createColumnMasterKeySql = $@"
            CREATE COLUMN MASTER KEY [{keyName}]
            WITH (
                KEY_STORE_PROVIDER_NAME = N'MSSQL_CERTIFICATE_STORE',
                KEY_PATH = N'{certificateStore.StoreLocation}/{certificateStore.StoreName}/{certificate.Thumbprint}',
                ENCLAVE_COMPUTATIONS (SIGNATURE = {signature})
            );";

        await connection.ExecuteNonQuery(createColumnMasterKeySql);

        return new ColumnMasterKey(keyName, certificate.Thumbprint, certificate.NeedsRotation());
    }

    private static async Task<bool> IsColumnEncryptionKeyCreated(this DbConnection connection, ILogger logger)
    {
        logger.LogInformation($"Checking if column encryption key '{ColumnEncryptionKeyName}' exists in the database.");

        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM sys.column_encryption_keys WHERE name = '{ColumnEncryptionKeyName}'";

        var columnEncryptionKeyExists = Convert.ToInt32(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture) > 0;

        return columnEncryptionKeyExists;
    }

    private static async Task CreateColumnEncryptionKey(this DbConnection connection, ICertificateStore certificateStore, ColumnMasterKey columnMasterKey, ILogger logger)
    {
        logger.LogInformation($"Column encryption key '{ColumnEncryptionKeyName}' does not exist. Creating it.");

        // Generate the encrypted value for the column encryption key
        var encryptedValue = GenerateEncryptedValueForColumnEncryptionKey(
            certificateStore.StoreLocation,
            certificateStore.StoreName,
            columnMasterKey.CertificateThumbprint);

        // Create the column encryption key using the column master key
        var createColumnEncryptionKeySql = $@"
        CREATE COLUMN ENCRYPTION KEY [{ColumnEncryptionKeyName}]
        WITH VALUES (
            COLUMN_MASTER_KEY = [{columnMasterKey.KeyName}],
            ALGORITHM = 'RSA_OAEP',
            ENCRYPTED_VALUE = {encryptedValue}
        );";

        await connection.ExecuteNonQuery(createColumnEncryptionKeySql);
    }

    /// <summary>
    /// Generates the enclave signature for the given certificate.
    /// </summary>
    private static string GenerateEnclaveSignature(StoreLocation storeLocation, StoreName storeName, string thumbprint)
    {
        var signature = new SqlColumnEncryptionCertificateStoreProvider().SignColumnMasterKeyMetadata($"{storeLocation}/{storeName}/{thumbprint}", allowEnclaveComputations: true);
        return "0x" + Convert.ToHexString(signature).Replace("-", string.Empty);
    }

    /// <summary>
    /// Generates the encrypted value for the column encryption key.
    /// </summary>
    private static string GenerateEncryptedValueForColumnEncryptionKey(StoreLocation storeLocation, StoreName storeName, string thumbprint)
    {
        var key = RandomNumberGenerator.GetBytes(32);
        var encryptedKey = new SqlColumnEncryptionCertificateStoreProvider().EncryptColumnEncryptionKey($"{storeLocation}/{storeName}/{thumbprint}", "RSA_OAEP", key);
        return "0x" + Convert.ToHexString(encryptedKey).Replace("-", string.Empty);
    }

    internal static async Task RotateColumnMasterKey(this DbConnection connection, ICertificateStore certificateStore, ColumnMasterKey oldColumnMasterKey, ILogger logger)
    {
        logger.LogInformation("Rotating the column master key.");

        var newColumnMasterKey = await connection.CreateColumnMasterKey(certificateStore, forceCreate: true);
        // Generate the encrypted value for the column encryption key
        var newEncryptedValue = GenerateEncryptedValueForColumnEncryptionKey(
            certificateStore.StoreLocation,
            certificateStore.StoreName,
            newColumnMasterKey.CertificateThumbprint);

        var rotateColumnMasterKeySql = $@"
            ALTER COLUMN ENCRYPTION KEY [{ColumnEncryptionKeyName}]
            ADD VALUE (
                COLUMN_MASTER_KEY = [{newColumnMasterKey.KeyName}],
                ALGORITHM = 'RSA_OAEP',
                ENCRYPTED_VALUE = {newEncryptedValue}
            );

            ALTER COLUMN ENCRYPTION KEY [{ColumnEncryptionKeyName}]
            DROP VALUE (
                COLUMN_MASTER_KEY = [{oldColumnMasterKey.KeyName}]
            );

            DROP COLUMN MASTER KEY [{oldColumnMasterKey.KeyName}];";

        await connection.ExecuteNonQuery(rotateColumnMasterKeySql);
    }

    private static async Task ExecuteNonQuery(this DbConnection connection, string commandText)
    {
        await using var transaction = await connection.BeginTransactionAsync();
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
#pragma warning disable CA2100
        command.CommandText = commandText;
#pragma warning restore CA2100
        await command.ExecuteNonQueryAsync();
        await transaction.CommitAsync();
    }

    private static bool NeedsRotation(this X509Certificate2 certificate)
    {
        return certificate.NotAfter < DateTime.Now.Add(_transitionPeriod);
    }
}
