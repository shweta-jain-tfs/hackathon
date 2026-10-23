using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Application.Settings.Lis;

public class DataCapFtpSettings
{
    [DisplayNameLocal("DataCapFtpSettings_ConnectionMode")]
    public LisConnectionMode ConnectionMode { get; set; } = LisConnectionMode.Server;

    [DisplayNameLocal("DataCapFtpSettings_AsciiMode")]
    public bool AsciiMode { get; set; }

    [DisplayNameLocal("DataCapFtpSettings_SendConfirmationFile")]
    public bool SendConfirmationFile { get; set; }

    [DisplayNameLocal("DataCapFtpSettings_UserName")]
    public string? UserName { get; set; }

    [DisplayNameLocal("DataCapFtpSettings_Password")]
    public string? Password { get; set; }

    [DisplayNameLocal("DataCapFtpSettings_LisIpAddress")]
    public string? LisIpAddress { get; set; }

    [DisplayNameLocal("DataCapFtpSettings_FtpDirectory")]
    public string? FtpDirectory { get; set; }

    [DisplayNameLocal("DataCapFtpSettings_ExportSubFolder")]
    public string? ExportSubFolder { get; set; }
}