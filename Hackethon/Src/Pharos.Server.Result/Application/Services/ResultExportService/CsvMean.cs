using Pharos.Server.Result.Domain;

// ReSharper disable StringLiteralTypo

namespace Pharos.Server.Result.Application.Services.ResultExportService;

/// <summary>
/// A row in the [MEAN] section in the exported CSV file.
/// Represents a test result consisting of the aggregated values of a set of replicates in a test order.
/// </summary>
public record CsvMean
{
    /// <summary>
    /// Sequence number within the list. Starting with 1.
    /// </summary>
    [CsvField("Seq")]
    public int Sequence { get; set; }

    /// <summary>
    /// Type of test (Calibrator, CC, QC or Sample). <see cref="TestResultType"/>
    /// </summary>
    [CsvField("SampleType")]
    public required int SampleType { get; init; }

    /// <summary>
    /// Internal group identity of this group of replicates.
    /// </summary>
    [CsvField("GroupId")]
    public string GroupId { get; init; } = "";

    /// <summary>
    /// Identity of Calibrator, CC, QC or Sample.
    /// </summary>
    [CsvField("SampleId")]
    public required string SampleId { get; init; }

    /// <summary>
    /// Abbreviated name of method (example "sIgE").
    /// </summary>
    [CsvField("MethodName")]
    public required string MethodName { get; init; }

    /// <summary>
    /// Name of test (example "t17").
    /// </summary>
    [CsvField("TestName")]
    public required string TestName { get; init; }

    /// <summary>
    /// Mean Response in RU.
    /// </summary>
    [CsvField("Resp")]
    public required double MeanResponse { get; init; }

    /// <summary>
    /// Mean Concentration in units.
    /// </summary>
    [CsvField("Conc")]
    public required double MeanConcentration { get; init; }

    /// <summary>
    /// %CV for Response. Coefficient variance.
    /// </summary>
    [CsvField("%CVResp")]
    public double PercentCvResponse { get; init; }

    /// <summary>
    /// %CV for Response. Coefficient variance.
    /// </summary>
    [CsvField("%CVConc")]
    public double PercentCvConcentration { get; init; }
}