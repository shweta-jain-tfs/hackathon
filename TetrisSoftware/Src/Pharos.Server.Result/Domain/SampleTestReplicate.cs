namespace Pharos.Server.Result.Domain;

public class SampleTestReplicate : TestReplicate
{
    [MaxLength(SampleTestResult.ClassLength)]
    public string? Class { get; set; }
    public bool LowestClassFlag { get; set; }
    public bool HighestClassFlag { get; set; }

    [MaxLength(SampleTestResult.CutOffLength)]
    public string? CutOff { get; set; }

    public double? Quotient { get; set; }

    [MaxLength(SampleTestResult.QuotientAsTextLength)]
    public string? QuotientAsText { get; set; }
}
