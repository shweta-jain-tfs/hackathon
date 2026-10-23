using System.ComponentModel.DataAnnotations;
using Pharos.Server.Domain;

namespace Pharos.Server.LabCommunity.Domain;

internal sealed class Configuration
{
    [Key]
    [MaxLength(Constants.LabCommunityLaboratoryCodeLength)]
    public string LaboratoryCode { get; init; } = string.Empty;

    public int CheckInIntervalSeconds { get; set; }

    public bool SystemStatusEnabled { get; set; }

    public int SystemStatusIntervalSeconds { get; set; }

    public bool ReportInstrumentErrorsEnabled { get; set; }

    public bool ReportCalibrationAndQualityResultsEnabled { get; set; }
}