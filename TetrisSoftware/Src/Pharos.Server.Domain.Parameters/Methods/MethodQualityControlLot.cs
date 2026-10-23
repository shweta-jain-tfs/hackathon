#pragma warning disable CS8618

using System.ComponentModel.DataAnnotations;
using Pharos.Server.Domain.Common;
using Pharos.Server.Domain.Enums;

namespace Pharos.Server.Domain.Parameters.Methods;

/// <summary>
/// Method parameters for a Quality Control
/// </summary>
public class MethodQualityControlLot : BaseAuditableEntity
{
    public const int LotNumberLength = 7;
    public const int MaxAcceptanceLength = 100000;
    public const int MaxTargetValueLength = 100000;
    public const int MaxLotStdDevLimitLength = 1000;

    public int MethodQualityControlId { get; set; }
    /// <summary>
    /// The default range min for this combination of QC liquid and method test
    /// </summary>
    [MaxLength(MaxAcceptanceLength)]
    public double? RangeMin { get; set; }
    /// <summary>
    /// The default range max for this combination of QC liquid and method test
    /// </summary>
    [MaxLength(MaxAcceptanceLength)]
    public double? RangeMax { get; set; }
    /// <summary>
    /// If set to true the <see cref="RangeMax"/> must be excluded from the range even if
    /// the value is provided (target is less than MaxValue).
    /// </summary>
    public bool ExcludeRangeMax { get; set; }
    /// <summary>
    /// If set to true the <see cref="RangeMin"/> must be excluded from the range even if
    /// the value is provided (target is greater than MinValue).
    /// </summary>
    public bool ExcludeRangeMin { get; set; }
    /// <summary>
    /// The default range min for this combination of QC liquid and method test
    /// </summary>
    [MaxLength(MaxAcceptanceLength)]
    public double? ManufacturingRangeMin { get; set; }
    /// <summary>
    /// The default range max for this combination of QC liquid and method test
    /// </summary>
    [MaxLength(MaxAcceptanceLength)]
    public double? ManufacturingRangeMax { get; set; }
    /// <summary>
    /// The default expected concentration for this combination of QC liquid and method test
    /// </summary>
    [MaxLength(MaxTargetValueLength)]
    public double? ExpectedConcentration { get; set; }
    /// <summary>
    /// The default manufacturing expected concentration for this combination of QC liquid and method test
    /// </summary>
    [MaxLength(MaxTargetValueLength)]
    public double? ManufacturingExpectedConcentration { get; set; }
    /// <summary>
    /// The default expected SD for this combination of QC liquid and method test
    /// </summary>
    [MaxLength(MaxTargetValueLength)]
    public double? ExpectedStdDev { get; set; }
    /// <summary>
    /// The default expected SD limit for this combination of QC liquid and method test
    /// </summary>
    [MaxLength(MaxLotStdDevLimitLength)]
    public double? StdDevLimit { get; set; }

    [MaxLength(LotNumberLength)]
    public string? LotNumber { get; set; }

    public DateTimeOffset? ExpirationDate { get; set; }

    public LotType LotType { get; set; }

    public ManufacturingLimitCheck ManufacturingLimitCheck { get; set; }

    #region Navigation properties

    public virtual BaseQualityControlTest QualityControlTest { get; set; }

    #endregion

}