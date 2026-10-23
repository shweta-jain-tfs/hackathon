using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Application.Settings.Lis;

/// <summary>
/// Represents LIS connection settings for the HL7 protocol.
/// </summary>
public class HL7Settings
{
    [DisplayNameLocal("HL7Settings_CommunicationSettings")]
    public HL7CommunicationSettings CommunicationSettings { get; set; } = new();

    [DisplayNameLocal("HL7Settings_ImportSettings")]
    public HL7ImportSettings ImportSettings { get; set; } = new();

    [DisplayNameLocal("HL7Settings_ExportSettings")]
    public HL7ExportSettings ExportSettings { get; set; } = new();

    [DisplayNameLocal("HL7Settings_Other")]
    public HL7OtherSettings Other { get; set; } = new();
}