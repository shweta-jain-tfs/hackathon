using Microsoft.EntityFrameworkCore;

namespace Pharos.Server.Infrastructure.Services;

/// <summary>
/// A database encryption service.
/// </summary>
public interface IDbEncryptionService
{
    /// <summary>
    /// Enables "Always Encrypted with secure enclaves" SQL Server feature the specified <see cref="DbContext"/> object.
    /// Encryption is enabled for all entity properties marked with ProtectedDataAttribute (entities in the context).
    /// </summary>
    Task UseAlwaysEncryptedWithSecureEnclaves(DbContext context, string? schemaName = null);
}
