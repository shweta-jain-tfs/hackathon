using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using Pharos.Server.Infrastructure.Services;
using Pharos.Server.Infrastructure.Settings;

namespace Pharos.Server.Infrastructure.Tests.Services;

[CollectionDefinition(nameof(CertificateStoreCollection), DisableParallelization = true)]
public sealed class CertificateStoreCollection;

[Collection(nameof(CertificateStoreCollection))]
public sealed class CertificateStoreTests : IAsyncLifetime
{
    private const string _certificateSuffix = "XUnit";
    private readonly List<(StoreLocation StoreLocation, StoreName StoreName, string Thumbprint)> _addedCertificates = [];
    private readonly TestDateTime _dateTime = new(DateTimeOffset.Now);
    private readonly Mock<IHostEnvironment> _environment = new();
    private string _defaultServerName = null!;
    private CertificateStore _store = null!;

    public ValueTask InitializeAsync()
    {
        _environment.SetupGet(environment => environment.EnvironmentName).Returns("XUnit");
        _defaultServerName = $"operator-{Guid.NewGuid():N}.pharos.test";
        _store = CreateStore(_defaultServerName, useTpm: false);
        _store.CertificateAdded += OnCertificateAdded;
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        foreach (var (storeLocation, storeName, thumbprint) in _addedCertificates)
        {
            RemoveCertificate(storeLocation, storeName, thumbprint);
        }

        return ValueTask.CompletedTask;
    }

    [Fact]
    public void Initialize_CreatesLocalTrustRoot_WithRequiredProperties()
    {
        // Act
        _store.Initialize();

        // Assert
        using var certificate = _store.GetLocalTrustRoot();
        certificate.Subject.Should().Be($"CN={GetCertificateName(CertificateStore.LocalTrustRootCommonName)}, O={CertificateStore.Organization}");

        var basicConstraints = certificate.Extensions.OfType<X509BasicConstraintsExtension>().Single();
        basicConstraints.CertificateAuthority.Should().BeTrue();
        basicConstraints.HasPathLengthConstraint.Should().BeTrue();
        basicConstraints.PathLengthConstraint.Should().Be(0);
        basicConstraints.Critical.Should().BeTrue();

        var keyUsage = certificate.Extensions.OfType<X509KeyUsageExtension>().Single();
        keyUsage.KeyUsages.Should().Be(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign);
        keyUsage.Critical.Should().BeTrue();
        certificate.Extensions.OfType<X509EnhancedKeyUsageExtension>().Should().BeEmpty();

        using var rsa = certificate.GetRSAPrivateKey();
        using var key = rsa.Should().BeOfType<RSACng>().Subject.Key;
        key.Provider!.Provider.Should().Be("Microsoft Software Key Storage Provider");
        key.IsMachineKey.Should().BeFalse();
        key.ExportPolicy.Should().Be(CngExportPolicies.None);
    }

    [Fact]
    public void Initialize_CreatesLocalTrustRootInProduction_ThrowsTpmException()
    {
        // Arrange
        _environment.SetupGet(environment => environment.EnvironmentName).Returns(Environments.Production);
        var productionStore = CreateStore($"operator-{Guid.NewGuid():N}.pharos.test", useTpm: true);
        productionStore.GetLocalTrustRootInternal().Should().BeNull("a matching root certificate indicates the local machine certificate store was polluted before this test");

        // Act
        var initialize = productionStore.Initialize;

        // Assert
        initialize.Should().Throw<CryptographicException>()
            .Which.HResult.Should().BeOneOf(
                unchecked((int)0x80090010), // NTE_PERM
                unchecked((int)0x80090030)); // NTE_DEVICE_NOT_READY
    }

