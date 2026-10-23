using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using CustomAction.CustomActionLogic.Interfaces.Database;
using CustomAction.Wrappers.Interfaces;

namespace CustomAction.CustomActionLogic.Database;

public class DbConnectionFactory : IDbConnectionFactory
{
    private readonly ISessionWrapper _session;

    public DbConnectionFactory(ISessionWrapper session)
    {
        _session = session;
    }
    public IDbConnection CreateConnection(string connectionString)
    {
        var sanitized = RemoveAttestationProtocol(connectionString);
        var instanceScoped = ForceMasterDatabase(sanitized);

        _session.Log("Creating SQL connection with connection string: " + instanceScoped);

        return new SqlConnection(instanceScoped);
    }

    // We use System.Data.SqlClient for MSI custom actions because it avoids carrying extra NuGet dependencies
    // (e.g., System.Runtime.CompilerServices.Unsafe) that are problematic for the MSI loader. The keyword
    // "Attestation Protocol" is specific to Microsoft.Data.SqlClient (Always Encrypted with secure enclaves)
    // and is not understood by System.Data.SqlClient. Backup/restore and SQL server configuration do not require client-side
    // enclave attestation, so we strip this key for the custom action connection only.
    private static string RemoveAttestationProtocol(string cs)
    {
        if (string.IsNullOrWhiteSpace(cs))
        {
            return cs;
        }

        var builder = new DbConnectionStringBuilder { ConnectionString = cs };
        builder.Remove("Attestation Protocol");

        return builder.ConnectionString;
    }

    // Force admin operations to connect to the instance context so they work even if the target DB doesn't exist.
    private static string ForceMasterDatabase(string cs)
    {
        if (string.IsNullOrWhiteSpace(cs))
        {
            return cs;
        }

        var builder = new SqlConnectionStringBuilder(cs)
        {
            InitialCatalog = "master"
        };

        return builder.ToString();
    }
}