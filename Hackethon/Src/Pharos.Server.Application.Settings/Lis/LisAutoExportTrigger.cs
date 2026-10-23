using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Application.Settings.Lis;

public enum LisAutoExportTrigger
{
    [DisplayNameLocal("LisExportSampleSettings_AutomaticSampleExport_NotUsed")]
    NotUsed = 0,

    [DisplayNameLocal("LisExportSampleSettings_AutomaticSampleExport_WhenTestIsApprovedOrRejected")]
    WhenTestIsApprovedOrRejected = 1,

    [DisplayNameLocal("LisExportSampleSettings_AutomaticSampleExport_WhenSampleIsApprovedOrRejected")]
    WhenSampleIsApprovedOrRejected = 2
}
