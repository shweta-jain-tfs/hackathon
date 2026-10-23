using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Pharos.Server.Infrastructure.Jobs;
using Pharos.Server.Infrastructure.Settings;
using Quartz;

namespace Pharos.Server.Infrastructure.Tests.Jobs;

public sealed class ColumnMasterKeyRotationJobTests
{
    [Fact]
    public async Task Execute_WhenDataProtectionIsDisabled_DoesNotRemoveCertificates()
    {
        // Arrange
        var certificateStore = new Mock<ICertificateStore>();
        var sut = CreateJob(certificateStore, new Dictionary<string, string?>
        {
            [$"{DataProtectionSettings.DataProtectionSection}:EnableDataProtection"] = "false"
        });

        // Act
        await sut.Execute(Mock.Of<IJobExecutionContext>());

        // Assert
        sut.CheckedConnectionStrings.Should().BeEmpty();
        certificateStore.Verify(store => store.RemoveExpiredEncryptionCertificate(), Times.Never);
    }

    [Fact]
    public async Task Execute_WhenOnlyUnencryptedConnectionsAreConfigured_SkipsRotationAndRemovesExpiredCertificates()
    {
        // Arrange
        var certificateStore = new Mock<ICertificateStore>();
        var sut = CreateJob(certificateStore, new Dictionary<string, string?>
        {
            [$"{DataProtectionSettings.DataProtectionSection}:EnableDataProtection"] = "true",
            ["ConnectionStrings:Audit"] = "Server=not-used;Database=AuditTrail;Trusted_Connection=True;"
        });

        // Act
        await sut.Execute(Mock.Of<IJobExecutionContext>());

        // Assert
        sut.CheckedConnectionStrings.Should().BeEmpty();
        certificateStore.Verify(store => store.RemoveExpiredEncryptionCertificate(), Times.Once);
    }

    [Fact]
    public async Task Execute_WhenEncryptedConnectionsAreConfigured_ChecksEachDistinctConnectionBeforeCleanup()
    {
        // Arrange
        var certificateStore = new Mock<ICertificateStore>();
        var checkedConnectionCountAtCleanup = -1;
        var sut = CreateJob(certificateStore, new Dictionary<string, string?>
        {
            [$"{DataProtectionSettings.DataProtectionSection}:EnableDataProtection"] = "true",
            ["ConnectionStrings:Default"] = "Server=first;Database=Pharos;Column Encryption Setting=Enabled;",
            ["ConnectionStrings:Duplicate"] = "Server=first;Database=Pharos;Column Encryption Setting=Enabled;",
            ["ConnectionStrings:Other"] = "Server=second;Database=AuditTrail;Column Encryption Setting=Enabled;",
            ["ConnectionStrings:Unencrypted"] = "Server=not-used;Database=Unencrypted;"
        });
        certificateStore
            .Setup(store => store.RemoveExpiredEncryptionCertificate())
            .Callback(() => checkedConnectionCountAtCleanup = sut.CheckedConnectionStrings.Count);

        // Act
        await sut.Execute(Mock.Of<IJobExecutionContext>());

        // Assert
        sut.CheckedConnectionStrings.Should().BeEquivalentTo(
            "Server=first;Database=Pharos;Column Encryption Setting=Enabled;",
            "Server=second;Database=AuditTrail;Column Encryption Setting=Enabled;");
        checkedConnectionCountAtCleanup.Should().Be(2);
        certificateStore.Verify(store => store.RemoveExpiredEncryptionCertificate(), Times.Once);
    }

    [Fact]
    public async Task Execute_WhenCertificateCleanupFails_DoesNotPropagateTheException()
    {
        // Arrange
        var certificateStore = new Mock<ICertificateStore>();
        certificateStore
            .Setup(store => store.RemoveExpiredEncryptionCertificate())
            .Throws(new InvalidOperationException("Unexpected failure"));
        var sut = CreateJob(certificateStore, new Dictionary<string, string?>
        {
            [$"{DataProtectionSettings.DataProtectionSection}:EnableDataProtection"] = "true"
        });

        // Act
        await sut.Execute(Mock.Of<IJobExecutionContext>());
    }

    [Fact]
    public void Key_ReturnsStableQuartzJobKey()
    {
        ColumnMasterKeyRotationJob.Key().Should().Be(new JobKey("column-master-key-rotation-key"));
    }

    private static TestColumnMasterKeyRotationJob CreateJob(
        Mock<ICertificateStore> certificateStore,
        Dictionary<string, string?> values)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        return new TestColumnMasterKeyRotationJob(
            configuration,
            certificateStore.Object);
    }

    private sealed class TestColumnMasterKeyRotationJob(
        IConfiguration configuration,
        ICertificateStore certificateStore) : ColumnMasterKeyRotationJob(
            configuration,
            certificateStore,
            NullLogger<ColumnMasterKeyRotationJob>.Instance)
    {
        public List<string?> CheckedConnectionStrings { get; } = [];

        protected override Task CheckAndRotateColumnMasterKey(string? connectionString)
        {
            CheckedConnectionStrings.Add(connectionString);
            return Task.CompletedTask;
        }
    }
}
