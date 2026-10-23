using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Pharos.Server.Infrastructure.DataProtection;
using Pharos.Server.Infrastructure.Settings;
using Quartz;

namespace Pharos.Server.Infrastructure.Jobs;

[DisallowConcurrentExecution]
internal class ColumnMasterKeyRotationJob(
    IConfiguration configuration,
    ICertificateStore certificateStore,
    ILogger<ColumnMasterKeyRotationJob> logger) : IJob
{
    public static JobKey Key()
    {
        return new JobKey("column-master-key-rotation-key");
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            logger.LogInformation("Column master key rotation job started.");
            var dataProtectionSettings = new DataProtectionSettings();
            configuration.GetSection(DataProtectionSettings.DataProtectionSection).Bind(dataProtectionSettings);
            if (!dataProtectionSettings.EnableDataProtection)
            {
                return;
            }

            var connectionStrings = configuration.GetSection("ConnectionStrings")
                .GetChildren()
                .Select(section => section.Value)
                .Where(connectionString => !string.IsNullOrWhiteSpace(connectionString))
                .Distinct(StringComparer.Ordinal);

            foreach (var connectionString in connectionStrings)
            {
                var builder = new SqlConnectionStringBuilder(connectionString);
                if (builder.ColumnEncryptionSetting != SqlConnectionColumnEncryptionSetting.Enabled)
                {
                    continue;
                }

                await CheckAndRotateColumnMasterKey(connectionString);
            }

            // we only remove expired encryption certificates after all column master keys have been rotated,
            // to avoid removing a certificate that is still in use.
            certificateStore.RemoveExpiredEncryptionCertificate();
            logger.LogInformation("Column master key rotation job completed.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Column master key rotation job failed during execution.");
        }
    }

    protected virtual async Task CheckAndRotateColumnMasterKey(string? connectionString)
    {
        await using var connection = new SqlConnection(connectionString!);
        await connection.OpenAsync();

        var columnMasterKey = await connection.GetColumnMasterKey(certificateStore, logger);
        if (columnMasterKey?.RequiresRotation == true)
        {
            await connection.RotateColumnMasterKey(certificateStore, columnMasterKey, logger);
        }
    }
}
