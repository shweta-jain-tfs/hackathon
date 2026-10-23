using Pharos.Server.Domain;

namespace Pharos.Server.Result.Domain;
public class CurveControlTestReplicate : TestReplicate
{
    public CurveControlJudgment Judgment { get; set; } = CurveControlJudgment.NotDefined;

    [MaxLength(Constants.ArticleBarcodeLength)]
    public string? CurveControlBarcode { get; set; }
}