    [Fact]
    public void Initialize_CreatesDefaultTlsCertificate_ForConfiguredServerName()
    {
        var serverName = _defaultServerName;

        // Act
        _store.Initialize();

        // Assert
        using var certificate = _store.GetTlsCertificate(null);
        certificate.GetNameInfo(X509NameType.SimpleName, false).Should().Be(GetCertificateName(serverName));
        certificate.Subject.Should().Be($"CN={GetCertificateName(serverName)}, O={CertificateStore.Organization}");
        certificate.Issuer.Should().Be($"CN={GetCertificateName(CertificateStore.LocalTrustRootCommonName)}, O={CertificateStore.Organization}");
        certificate.NotBefore.Should().BeOnOrBefore(_dateTime.Now.LocalDateTime);
        certificate.NotAfter.Should().BeAfter(_dateTime.Now.LocalDateTime.AddMonths(11));
        certificate.HasPrivateKey.Should().BeTrue();

        var subjectAlternativeName = certificate.Extensions["2.5.29.17"];
        subjectAlternativeName.Should().NotBeNull();
        var subjectAlternativeNames = new X509SubjectAlternativeNameExtension(subjectAlternativeName.RawData, subjectAlternativeName.Critical);
        subjectAlternativeNames.EnumerateDnsNames()
            .Should()
            .BeEquivalentTo([serverName, "localhost"]);
        subjectAlternativeNames.EnumerateIPAddresses()
            .Should()
            .BeEquivalentTo([IPAddress.Loopback, IPAddress.IPv6Loopback]);

        var enhancedKeyUsage = certificate.Extensions.OfType<X509EnhancedKeyUsageExtension>().Single();
        enhancedKeyUsage.EnhancedKeyUsages.Cast<Oid>()
            .Select(oid => oid.Value)
            .Should()
            .BeEquivalentTo(["1.3.6.1.5.5.7.3.1", "1.3.6.1.5.5.7.3.2"]);
    }

    [Fact]
    public void GetTlsCertificate_ThrowsBeforeInitialization()
    {
        // Act
        var getCertificate = () => _store.GetTlsCertificate(null);

        // Assert
        getCertificate.Should().Throw<InvalidOperationException>()
            .WithMessage("Initialize method must be called before getting a TLS certificate.");
    }

    [Fact]
    public void GetTlsCertificate_ReturnsCachedDefaultCertificate_ForConfiguredServerName()
    {
        // Arrange
        _store.Initialize();
        var defaultCertificate = _store.GetTlsCertificate(null);

        // Act
        var cachedCertificate = _store.GetTlsCertificate(_defaultServerName.ToUpperInvariant());

        // Assert
        cachedCertificate.Should().BeSameAs(defaultCertificate);
    }

    [Fact]
    public void GetTlsCertificate_ReturnsDefaultCertificate_WhenRequestedCertificateIsMissing()
    {
        // Arrange
        _store.Initialize();
        var defaultCertificate = _store.GetTlsCertificate(null);

        // Act
        var certificate = _store.GetTlsCertificate($"missing-{Guid.NewGuid():N}.pharos.test");

        // Assert
        certificate.Should().BeSameAs(defaultCertificate);
    }

    [Fact]
    public void GetTlsCertificate_ReturnsCustomCertificate_WhenCertificateExistsInStore()
    {
        // Arrange
        _store.Initialize();
        var serverName = $"custom-{Guid.NewGuid():N}.pharos.test";
        using var certificate = CreateTlsCertificate(serverName);
        AddCertificateToStore(certificate, _store.StoreLocation, _store.StoreName);

        // Act
        using var result1 = _store.GetTlsCertificate(serverName);
        var result2 = _store.GetTlsCertificate(serverName);

        // Assert
        result1.Thumbprint.Should().Be(certificate.Thumbprint);
        result2.Should().BeSameAs(result1);
    }

    [Fact]
    public void GetTlsCertificate_ReturnsThirdPartyCertificate_WhenCertificateExistsInStore()
    {
        // Arrange
        _store.Initialize();
        var serverName = $"third-party-{Guid.NewGuid():N}.pharos.test";
        using var certificate = CreateThirdPartyTlsCertificate(serverName);
        AddCertificateToStore(certificate, _store.StoreLocation, _store.StoreName);

        // Act
        using var result = _store.GetTlsCertificate(serverName);

        // Assert
        result.Thumbprint.Should().Be(certificate.Thumbprint);
    }

