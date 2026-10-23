using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Application.Settings.Lis;

public class AstmCommunicationRs232Settings : LisRs232Settings
{
    [DisplayNameLocal("AstmCommunicationRs232Settings_ConverterIpAddress")]
    public string ConverterIpAddress { get; set; } = "127.0.0.1";

    [DisplayNameLocal("AstmCommunicationRs232Settings_ConverterDataTcpPort")]
    public int ConverterDataTcpPort { get; set; } = 23;

    [DisplayNameLocal("AstmCommunicationRs232Settings_ConverterCommandTcpPort")]
    public int ConverterCommandTcpPort { get; set; } = 23;
}