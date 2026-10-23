using MediatR;

namespace Pharos.Server.Application.Result;

public class GetMethodQualityQuery : IRequest<List<MethodQualityDto>>
{
    public List<string> MethodNameList { get; set; } = [];
    public DateTimeOffset? FromDate { get; set; }
    public DateTimeOffset? ToDate { get; set; }
    public List<string> InstrumentNumberList { get; set; } = [];
}

public enum MethodQualityIssue
{
    NoIssues = 0,
    LastCcFailed = 1,
    LastCcFlagged = 2,
    LastQcFailed = 3,
    CurveExpired = 4
}

public class MethodQualityDto
{
    public int Id { get; set; }
    
    /// <summary>
    /// The name of the method
    /// </summary>
    public string MethodName { get; set; } = string.Empty;

    public string? ConjugateLotNumber { get; set; }
    public string InstrumentNumber { get; set; } = string.Empty;
    public string InstrumentName { get; set; } = string.Empty;
    public bool? CurveControlsFailed { get; set; }
    public DateTimeOffset? MeasurementDateTime { get; set; }
    public int MaxNumberOfDays { get; set; }
    public MethodQualityIssue CurrentIssue { get; set; } = MethodQualityIssue.NoIssues;

    // Explicit composite identity for grouping/navigation.
    public string MethodInstrumentGroupKey => $"{InstrumentNumber}|{MethodName}";
}

