using Microsoft.Extensions.Configuration;
using Pharos.Server.Infrastructure.Settings;

namespace Pharos.Server.Infrastructure.Database;

public static class ConfigurationExtensions
{
    public static string? GetConnectionString(this IConfiguration configuration)
    {

        var connectionStringName = configuration.GetValue<string>(DatabaseSettings.ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionStringName))
        {
            connectionStringName = DatabaseSettings.MainConnectionStringName;
        }
        return configuration.GetConnectionString(connectionStringName);
    }

    public static string? GetAuditTrailConnectionString(this IConfiguration configuration)
    {
        var connectionStringName = configuration.GetValue<string>(DatabaseSettings.ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionStringName))
        {
            connectionStringName = DatabaseSettings.AuditConnectionStringName;
        }
        return configuration.GetConnectionString(connectionStringName);
    }
}