    [Fact]
    public void GetTlsCertificate_ReturnsWildcardCertificate_WhenEnabled()
    {
        // Arrange
        _store = CreateStore(_defaultServerName, allowWildcardCertificates: true);
        _store.CertificateAdded += OnCertificateAdded;
        _store.Initialize();
        var serverName1 = $"custom-{Guid.NewGuid():N}.pharos.test";
        var serverName2 = $"custom-{Guid.NewGuid():N}.pharos.test";
        using var certificate = CreateTlsCertificate("*.pharos.test");
        AddCertificateToStore(certificate, _store.StoreLocation, _store.StoreName);

        // Act
        using var result1 = _store.GetTlsCertificate(serverName1);
        using var result2 = _store.GetTlsCertificate(serverName2);

        // Assert
        result1.Thumbprint.Should().Be(certificate.Thumbprint);
        result2.Thumbprint.Should().Be(certificate.Thumbprint);
    }

    [Fact]
    public void GetTlsCertificate_DoesNotUseWildcardCertificate_WhenDisabled()
    {
        // Arrange
        _store.Initialize();
        var defaultCertificate = _store.GetTlsCertificate(null);
        var serverName = $"custom-{Guid.NewGuid():N}.pharos.test";
        using var wildcardCertificate = CreateTlsCertificate("*.pharos.test");
        AddCertificateToStore(wildcardCertificate, _store.StoreLocation, _store.StoreName);

        // Act
        var result = _store.GetTlsCertificate(serverName);

        // Assert
        result.Should().BeSameAs(defaultCertificate);
    }

    [Fact]
    public void GetTlsCertificate_DoesNotUseWildcardCertificate_ForMultipleSubdomains()
    {
        // Arrange
        _store = CreateStore(_defaultServerName, allowWildcardCertificates: true);
        _store.CertificateAdded += OnCertificateAdded;
        _store.Initialize();
        var defaultCertificate = _store.GetTlsCertificate(null);
        using var wildcardCertificate = CreateTlsCertificate("*.pharos.test");
        AddCertificateToStore(wildcardCertificate, _store.StoreLocation, _store.StoreName);

        // Act
        var result = _store.GetTlsCertificate($"custom.nested-{Guid.NewGuid():N}.pharos.test");

        // Assert
        result.Should().BeSameAs(defaultCertificate);
    }

    [Fact]
    public void GetTlsCertificate_IgnoresAspNetCoreDevelopmentCertificate()
    {
        // Arrange
        _store = CreateStore("localhost");
        _store.CertificateAdded += OnCertificateAdded;
        using var developmentCertificate = CreateAspNetCoreDevelopmentCertificate();
        AddCertificateToStore(developmentCertificate, _store.StoreLocation, _store.StoreName);
        _store.Initialize();

        // Act
        using var certificate = _store.GetTlsCertificate("localhost");

        // Assert
        certificate.Thumbprint.Should().NotBe(developmentCertificate.Thumbprint);
        certificate.Issuer.Should().Be($"CN={GetCertificateName(CertificateStore.LocalTrustRootCommonName)}, O={CertificateStore.Organization}");
    }

    [Fact]
    public void GetEncryptionCertificate_ReturnsNull_WhenNoCertificateExists()
    {
        // Act
        using var certificate = _store.GetEncryptionCertificate();

        // Assert
        certificate.Should().BeNull();
    }

    [Fact]
    public void GetEncryptionCertificate_ReusesExistingUsableCertificate()
    {
        // Arrange
        using var createdCertificate = _store.CreateEncryptionCertificate();

        // Act
        using var firstCertificate = _store.GetEncryptionCertificate();
        using var secondCertificate = _store.GetEncryptionCertificate();

        // Assert
        firstCertificate.Should().NotBeNull();
        secondCertificate.Should().NotBeNull();
        secondCertificate.Thumbprint.Should().Be(firstCertificate.Thumbprint);
        _addedCertificates.Should().ContainSingle();
    }

    [Fact]
    public void CreateEncryptionCertificate_CreatesExportableCertificate()
    {
        // Act
        using var certificate = _store.CreateEncryptionCertificate();

        // Assert
        certificate.Subject.Should().Be($"CN={GetCertificateName("Phadia E Column Encryption Master Key")}, O={CertificateStore.Organization}");
        certificate.HasPrivateKey.Should().BeTrue();
        // ReSharper disable once AccessToDisposedClosure
        var export = () => certificate.Export(X509ContentType.Pfx);
        export.Should().NotThrow();
    }

