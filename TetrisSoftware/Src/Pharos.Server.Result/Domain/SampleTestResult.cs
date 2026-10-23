using Pharos.Server.Domain;
using Pharos.Server.Domain.Entities;
using Pharos.Server.Domain.Enums;

namespace Pharos.Server.Result.Domain;

/// <summary>
/// Represents the final result for a test order on a patient sample.
/// </summary>
public class SampleTestResult : TestResult, ITestResult
{
    public const int ClassLength = 8;
    public const int CutOffLength = 8;
    public const int QuotientAsTextLength = 7;

    public SampleTestResult()
    {
        ResultType = TestResultType.Sample;
    }

    public RequestUid RequestUid { get; } = new RequestUid();

    /// <summary>
    /// The ID of the test order to which this result belongs.
    /// </summary>
    public Guid TestOrderId { get; set; }

    public double? MeanConcentrationRaw => MeanConcentration / PreDilutionFactor / InstrumentDilutionFactor;

    [MaxLength(Constants.ResultClassLength)]
    public string? MeanClass { get; set; }

    [MaxLength(Constants.ResultCutOffLength)]
    public string? MeanCutOff { get; set; }

    public double? MeanQuotient { get; set; }

    [MaxLength(QuotientAsTextLength)]
    public string? MeanQuotientAsText { get; set; }

    public bool LowestClassFlag { get; set; }
    public bool HighestClassFlag { get; set; }

    /// <summary>
    /// Is the result approved or not...
    /// </summary>
    public ResultApprovalStatus ApprovalStatus { get; set; } = ResultApprovalStatus.NotApproved;

    /// <summary>
    /// True when this result has been successfully reported to LIS.
    /// </summary>
    public bool IsReportedToLis { get; set; }

    /// <summary>
    /// The sample identifier from the request UID.
    /// </summary>
    public string? SampleId => RequestUid.SampleId;

    [MaxLength(Constants.SampleIdMaxLength)]
    public string? SampleIdNaturalSortKey { get; }

    [MaxLength(Constants.TestNameLength)]
    public string? TestNameNaturalSortKey { get; }

    [MaxLength(Constants.ResultClassLength)]
    public string? MeanClassNaturalSortKey { get; }

    [MaxLength(Constants.ResultCutOffLength)]
    public string? MeanCutOffNaturalSortKey { get; }

    [MaxLength(QuotientAsTextLength)]
    public string? MeanQuotientNaturalSortKey { get; }
}
