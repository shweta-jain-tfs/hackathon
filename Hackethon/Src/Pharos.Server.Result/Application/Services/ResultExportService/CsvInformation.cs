using System.Globalization;


// ReSharper disable StringLiteralTypo

namespace Pharos.Server.Result.Application.Services.ResultExportService;

/// <summary>
/// The [INFORMATION] section in the result export CSV file.
/// </summary>
public record CsvInformation
{
    /// <summary>
    /// Type of export <see cref="ResultExportType"/>
    /// </summary>
    [CsvField("TypeOfExport")]
    public required int TypeOfExport { get; init; }

    /// <summary>
    /// Start time of the first analytical run.
    /// Used for file name and information header. Date and time are serialized separately.
    /// </summary>
    public required DateTimeOffset StartDateTime { get; init; }

    [CsvField("StartDate")]
    public string StartDate => StartDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    [CsvField("StartTime")]
    public string StartTime => StartDateTime.ToString("HH:mm:ss", CultureInfo.InvariantCulture);

    /// <summary>
    /// End time of the last analytical run or null if there are unfinished runs.
    /// Date and time are serialized separately.
    /// </summary>
    public DateTimeOffset? EndDateTime { get; init; }

    [CsvField("EndDate")]
    public string EndDate => EndDateTime?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";

    [CsvField("EndTime")]
    public string EndTime => EndDateTime?.ToString("HH:mm:ss", CultureInfo.InvariantCulture) ?? "";

    /// <summary>
    /// Analytical run ID. "0" if not exporting a single analytical run.
    /// </summary>
    [CsvField("AnalyticalRunId")]
    public string AnalyticalRunId { get; init; } = "0";

    /// <summary>
    /// CMR ID used to generate the result export (can include several analytical runs).
    /// </summary>
    [CsvField("CustomRunId")]
    public string? CustomRunIdentity { get; init; } = "";

    /// <summary>
    /// Instrument "friendly" name as defined by the laboratory.
    /// </summary>
    [CsvField("InstrumentName")]
    public string? InstrumentName { get; set; } = "";

    [CsvField("InstrumentSerialNumber")]
    public string InstrumentSerialNumber { get; set; } = "";

    [CsvField("PharosVersion")]
    public string PharosVersion { get; init; } = "";

    /// <summary>
    /// Instrument type <see cref="InstrumentType"/>
    /// </summary>
    [CsvField("InstrumentType")]
    public int? InstrumentType { get; set; }

    /// <summary>
    /// Username or ID of the user who initiated the export (manual exports only).
    /// For automatic exports this field is empty.
    /// </summary>
    [CsvField("ExportByUser")]
    public string ExportByUser { get; init; } = "";

    /// <summary>
    /// Method used in analytical run. Null or empty for export of multiple analytical runs.
    /// Used for file name. Not serialized.
    /// </summary>
    public string? MethodName { get; init; }

}