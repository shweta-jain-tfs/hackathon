using System.ComponentModel.DataAnnotations;
using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Application.Settings.Lis;

public class HL7CommunicationTcpSettings
{
    [DisplayNameLocal("HL7TcpSettings_SocketType")]
    public SocketType SocketType { get; set; } = SocketType.Server;

    [DisplayNameLocal("HL7TcpSettings_RemoteNameOrIpAddress")]
    [MaxLength(64)]
    public string RemoteNameOrIpAddress { get; set; } = "127.0.0.1";

    [DisplayNameLocal("HL7TcpSettings_TcpPort")]
    [SettingsRange(0, 65535)]
    public int TcpPort { get; set; } = 1001;

    [DisplayNameLocal("HL7TcpSettings_TcpReconnectionInterval")]
    [SettingsRange(0, 600000, "ms")]
    public int TcpReconnectionInterval { get; set; } = 10000;

    [DisplayNameLocal("HL7TcpSettings_UseSeparateExportPort")]
    public bool UseSeparateExportPort { get; set; }

    [DisplayNameLocal("HL7TcpSettings_SocketTypeForSeparateExportPort")]
    [DependsFrom(nameof(UseSeparateExportPort), "True", DependencyType.Hide)]
    public SocketType SocketTypeForSeparateExportPort { get; set; } = SocketType.Server;

    [DisplayNameLocal("HL7TcpSettings_RemoteNameOrIpAddressForSeparateExportPort")]
    [DependsFrom(nameof(UseSeparateExportPort), "True", DependencyType.Hide)]
    [MaxLength(64)]
    public string RemoteNameOrIpAddressForSeparateExportPort { get; set; } = "127.0.0.1";

    [DisplayNameLocal("HL7TcpSettings_TcpPortForSeparateExportPort")]
    [DependsFrom(nameof(UseSeparateExportPort), "True", DependencyType.Hide)]
    [SettingsRange(0, 65535)]
    public int TcpPortForSeparateExportPort { get; set; } = 1001;

    [DisplayNameLocal("HL7TcpSettings_TcpReconnectionIntervalForSeparateExportPort")]
    [DependsFrom(nameof(UseSeparateExportPort), "True", DependencyType.Hide)]
    [SettingsRange(0, 600000, "ms")]
    public int TcpReconnectionIntervalForSeparateExportPort { get; set; } = 10000;

    [DisplayNameLocal("HL7TcpSettings_SendAcknowledgeOnImportPort")]
    [DependsFrom(nameof(UseSeparateExportPort), "True", DependencyType.Hide)]
    public bool SendAcknowledgeOnImportPort { get; set; }
}
