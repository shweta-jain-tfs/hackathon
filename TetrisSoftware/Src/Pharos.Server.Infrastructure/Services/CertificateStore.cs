using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Pharos.Server.Infrastructure.Settings;

namespace Pharos.Server.Infrastructure.Services;

internal class CertificateStore(IHostEnvironment environment, IOptions<CertificateSettings> options, IDateTime dateTime) : ICertificateStore
{
    private const string _encryptionMasterKeyCommonName = "Phadia E Column Encryption Master Key";
    public const string LocalTrustRootCommonName = "Phadia E Operator Software CA";
    public const string Organization = "Thermo Fisher Scientific";
    private const string _subjectAlternativeNameOid = "2.5.29.17";

    private bool _initialized;

    private readonly Lock _lock = new();
    private readonly Dictionary<string, X509Certificate2> _tlsCertificates = new();

    private string _defaultServerName = "localhost";

    /// <summary>
    /// Testing purposes only: a suffix to append to the certificate name when generating certificates.
    /// This allows for multiple certificates with the same common name to exist in the store.
    /// </summary>
    internal string CertificateSuffix { get; init; } = string.Empty;

    public StoreLocation StoreLocation { get; } = environment.IsProduction() ? StoreLocation.LocalMachine : StoreLocation.CurrentUser;

    public StoreName StoreName => StoreName.My;

    public event Action<StoreLocation, StoreName, string>? CertificateAdded;

    /// <summary>
    /// We won't add certificates to the root when running automated tests,
    /// because it triggers the Windows trusted root certificate warning (UI dialog).
    /// But it's ok to ask the use while debugging. This is why we do not use environment.IsProduction().
    /// </summary>
    private StoreName RootStore => environment.IsEnvironment("XUnit") ? StoreName.My : StoreName.Root;

    public string EncryptionMasterKeyCommonName => GetCertificateName(_encryptionMasterKeyCommonName);

    public void Initialize()
    {
        lock (_lock)
        {
            if (GetLocalTrustRootInternal() == null)
            {
                CreateLocalTrustRoot();
            }

            var settingValue = options.Value.DefaultServerName;
            if (!string.IsNullOrWhiteSpace(settingValue))
            {
#pragma warning disable CA1308
                _defaultServerName = settingValue.ToLowerInvariant();
#pragma warning restore CA1308
            }

            GetDefaultTlsCertificate();

            _initialized = true;
        }
    }

    public X509Certificate2 GetLocalTrustRoot()
    {
        var certificate = GetLocalTrustRootInternal();
        if (certificate == null)
        {
            throw new InvalidOperationException("The local trust root certificate could not be found.");
        }
        return certificate;
    }

