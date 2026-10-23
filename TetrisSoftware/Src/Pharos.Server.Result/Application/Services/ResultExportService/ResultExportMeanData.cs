using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Services.ResultExportService;
internal record ResultExportMeanData
{
    public TestResultType ResultType { get; set; }
    public double? MeanResponse { get; set; }
    public double? MeanConcentration { get; set; }
    public double? PercentCvResponse { get; set; }
    public double? PercentCvConcentration { get; set; }
    public required string MethodName { get; set; }
    public required string TestName { get; set; }
    public Guid? GroupId { get; set; }
    public string SampleId { get; set; } = "";
    public string CalibratorName { get; set; } = "";
    public string CurveControlName { get; set; } = "";
    public string QualityControlName { get; set; } = "";
}