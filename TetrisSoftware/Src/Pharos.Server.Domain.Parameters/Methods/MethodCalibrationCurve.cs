using Pharos.Server.Domain.Common;

namespace Pharos.Server.Domain.Parameters.Methods;

/// <summary>
/// Parameters for building a method calibration curve.
/// </summary>
public class MethodCalibrationCurve : BaseAuditableEntity
{
    /// <summary>
    /// The ID for the method the calibrators belongs to
    /// </summary>
    public int MethodId { get; set; }

    /// <summary>
    /// The ID of the method test to use for this method's calibrators, for example a_IgE for method sIgE
    /// </summary>
    public int MethodTestId { get; set; }

    /// <summary>
    /// The maximum number of day when the calibration curve can be used.
    /// Then, the curve is considered "too old" to use and is removed.
    /// </summary>
    public int MaxNumberOfDays => UserMaxNumberOfDays ?? DefaultMaxNumberOfDays;

    /// <summary>
    /// The default value for the <see cref="MaxNumberOfDays"/> property provided in the method update file.
    /// </summary>
    public int DefaultMaxNumberOfDays { get; set; }

    /// <summary>
    /// The user-provided value for the <see cref="MaxNumberOfDays"/> property.
    /// </summary>
    public int? UserMaxNumberOfDays { get; set; }

    /// <summary>
    /// See "Phadia Laboratory Systems Data Management Specification (DMS), 303781" in Docan
    /// </summary>
    public int MaxBadReplicates { get; set; }

    public int QuotientIndex { get; set; }

    public bool ExcludeLowestCalibratorFromMeanDiff { get; set; }

    public bool ReportLowRuForZeroCalibrators { get; set; }

    #region Navigation properties

    public virtual Method Method { get; set; } = default!;

    public virtual MethodTest MethodTest { get; set; } = default!;

    public virtual IList<MethodCalibrationPoint> CalibrationPoints { get; set; } = default!;

    #endregion

}
