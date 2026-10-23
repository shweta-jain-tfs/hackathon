using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Application.Settings.Lis;

public enum IncludeOtherTestsInAutomaticExports
{
    [DisplayNameLocal("LisExportSampleSettings_IncludeOtherTestsInAutomaticExports_NotUsed")]
    NotUsed = 0,

    [DisplayNameLocal("LisExportSampleSettings_IncludeOtherTestsInAutomaticExports_AllTestsForSample")]
    AllTestsForSample = 1,

    [DisplayNameLocal("LisExportSampleSettings_IncludeOtherTestsInAutomaticExports_AllTestsForSampleExceptAlreadyReported")]
    AllTestsForSampleExceptAlreadyReported = 2
}
