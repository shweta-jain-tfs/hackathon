using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Services.ResultExportService;

internal record ResultExportResultData
{
    public Guid InstrumentReplicateId { get; init; }
    public DateTimeOffset MeasurementDateTime { get; init; }
    public bool IsAborted { get; init; }
    public string SampleId { get; init; } = string.Empty;
    public string? RequestId { get; set; }
    public string MethodName { get; init; } = string.Empty;
    public string TestName { get; init; } = string.Empty;
    public TestResultType OrderType { get; init; }
    public double? RU { get; init; }
    public bool LowRU { get; init; }
    public double? Concentration { get; init; }
    public double? Quotient { get; init; }
    public int PreDilutionFactor { get; init; }
    public int InstrumentDilutionFactor { get; init; }
    public int? BlankId { get; init; }
    public Guid? GroupId { get; init; }
    public required List<TestReplicateReagent> Reagents { get; init; }
    public string? Class { get; init; }
    public string? CutOff { get; init; }
    public Guid InstrumentAnalyticalRunId { get; init; }
    public string? Comment { get; init; }
    public double? FluorometerValue { get; init; }
    public string? CmrRunId { get; init; }
    public string? CmrTestId { get; init; }
    public Guid? TestOrderId { get; init; }
    public int? CurveId { get; init; }
    public double CalibratorOriginalResponse { get; init; }
    public int? CalculationCurveId { get; init; }
    public double? EditedResponse { get; init; }
    public int Judgement { get; init; }
    public int[] ErrorCodes { get; init; } = [];
    public DateTimeOffset RequestCreatedDate { get; set; }
    public int IrwRow { get; set; }
    public int IrwSpoke { get; set; }
    public int ErwRow { get; set; }
    public int ErwSpoke { get; set; }
}
