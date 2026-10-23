using Pharos.Server.Domain;

namespace Pharos.Server.Result.Domain;

public class QualityControlTestReplicate : TestReplicate
{
    [MaxLength(Constants.ArticleBarcodeLength)]
    public string? QualityControlBarcode { get; set; }

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
