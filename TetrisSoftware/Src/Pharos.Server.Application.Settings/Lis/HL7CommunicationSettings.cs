using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Application.Settings.Lis;

public class HL7CommunicationSettings
{
    [DisplayNameLocal("HL7CommunicationSettings_TcpSettings")]
    public HL7CommunicationTcpSettings TcpSettings { get; set; } = new();

    [DisplayNameLocal("HL7CommunicationSettings_DelayBetweenRequestInformationRecords")]
    [SettingsRange(0, 1200)]
    public int DelayBetweenRequestInformationRecords { get; set; }

}
