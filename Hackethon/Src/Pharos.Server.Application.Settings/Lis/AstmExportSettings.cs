using System.ComponentModel.DataAnnotations;
using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Application.Settings.Lis;

public class AstmExportSettings
{
    [DisplayNameLocal("AstmExportSettings_UseFourFieldsInResultReporting")]
    public bool UseFourFieldsInResultReporting { get; set; }

    [DisplayNameLocal("AstmExportSettings_IncludeUserIdInCommentRecord")]
    public bool IncludeUserIdInCommentRecord { get; set; }

    [DisplayNameLocal("AstmExportSettings_IncludeUserIdInResultsRecord")]
    public bool IncludeUserIdInResultsRecord { get; set; }

    [DisplayNameLocal("AstmExportSettings_IncludeLotNumbersInCommentRecord")]
    public IncludeLotNumbersInCommentRecord IncludeLotNumbersInCommentRecord { get; set; } = IncludeLotNumbersInCommentRecord.NotUsed;

    [DisplayNameLocal("AstmExportSettings_IncludeRawDataInCommentRecord")]
    public bool IncludeRawDataInCommentRecord { get; set; }

    [DisplayNameLocal("AstmExportSettings_UseLocalSampleIdAsLisSampleId")]
    public bool UseLocalSampleIdAsLisSampleId { get; set; }

    [DisplayNameLocal("AstmExportSettings_OverrideInstrumentName")]
    [MaxLength(40)]
    public string OverrideInstrumentName { get; set; } = string.Empty;

    [DisplayNameLocal("AstmExportSettings_ExportPanelNameInOrderRecord")]
    public ExportPanelNameInOrderRecord ExportPanelNameInOrderRecord { get; set; } = ExportPanelNameInOrderRecord.NotUsed;

    [DisplayNameLocal("AstmExportSettings_ExportResultInOneOrderRecord")]
    public bool ExportResultInOneOrderRecord { get; set; }

    [DisplayNameLocal("AstmExportSettings_SkipExportOfRequestorInformation")]
    public bool SkipExportOfRequestorInformation { get; set; }

    [DisplayNameLocal("AstmExportSettings_SkipExportOfPatientInformation")]
    public bool SkipExportOfPatientInformation { get; set; }

    //TODO: #These settings will be implemented in future when we add support for pending and error status in the result record.

    //[DisplayNameLocal("AstmExportSettings_IncludePendingStatusInResultRecord")]
    //public IncludePendingStatusInResultRecord IncludePendingStatusInResultRecord { get; set; } = IncludePendingStatusInResultRecord.NotUsed;

    //[DisplayNameLocal("AstmExportSettings_IncludeProcessingErrorStatusInResultRecord")]
    //public bool IncludeProcessingErrorStatusInResultRecord { get; set; }
}

public enum ExportPanelNameInOrderRecord
{
    [DisplayNameLocal("AstmExportSettings_ExportPanelNameInOrderRecord_NotUsed")]
    NotUsed = 0,

    [DisplayNameLocal("AstmExportSettings_ExportPanelNameInOrderRecord_Used")]
    Used = 1,

    [DisplayNameLocal("AstmExportSettings_ExportPanelNameInOrderRecord_UsedWithResultsForAllPanels")]
    UsedWithResultsForAllPanels = 2
}

public enum IncludeLotNumbersInCommentRecord
{
    [DisplayNameLocal("AstmExportSettings_IncludeLotNumbersInCommentRecord_NotUsed")]
    NotUsed = 0,

    [DisplayNameLocal("AstmExportSettings_IncludeLotNumbersInCommentRecord_UsedIncludingEmptyLotNumbers")]
    UsedIncludingEmptyLotNumbers = 1,

    [DisplayNameLocal("AstmExportSettings_IncludeLotNumbersInCommentRecord_UsedExcludingEmptyLotNumbers")]
    UsedExcludingEmptyLotNumbers = 2
}

public enum IncludePendingStatusInResultRecord
{
    [DisplayNameLocal("AstmExportSettings_IncludePendingStatusInResultRecord_NotUsed")]
    NotUsed = 0,

    [DisplayNameLocal("AstmExportSettings_IncludePendingStatusInResultRecord_Used")]
    Used = 1,

    [DisplayNameLocal("AstmExportSettings_IncludePendingStatusInResultRecord_UsedExportSampleWhenPendingStatusChanges")]
    UsedExportSampleWhenPendingStatusChanges = 2
}