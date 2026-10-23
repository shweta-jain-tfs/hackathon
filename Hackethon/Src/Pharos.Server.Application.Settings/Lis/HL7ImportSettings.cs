using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Application.Settings.Lis;

public class HL7ImportSettings
{
    [DisplayNameLocal("HL7ImportSettings_SkipImportOfRequestorInformation")]
    public bool SkipImportOfRequestorInformation { get; set; }

    [DisplayNameLocal("HL7ImportSettings_SampleIdInOBX")]
    public bool SampleIdInOBX { get; set; }

    [DisplayNameLocal("HL7ImportSettings_UseRequestIdAsSampleId")]
    public bool UseRequestIdAsSampleId { get; set; }
}
