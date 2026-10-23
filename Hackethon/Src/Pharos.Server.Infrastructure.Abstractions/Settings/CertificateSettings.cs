namespace Pharos.Server.Infrastructure.Settings;

public sealed class CertificateSettings
{
    public const string CertificateSettingsSectionName = "Certificates";

    /// <summary>
    /// The server name to use (in the common name) when generating a default TLS certificate.
    /// If not specified, the default server name will be "localhost".
    /// </summary>
    public string DefaultServerName { get; set; } = null!;

    /// <summary>
    /// Whether to allow wildcard certificates like *.pharos.phadia.com. Not intended for production use.
    /// </summary>
    public bool AllowWildcardCertificates { get; set; }

    /// <summary>
    /// Whether to protect the locally generated trust-root private key with the TPM.
    /// </summary>
    public bool UseTpm { get; set; }

    public int ColumnMasterKeyLifetimeDays { get; set; } = 365 * 2;
}
