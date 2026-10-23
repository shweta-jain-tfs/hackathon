using System.Text.Json.Serialization;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Enums;

namespace Pharos.Server.Result.Domain;

/// <summary>
/// Represents the final result for a test order, calculated across all replicates.
/// </summary>
public abstract class TestResult : BaseAuditableEntity
{
    /// <summary>
    /// The ID of the analytical run of the first replicate of the test.
    /// </summary>
    /// <remarks>
    /// Sometimes, replicates can be in different analytical runs
    /// (e.g. in RnD mode, different lot numbers for different replicates could be specified).
    /// But we usually display results by analytical runs, so this field is needed for simplicity
    /// and better performance.
    /// </remarks>
    public int? AnalyticalRunId { get; set; }

    /// <summary>
    /// The test result type (sample, quality control, etc.)
    /// </summary>
    public TestResultType ResultType { get; set; }

    /// <summary>
    /// The instrument serial number.
    /// </summary>
    [MaxLength(Constants.InstrumentSerialNumberLength)]
    public string InstrumentNumber { get; set; } = default!;

    /// <summary>
    /// The name of an assay method that must be used for the test run.
    /// </summary>
    [MaxLength(Constants.MethodNameLength)]
    public string MethodName { get; set; } = default!;

    /// <summary>
    /// The name of the test (within the assay method) that must be used for the run.
    /// </summary>
    [MaxLength(Constants.TestNameLength)]
    public string TestName { get; set; } = default!;

    /// <summary>
    /// Number of times (integer) the instrument should dilute for this test.
    /// A value of 1 represents “no dilution”.
    /// </summary>
    public int InstrumentDilutionFactor { get; set; } = 1;

    /// <summary>
    /// Number of times (integer) the liquid was diluted by the lab before testing.
    /// The number less than 2 means that the liquid was not diluted.
    /// </summary>
    public int PreDilutionFactor { get; set; } = 1;

    /// <summary>
    /// The result status.
    /// </summary>
    public TestResultStatus Status { get; set; }

    /// <summary>
    /// Average response value for the result's replicates
    /// </summary>
    public double? MeanResponse { get; set; }

    /// <summary>
    /// The formatted version of the MeanResponse value
    /// </summary>
    [MaxLength(Constants.ResponseAsTextLength)]
    public string? MeanResponseAsText { get; set; }

    /// <summary>
    /// Average concentration value for the result's replicates
    /// </summary>
    public double? MeanConcentration { get; set; }

    /// <summary>
    /// The formatted version of the MeanConcentration value.
    /// Also depends on the replicates ConcentrationStatus
    /// </summary>
    [MaxLength(Constants.ConcentrationAsTextLength)]
    public string? MeanConcentrationAsText { get; set; }

    /// <summary>
    /// Concentration status (OK, below curve, ...)
    /// </summary>
    public ConcentrationStatus MeanConcentrationStatus { get; set; } = ConcentrationStatus.NotAvailable;

    /// <summary>
    /// The requested number of replicates for the test order. The result for the test order
    /// will the mean response value across all replicates.
    /// </summary>
    public int NumberOfReplicates { get; set; } = 1;


    /// <summary>
    /// Calculated %CV for Response. Coefficient variance.
    /// </summary>
    public double? PercentCvResponse { get; set; }

    /// <summary>
    /// Calculated %CV for Response. Coefficient variance.
    /// </summary>
    public double? PercentCvConcentration { get; set; }

    /// <summary>
    /// The first replicate measurement time stamp.
    /// </summary>
    public DateTimeOffset MeasurementDateTime { get; set; }

    /// <summary>
    /// Contains operator comment if available.
    /// </summary>
    [MaxLength(Constants.OperatorCommentLength)]
    public string? OperatorComment { get; set; }

    #region Navigation properties

    public virtual IList<TestReplicate>? Replicates { get; set; }

    /// <summary>
    /// The navigation property for the analytical run.
    /// </summary>
    [JsonIgnore]
    public virtual AnalyticalRun? AnalyticalRun { get; set; }

    #endregion

    public bool AllReplicatesHaveResponse => Replicates != null && Replicates.Count == NumberOfReplicates && Replicates!.All(r => r.UsedBlankId != null || r.IsAborted);
    public bool AllReplicatesAvailable => Replicates != null && Replicates.Count == NumberOfReplicates;
}
