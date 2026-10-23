using System.ComponentModel.DataAnnotations;
using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Application.Settings.Lis;

public class AstmCommunicationTcpSettings
{
    [DisplayNameLocal("AstmCommunicationTcpIpSettings_SocketType")]
    public SocketType SocketType { get; set; } = SocketType.Client;

    [DisplayNameLocal("AstmCommunicationTcpIpSettings_IpAddress")]
    public string IpAddress { get; set; } = "127.0.0.1";

    [DisplayNameLocal("AstmCommunicationTcpIpSettings_TcpPort")]
    [SettingsRange(0, 65535)]
    public int TcpPort { get; set; } = 1001;

    [DisplayNameLocal("AstmCommunicationTcpIpSettings_HostName")]
    [MaxLength(64)]
    public string HostName { get; set; } = string.Empty;

    [DisplayNameLocal("AstmCommunicationTcpIpSettings_TcpReconnectionInterval")]
    [SettingsRange(0, 600000, "ms")]
    public int TcpReconnectionInterval { get; set; } = 10000;

    [DisplayNameLocal("AstmCommunicationTcpIpSettings_UseSeparateExportPort")]
    public bool UseSeparateExportPort { get; set; }

    [DisplayNameLocal("AstmCommunicationTcpIpSettings_SocketTypeForSeparateExportPort")]
    [DependsFrom(nameof(UseSeparateExportPort), "True", DependencyType.Hide)]
    public SocketType SocketTypeForSeparateExportPort { get; set; } = SocketType.Server;

    [DisplayNameLocal("AstmCommunicationTcpIpSettings_IpAddressForSeparateExportPort")]
    [DependsFrom(nameof(UseSeparateExportPort), "True", DependencyType.Hide)]
    public string IpAddressForSeparateExportPort { get; set; } = "127.0.0.1";

    [DisplayNameLocal("AstmCommunicationTcpIpSettings_TcpPortForSeparateExportPort")]
    [DependsFrom(nameof(UseSeparateExportPort), "True", DependencyType.Hide)]
    [SettingsRange(0, 65535)]
    public int TcpPortForSeparateExportPort { get; set; } = 1001;

    [DisplayNameLocal("AstmCommunicationTcpIpSettings_HostNameForSeparateExportPort")]
    [DependsFrom(nameof(UseSeparateExportPort), "True", DependencyType.Hide)]
    [MaxLength(64)]
    public string HostNameForSeparateExportPort { get; set; } = string.Empty;

    [DisplayNameLocal("AstmCommunicationTcpIpSettings_TcpReconnectionIntervalForSeparateExportPort")]
    [DependsFrom(nameof(UseSeparateExportPort), "True", DependencyType.Hide)]
    [SettingsRange(0, 600000, "ms")]
    public int TcpReconnectionIntervalForSeparateExportPort { get; set; } = 10000;
}

public enum SocketType
{
    [DisplayNameLocal("SocketType_Server")]
    Server = 0,

    [DisplayNameLocal("SocketType_Client")]
    Client = 1
}