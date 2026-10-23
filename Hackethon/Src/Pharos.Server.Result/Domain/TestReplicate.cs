using System.Text.Json.Serialization;
using Pharos.Server.Domain;

namespace Pharos.Server.Result.Domain;

/// <summary>
/// Represents a single replicate within the test order.
/// </summary>
public abstract class TestReplicate : BaseEntity
{
    public const int CmrTestIdLength = 50;

    /// <summary>
    /// The unique ID of a test replicate that is used by the instrument.
    /// </summary>
    public Guid InstrumentReplicateId { get; set; }

    /// <summary>
    /// The replicate number assigned by the instrument.
    /// </summary>
    public int ReplicateNumber { get; set; }

    public int TestResultId { get; set; }

    /// <summary>
    /// The raw response value for the test (after any response adjustments by for example lot specific code on some EliA carrier barcode).
    /// </summary>
    public double? Response { get; set; }

    /// <summary>
    /// The formatted version of the Response value
    /// </summary>
    [MaxLength(Constants.ResponseAsTextLength)]
    public string? ResponseAsText { get; set; }

    /// <summary>
    /// The raw response value for the test, calculated from the sample signals received from the instrument, before any response adjustments (f.ex. by lot specific code on some EliA carrier barcodes)
    /// </summary>
    public double? ResponseFromInstrument { get; set; }

    /// <summary>
    /// The allergen concentration in the serum (calculated using a calibration curve
    /// after the raw response is received).
    /// </summary>
    public double? Concentration { get; set; }

    /// <summary>
    /// The formatted version of the Concentration value.
    /// Also depends on ConcentrationStatus
    /// </summary>
    [MaxLength(Constants.ConcentrationAsTextLength)]
    public string? ConcentrationAsText { get; set; }

    /// <summary>
    /// Concentration status (OK, below curve, ...)
    /// </summary>
    public ConcentrationStatus ConcentrationStatus { get; set; } = ConcentrationStatus.NotAvailable;

    /// <summary>
    /// The response measurement time stamp.
    /// </summary>
    public DateTimeOffset MeasurementDateTime { get; set; }

    /// <summary>
    /// Test ID of a result in a Custom made run
    /// </summary>
    [MaxLength(CmrTestIdLength)]
    public string? CmrTestId { get; set; }

    /// <summary>
    /// IRW row used 
    /// </summary>
    public int IrwRow { get; set; }

    /// <summary>
    /// IRW spoke used
    /// </summary>
    public int IrwSpoke { get; set; }

    /// <summary>
    /// ERW row used 
    /// </summary>
    public int ErwRow { get; set; }

    /// <summary>
    /// ERW spoke used
    /// </summary>
    public int ErwSpoke { get; set; }

    /// <summary>
    /// For calibrator results: may be set to false if norm dose check fails
    /// For sample results: May be set to false by operator to not use the result of this replicate
    /// (if more than one replicate, the remaining Active replicate results may be used)
    /// </summary>
    public bool Active { get; set; } = true;

    /// <summary>
    /// The value indicating whether this test replicate was aborted or not.
    /// </summary>
    public bool IsAborted { get; set; }

    /// <summary>
    /// The fluorometer measurements for this replicate (mV).
    /// </summary>
    public double? FluorometerValue { get; set; }

    /// <summary>
    /// Set to true if the response value is "lower than possible", indication a problem...
    /// </summary>
    public bool IsLowResponse { get; set; }

    /// <summary>
    /// Carrier barcode for this replicate
    /// </summary>
    [MaxLength(Constants.ArticleBarcodeLength)]
    public string? CarrierBarcode { get; set; }

    /// <summary>
    /// Reference to the blank that was used to calculate response value
    /// </summary>
    public int? UsedBlankId { get; set; }

    #region Navigation properties

    [JsonIgnore]
    public virtual TestResult TestResult { get; set; } = default!;

    /// <summary>
    /// The navigation property for the test order.
    /// </summary>
    public virtual IList<TestReplicateError>? TestReplicateErrors { get; set; }
    public virtual IList<TestReplicateReagent>? TestReplicateReagents { get; set; }

    #endregion
}
