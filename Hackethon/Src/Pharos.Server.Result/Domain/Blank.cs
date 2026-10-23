using Pharos.Server.Domain;

namespace Pharos.Server.Result.Domain;

/// <summary>
/// Represents the result of a reagent blank measurement sent by an instrument to Pharos.
/// </summary>
public class Blank : BaseEntity
{
    /// <summary>
    /// The instrument serial number.
    /// </summary>
    [MaxLength(Constants.InstrumentSerialNumberLength)]
    public string InstrumentNumber { get; set; } = default!;

    public BlankType BlankType { get; set; }
    public bool ResultOk { get; set; }
    public double? LowerLimit { get; set; }
    public double MeanVoltage { get; set; }
    public double UpperLimit { get; set; }

    /// <summary>
    /// The complete barcode of used development solution.
    /// </summary>
    [MaxLength(Constants.ArticleBarcodeLength)]
    public string DevSolutionBarcode { get; set; } = default!;

    /// <summary>
    /// The number of the cartridge cell from which the development solution was aspirated.
    /// </summary>
    public int DevSolutionCellPosition { get; set; }

    /// <summary>
    /// The complete barcode of used stop solution.
    /// </summary>
    [MaxLength(Constants.ArticleUdiLength)]
    public string StopSolutionBarcode { get; set; } = default!;

    /// <summary>
    /// The time when the blank was measured (taken from the earliest blank replicate). 
    /// </summary>
    public DateTimeOffset MeasurementTime { get; set; }

    #region Navigation properties

    public virtual IList<BlankReplicate>? BlankReplicates { get; set; }

    #endregion

}
