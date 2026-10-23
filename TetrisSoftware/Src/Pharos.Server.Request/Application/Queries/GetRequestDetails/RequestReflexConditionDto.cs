using Pharos.Server.Domain.Reflex;

namespace Pharos.Server.Request.Application.Queries.GetRequestDetails;

public class RequestReflexConditionDto
{
    public string? TestName { get; set; } = string.Empty;
    public string? MethodName { get; set; } = string.Empty;
    public ResultType ResultType { get; set; }
    public CompareType CompareType { get; set; }
    public double? ConcentrationCompareValue { get; set; }
    public double? ConcentrationCompareValueUpperLimit { get; set; }
    public double? ConcentrationRawCompareValue { get; set; }
    public double? ConcentrationRawCompareValueUpperLimit { get; set; }
    public string? ClassCompareValue { get; set; }
    public double? QuotientCompareValue { get; set; }
    public double? QuotientCompareValueUpperLimit { get; set; }
    public string? CutOffCompareValue { get; set; }
    public double? RuCompareValue { get; set; }
}