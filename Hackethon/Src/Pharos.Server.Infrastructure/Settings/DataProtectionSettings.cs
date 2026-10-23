namespace Pharos.Server.Infrastructure.Settings;

/// <summary>
/// Options for configuring data protection.
/// </summary>
internal sealed class DataProtectionSettings
{
    public const string DataProtectionSection = "DataProtection";

    public bool EnableDataProtection { get; set; }
}