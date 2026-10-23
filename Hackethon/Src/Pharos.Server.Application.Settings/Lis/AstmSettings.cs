using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Application.Settings.Lis;

/// <summary>
/// Represents LIS connection settings for the ASTM protocol.
/// </summary>
public class AstmSettings
{
    [DisplayNameLocal("AstmSettings_CommunicationSettings")]
    public AstmCommunicationSettings CommunicationSettings { get; set; } = new();

    [DisplayNameLocal("AstmSettings_ImportSettings")]
    public AstmImportSettings ImportSettings { get; set; } = new();

    [DisplayNameLocal("AstmSettings_ExportSettings")]
    public AstmExportSettings ExportSettings { get; set; } = new();

    [DisplayNameLocal("AstmSettings_Other")]
    public AstmOtherSettings Other { get; set; } = new();
}