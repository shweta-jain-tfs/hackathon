using System.Security.Cryptography.X509Certificates;

namespace Pharos.Server.Infrastructure;

public interface ICertificateStore
{
    /// <summary>
    /// The location of the store for generated certificates.
    /// </summary>
    StoreLocation StoreLocation { get; }

    /// <summary>
    /// The name of the store for generated certificates.
    /// </summary>
    StoreName StoreName { get; }

    /// <summary>
    /// Ensures that a local Pharos CA key and a default TLS certificate have been created.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Gets the local Pharos CA certificate.
    /// </summary>
    X509Certificate2 GetLocalTrustRoot();

    /// <summary>
    /// Gets the TLS certificate for the specified server name. If a certificate for the specified server name does not exist.
    /// A default TLS certificate will be returned.
    /// </summary>
    X509Certificate2 GetTlsCertificate(string? serverName);

    /// <summary>
    /// Gets the encryption certificate for use with Always Encrypted, if one exists.
    /// </summary>
    X509Certificate2? GetEncryptionCertificate();

    /// <summary>
    /// Creates a new encryption certificate for use with Always Encrypted.
    /// Used when rotating a column master key.
    /// </summary>
    /// <param name="createExpired">If true, creates an expired certificate. Works in XUnit environment only.</param>
    X509Certificate2 CreateEncryptionCertificate(bool createExpired = false);

    /// <summary>
    /// Removes the expired encryption certificate from the local certificate store.
    /// </summary>
    void RemoveExpiredEncryptionCertificate();

    /// <summary>
    /// Gets a certificate from the local certificate store based on the specified search criteria.
    /// </summary>
    X509Certificate2? GetCertificate(StoreLocation storeLocation, StoreName storeName, string thumbprint);

    /// <summary>
    /// Adds a trusted root certificate to the local certificate store.
    /// </summary>
    void AddTrustedRoot(X509Certificate2 certificate);

    /// <summary>
    /// Occurs when a certificate is added to the local certificate store. Used for testing purposes.
    /// </summary>
    event Action<StoreLocation, StoreName, string>? CertificateAdded;
}
