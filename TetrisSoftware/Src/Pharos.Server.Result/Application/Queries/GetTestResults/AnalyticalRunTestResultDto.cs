using Pharos.Server.Domain.Enums;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Queries.GetTestResults;


public class AnalyticalRunTestResultDto
{
    public string TestName { get; set; } = default!;
    public string MethodName { get; set; } = default!;
    public TestResultType ResultType { get; set; }
    public string? MeanResponseAsText { get; set; }
    public string? MeanConcentrationAsText { get; set; }
    public DateTimeOffset MeasurementDateTime { get; set; }
    public int NumberOfReplicates { get; set; }
    public TestResultStatus Status { get; set; }
    public double TotalDilution { get; set; }
    public string? Comment { get; set; }
    public string Unit { get; set; } = default!;
    public Guid? TestOrderId { get; set; }
    public ResultApprovalStatus? ApprovalStatus { get; set; }
    public int? RequestId { get; set; }
    public string SampleId { get; set; } = default!;
    public DateTimeOffset? RequestDate { get; set; }
    public string? Class { get; set; }
    public string? CutOff { get; set; }
    public int Id { get; set; }
    public ConcentrationStatus MeanConcentrationStatus { get; set; } = ConcentrationStatus.NotAvailable;
    public int InstrumentDilution { get; set; }
    public int PreDilution { get; set; }
    public string? MeanConcentrationRaw { get; set; }
    public string? MeanQuotientAsText { get; set; }
    public string SampleIdDisplay { get; set; } = default!;
}