    public X509Certificate2 GetTlsCertificate(string? serverName)
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("Initialize method must be called before getting a TLS certificate.");
        }

        lock (_lock)
        {
#pragma warning disable CA1308
            serverName = string.IsNullOrWhiteSpace(serverName) ? _defaultServerName : serverName.ToLowerInvariant();
#pragma warning restore CA1308

            if (serverName == _defaultServerName)
            {
                return GetDefaultTlsCertificate();
            }

            // try cache first
            if (_tlsCertificates.TryGetValue(serverName, out var certificate))
            {
                if (IsUsable(certificate))
                {
                    return certificate;
                }

                // we do not return the default cert, maybe there is a new on in the store
                _tlsCertificates.Remove(serverName);
                certificate.Dispose();
            }

            // try to find in store
            certificate = FindTlsCertificate(serverName) ?? FindWildcardTlsCertificate(serverName);
            if (certificate != null)
            {
                _tlsCertificates[serverName] = certificate;
                return certificate;
            }

            return GetDefaultTlsCertificate();
        }
    }

    public X509Certificate2? GetEncryptionCertificate()
    {
        using var store = new X509Store(StoreName, StoreLocation);
        store.Open(OpenFlags.ReadOnly);

        var certificate = store.Certificates
            .Find(X509FindType.FindBySubjectName, EncryptionMasterKeyCommonName, false)
            .Where(candidate => candidate.HasPrivateKey)
            .Where(candidate => string.Equals(
                candidate.GetNameInfo(X509NameType.SimpleName, false),
                EncryptionMasterKeyCommonName,
                StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(candidate => candidate.NotAfter)
            .FirstOrDefault();

        return certificate;
    }

    public X509Certificate2 CreateEncryptionCertificate(bool createExpired = false)
    {
        var distinguishedName = new X500DistinguishedName($"CN={EncryptionMasterKeyCommonName}, O={Organization}");

        // Use RSA with CNG provider
        using var rsa = RSA.Create(2048);

        var certificateRequest = new CertificateRequest(distinguishedName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        // Add Subject Key Identifier extension
        certificateRequest.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(certificateRequest.PublicKey, false));

        // Set certificate properties
        certificateRequest.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyEncipherment, true));
        certificateRequest.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
            [
                new Oid("1.3.6.1.5.5.8.2.2"), // IP security IKE intermediate
                new Oid("1.3.6.1.4.1.311.10.3.11")
            ],
            false));


        // Create the self-signed certificate
        X509Certificate2 certificate;
        if (createExpired && environment.IsEnvironment("XUnit"))
        {
            certificate = certificateRequest.CreateSelfSigned(dateTime.Now.AddDays(-2), dateTime.Now.AddDays(-1));
        }
        else
        {
            certificate = certificateRequest.CreateSelfSigned(dateTime.Now.AddDays(-1), dateTime.Now.AddDays(options.Value.ColumnMasterKeyLifetimeDays));
        }

        // Export the certificate with the private key and re-import it to ensure the private key is included
        var result = X509CertificateLoader.LoadPkcs12(
            certificate.Export(X509ContentType.Pfx),
            null,
            X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);

        AddToStore(result);

        return result;
    }

    public void RemoveExpiredEncryptionCertificate()
    {
        using var store = new X509Store(StoreName, StoreLocation);
        store.Open(OpenFlags.ReadWrite);
        var expiredCertificates = store.Certificates
            .Find(X509FindType.FindBySubjectName, EncryptionMasterKeyCommonName, false)
            .Where(certificate => certificate.NotAfter < dateTime.Now)
            .ToList();

        foreach (var certificate in expiredCertificates)
        {
            store.Remove(certificate);
        }
    }

    public X509Certificate2? GetCertificate(StoreLocation storeLocation, StoreName storeName, string thumbprint)
    {
        using var store = new X509Store(storeName, storeLocation);
        store.Open(OpenFlags.ReadOnly);
        return store.Certificates
            .Find(X509FindType.FindByThumbprint, thumbprint, false)
            .FirstOrDefault();
    }

    public void AddTrustedRoot(X509Certificate2 certificate)
    {
        AddToStore(certificate, root: true);
    }

    private void AddToStore(X509Certificate2 certificate, bool root = false)
    {
        var storeName = root ? RootStore : StoreName;

#pragma warning disable CA5380
        using var store = new X509Store(storeName, StoreLocation);
        store.Open(OpenFlags.ReadWrite);
        store.Add(certificate);
#pragma warning restore CA5380
        CertificateAdded?.Invoke(StoreLocation, storeName, certificate.Thumbprint);
    }

    public X509Certificate2? GetLocalTrustRootInternal()
    {
        using var store = new X509Store(RootStore, StoreLocation);
        store.Open(OpenFlags.ReadOnly);
        var certificate = store.Certificates
            .Find(X509FindType.FindBySubjectName, GetCertificateName(LocalTrustRootCommonName), false)
            .FirstOrDefault(IsUsable);
        store.Close();
        return certificate;
    }

    private X509Certificate2 CreateTlsCertificate(string serverName)
    {
        using var rootCertificate = GetLocalTrustRoot();
        using var rsa = RSA.Create(2048);

        var subject = new X500DistinguishedName($"CN={GetCertificateName(serverName)}, O={Organization}");
        var request = new CertificateRequest(subject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var subjectAlternativeNames = new SubjectAlternativeNameBuilder();
        subjectAlternativeNames.AddDnsName(serverName);
        if (!string.Equals(serverName, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            subjectAlternativeNames.AddDnsName("localhost");
        }
        subjectAlternativeNames.AddIpAddress(IPAddress.Loopback);
        subjectAlternativeNames.AddIpAddress(IPAddress.IPv6Loopback);
        request.CertificateExtensions.Add(subjectAlternativeNames.Build());
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
            [
                new Oid("1.3.6.1.5.5.7.3.1"), // server authentication
                new Oid("1.3.6.1.5.5.7.3.2")  // client authentication
            ],
            false));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

        var now = dateTime.Now;
        using var generatedCertificate = request.Create(rootCertificate, now.AddDays(-1), now.AddYears(1), RandomNumberGenerator.GetBytes(16));
        var certificate = X509CertificateLoader.LoadPkcs12(
            generatedCertificate.CopyWithPrivateKey(rsa).Export(X509ContentType.Pfx),
            null,
            X509KeyStorageFlags.PersistKeySet);

        AddToStore(certificate);
        return certificate;
    }

    private void CreateLocalTrustRoot()
    {
#pragma warning disable CA1416
        var keyCreationParameters = new CngKeyCreationParameters
        {
            Provider = options.Value.UseTpm
                ? new CngProvider("Microsoft Platform Crypto Provider")
                : new CngProvider("Microsoft Software Key Storage Provider"),
            KeyUsage = CngKeyUsages.Signing,
            ExportPolicy = CngExportPolicies.None
        };

        var keyOpenOptions = CngKeyOpenOptions.Silent;
        if (environment.IsProduction())
        {
            keyCreationParameters.KeyCreationOptions = CngKeyCreationOptions.MachineKey;
            keyOpenOptions |= CngKeyOpenOptions.MachineKey;
        }

        using var key = CngKey.Exists("PharosRootCa", keyCreationParameters.Provider, keyOpenOptions)
            ? CngKey.Open("PharosRootCa", keyCreationParameters.Provider, keyOpenOptions)
            : CngKey.Create(CngAlgorithm.Rsa, "PharosRootCa", keyCreationParameters);
        using var rsa = new RSACng(key);
#pragma warning restore CA1416

        var subject = new X500DistinguishedName($"CN={GetCertificateName(LocalTrustRootCommonName)}, O={Organization}");
        var request = new CertificateRequest(subject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, true, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, true));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

        var certificate = request.CreateSelfSigned(dateTime.Now.AddDays(-1), dateTime.Now.AddYears(30));
        AddToStore(certificate, root: true);
    }

    private X509Certificate2? FindTlsCertificate(string serverName)
    {
        var requireLocalIssuer = string.Equals(serverName, _defaultServerName, StringComparison.OrdinalIgnoreCase);

        using var store = new X509Store(StoreName, StoreLocation);
        store.Open(OpenFlags.ReadOnly);

        var commonNameCertificate = store.Certificates
            .Find(X509FindType.FindBySubjectName, GetCertificateName(serverName), false)
            .FirstOrDefault(certificate =>
                IsUsable(certificate) &&
                (!requireLocalIssuer || IsIssuedByLocalTrustRoot(certificate)) &&
                string.Equals(certificate.GetNameInfo(X509NameType.SimpleName, false), GetCertificateName(serverName), StringComparison.OrdinalIgnoreCase));
        if (commonNameCertificate != null)
        {
            return commonNameCertificate;
        }

        return store.Certificates
            .Find(X509FindType.FindByExtension, _subjectAlternativeNameOid, false)
            .FirstOrDefault(certificate =>
                IsUsable(certificate) &&
                (!requireLocalIssuer || IsIssuedByLocalTrustRoot(certificate)) &&
                certificate.Extensions[_subjectAlternativeNameOid] is { } extension &&
                new X509SubjectAlternativeNameExtension(extension.RawData, extension.Critical)
                    .EnumerateDnsNames()
                    .Any(dnsName => string.Equals(dnsName, serverName, StringComparison.OrdinalIgnoreCase)));
    }

    private X509Certificate2? FindWildcardTlsCertificate(string serverName)
    {
        if (!options.Value.AllowWildcardCertificates)
        {
            return null;
        }

        var firstLabelSeparator = serverName.IndexOf('.');
        if (firstLabelSeparator <= 0 || firstLabelSeparator == serverName.Length - 1)
        {
            return null;
        }

        return FindTlsCertificate($"*{serverName[firstLabelSeparator..]}");
    }

    private X509Certificate2 GetDefaultTlsCertificate()
    {
        if (_tlsCertificates.TryGetValue(_defaultServerName, out var certificate))
        {
            if (IsUsable(certificate))
            {
                return certificate;
            }

            using var store = new X509Store(StoreName, StoreLocation);
            store.Open(OpenFlags.ReadWrite);
            store.Remove(certificate);
            certificate.Dispose();
            _tlsCertificates.Remove(_defaultServerName);
            certificate = null;
        }
        else
        {
            certificate = FindTlsCertificate(_defaultServerName);
        }

        certificate ??= CreateTlsCertificate(_defaultServerName);
        _tlsCertificates[_defaultServerName] = certificate;
        return certificate;
    }

    private bool IsUsable(X509Certificate2 certificate)
    {
        return certificate.HasPrivateKey &&
               certificate.NotBefore <= dateTime.Now &&
               certificate.NotAfter > dateTime.Now;
    }

    private bool IsIssuedByLocalTrustRoot(X509Certificate2 certificate)
    {
        var issuer = new X500DistinguishedName($"CN={GetCertificateName(LocalTrustRootCommonName)}, O={Organization}");
        return certificate.IssuerName.RawData.AsSpan().SequenceEqual(issuer.RawData);
    }

    private string GetCertificateName(string certificateName)
    {
        return string.IsNullOrEmpty(CertificateSuffix)
            ? certificateName
            : $"{certificateName} {CertificateSuffix}";
    }
}
