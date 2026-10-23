using System.ComponentModel.DataAnnotations;
using Pharos.Server.Domain.Common;

namespace Pharos.Server.Domain.Parameters.Methods;

/// <summary>
/// One calibrator point
/// A number of points will together create a calibration curve
/// </summary>
public class MethodCalibrationPoint : BaseAuditableEntity
{
    public const int CalibratorNameLength = 8;

    /// <summary>
    /// The ID of the method curve this point belongs to.
    /// </summary>
    public int MethodCalibrationCurveId { get; set; }

    /// <summary>
    /// Name of the calibrator point, for example CAL-0.70
    /// </summary>
    [MaxLength(CalibratorNameLength)]
    public string CalibratorName { get; set; } = default!;

    /// <summary>
    /// The expected concentration for this calibrator point, for example 0,70
    /// </summary>
    public double ExpectedConcentration { get; set; }

    /// <summary>
    /// Number of replicates to use for this calibrator point, for example 2
    /// </summary>
    public int Replicates { get; set; }

    public double AdjustmentForAlfaBeta1Beta2 { get; set; }

    /// <summary>
    /// See "Phadia Laboratory Systems Data Management Specification (DMS), 303781" in Docan
    /// </summary>
    public double IdealResponse { get; set; }

    public double ExpectedStdDev { get; set; }

    public double UpperResponseLimit { get; set; }

    public double LowerResponseLimit { get; set; }

    public double StdDevLimit { get; set; }

    public double StdDevPrecisionLimit { get; set; }

    public double? UpperLogarithmicLimit { get; set; }

    public double? LowerLogarithmicLimit { get; set; }

    #region Navigation properties

    /// <summary>
    /// The navigation property for the method calibrators this point belongs to
    /// </summary>
    public virtual MethodCalibrationCurve CalibrationCurve { get; set; } = default!;

    #endregion
}
