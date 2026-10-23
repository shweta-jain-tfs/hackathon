using Pharos.Server.Domain;

namespace Pharos.Server.Result.Domain;

public class CalibratorTestReplicate : TestReplicate
{
    public NormDoseCheckResult NormDoseCheckResult { get; set; } = NormDoseCheckResult.NotDefined;
    public double CalibratorOriginalResponse { get; set; }
    public double? EditedResponse { get; set; }
    public Guid? ResponseEditedBy { get; set; }
    public DateTimeOffset? ResponseEditedDateAndTime { get; set; }

    [MaxLength(Constants.ArticleBarcodeLength)]
    public string? CalibratorBarcode { get; set; }
}