    [Fact]
    public void RemoveExpiredEncryptionCertificate_RemovesExpiredEncryptionCertificate()
    {
        // Arrange
        using var certificate = _store.CreateEncryptionCertificate(createExpired: true);
        _store.GetCertificate(_store.StoreLocation, _store.StoreName, certificate.Thumbprint).Should().NotBeNull();

        // Act
        _store.RemoveExpiredEncryptionCertificate();

        // Assert
        _store.GetCertificate(_store.StoreLocation, _store.StoreName, certificate.Thumbprint).Should().BeNull();
    }

    [Fact]
    public void GetTlsCertificate_ReplacesExpiredCachedDefaultCertificate()
    {
        // Arrange
        _store.Initialize();
        using var originalCertificate = _store.GetTlsCertificate(null);
        var originalThumbprint = originalCertificate.Thumbprint;
        _dateTime.Now = _dateTime.Now.AddYears(2);

        // Act
        using var replacementCertificate = _store.GetTlsCertificate(null);

        // Assert
        replacementCertificate.Thumbprint.Should().NotBe(originalThumbprint);
        replacementCertificate.NotAfter.Should().BeAfter(_dateTime.Now.LocalDateTime);
    }

    private CertificateStore CreateStore(string defaultServerName, bool useTpm = false, bool allowWildcardCertificates = false)
    {
        return new CertificateStore(
            _environment.Object,
            Options.Create(new CertificateSettings
            {
                DefaultServerName = defaultServerName,
                UseTpm = useTpm,
                AllowWildcardCertificates = allowWildcardCertificates
            }),
            _dateTime)
        {
            CertificateSuffix = _certificateSuffix
        };
    }

    private void OnCertificateAdded(StoreLocation storeLocation, StoreName storeName, string thumbprint)
    {
        _addedCertificates.Add((storeLocation, storeName, thumbprint));
    }

    private X509Certificate2 CreateTlsCertificate(string serverName)
    {
        using var rootCertificate = _store.GetLocalTrustRoot();
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            new X500DistinguishedName($"CN={GetCertificateName(serverName)}, O={CertificateStore.Organization}"),
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));

        using var generatedCertificate = request.Create(
            rootCertificate,
            _dateTime.Now.AddMinutes(-5),
            _dateTime.Now.AddYears(1),
            RandomNumberGenerator.GetBytes(16));
        return X509CertificateLoader.LoadPkcs12(
            generatedCertificate.CopyWithPrivateKey(rsa).Export(X509ContentType.Pfx),
            null,
            X509KeyStorageFlags.PersistKeySet);
    }

    private X509Certificate2 CreateAspNetCoreDevelopmentCertificate()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            $"CN={GetCertificateName("localhost")}",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        using var certificate = request.CreateSelfSigned(_dateTime.Now.AddDays(-1), _dateTime.Now.AddYears(1));
        return X509CertificateLoader.LoadPkcs12(
            certificate.Export(X509ContentType.Pfx),
            null,
            X509KeyStorageFlags.PersistKeySet);
    }

    private X509Certificate2 CreateThirdPartyTlsCertificate(string serverName)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            new X500DistinguishedName($"CN={GetCertificateName(serverName)}, O=Third Party"),
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        using var certificate = request.CreateSelfSigned(_dateTime.Now.AddDays(-1), _dateTime.Now.AddYears(1));
        return X509CertificateLoader.LoadPkcs12(
            certificate.Export(X509ContentType.Pfx),
            null,
            X509KeyStorageFlags.PersistKeySet);
    }

    private void AddCertificateToStore(X509Certificate2 certificate, StoreLocation storeLocation, StoreName storeName)
    {
        using var store = new X509Store(storeName, storeLocation);
        store.Open(OpenFlags.ReadWrite);
        store.Add(certificate);
        OnCertificateAdded(storeLocation, storeName, certificate.Thumbprint);
    }

    private static void RemoveCertificate(StoreLocation storeLocation, StoreName storeName, string thumbprint)
    {
        using var store = new X509Store(storeName, storeLocation);
        store.Open(OpenFlags.ReadWrite);
        foreach (var certificate in store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false))
        {
            store.Remove(certificate);
        }
    }

    private static string GetCertificateName(string certificateName)
    {
        return $"{certificateName} {_certificateSuffix}";
    }

    private sealed class TestDateTime(DateTimeOffset now) : IDateTime
    {
        public DateTimeOffset UtcNow => Now.ToUniversalTime();

        public DateTimeOffset Now { get; set; } = now;
    }
}
