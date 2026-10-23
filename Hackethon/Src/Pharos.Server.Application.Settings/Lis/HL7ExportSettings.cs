using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Application.Settings.Lis;

public class HL7ExportSettings
{
    [DisplayNameLocal("HL7ExportSettings_IncludeUserIdInCommentRecord")]
    public bool IncludeUserIdInCommentRecord { get; set; }

    [DisplayNameLocal("HL7ExportSettings_IncludeLotNumbersInCommentRecord")]
    public bool IncludeLotNumbersInCommentRecord { get; set; }

    [DisplayNameLocal("HL7ExportSettings_IncludeRawDataInCommentRecord")]
    public bool IncludeRawDataInCommentRecord { get; set; }

    [DisplayNameLocal("HL7ExportSettings_UseLocalSampleIdAsLisSampleId")]
    public bool UseLocalSampleIdAsLisSampleId { get; set; }

    [DisplayNameLocal("HL7ExportSettings_ExportPanelNameInOrderRecord")]
    public bool ExportPanelNameInOrderRecord { get; set; }

    [DisplayNameLocal("HL7ExportSettings_SkipExportOfRequestorInformation")]
    public bool SkipExportOfRequestorInformation { get; set; }

    [DisplayNameLocal("HL7ExportSettings_ExportResultInOneOBR")]
    public bool ExportResultInOneOBR { get; set; }

    [DisplayNameLocal("HL7ExportSettings_UseLegacyValueForGenderAndTubeType")]
    public bool UseLegacyValueForGenderAndTubeType { get; set; }

    [DisplayNameLocal("HL7ExportSettings_ResultFormat")]
    public HL7ExportResultFormat ResultFormat { get; set; } = HL7ExportResultFormat.OUL;
}

public enum HL7ExportResultFormat
{
    [DisplayNameLocal("HL7ExportResultFormat_ORM")]
    ORM,

    [DisplayNameLocal("HL7ExportResultFormat_ORU")]
    ORU,

    [DisplayNameLocal("HL7ExportResultFormat_OUL")]
    OUL
}
