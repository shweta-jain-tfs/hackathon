using System.ComponentModel.DataAnnotations;

namespace Pharos.Server.Domain.Parameters.Methods;

/// <summary>
/// One curve control belonging to a method
/// A set of curve controls are used to see if a previously processed
/// calibration curve still can be used for calculations
/// </summary>
public class MethodCurveControl
{
    public const int CurveControlNameLength = 8;

    /// <summary>
    /// The ID for the method the curve control belongs to
    /// </summary>
    public int MethodId { get; set; }

    /// <summary>
    /// Name of the curve control, for example CC-1
    /// </summary>
    [MaxLength(CurveControlNameLength)]
    public string CurveControlName { get; set; } = default!;

    /// <summary>
    /// The value indicating whether this curve control active (used) or not.
    /// </summary>
    public bool Active { get; set; }

    /// <summary>
    /// The ID of the method test to use for this method's curve control, for example a_IgE for method sIgE
    /// </summary>
    public int MethodTestId { get; set; }

    /// <summary>
    /// Number of replicates to use for this curve control, for example 1
    /// </summary>
    public int Replicates { get; set; }

    /// <summary>
    /// The expected concentration for this curve control, for example 0,70
    /// </summary>
    public double ExpectedConcentration { get; set; }

    /// <summary>
    /// Expected standard deviation, for example 0,084
    /// </summary>
    public double ExpectedStdDev { get; set; }

    /// <summary>
    /// Inner standard deviation limit, for example 2,2
    /// </summary>
    /// <remarks>
    /// Example of usage: Inner minimum concentration = 0,7 – (0,084 * 2,2) = 0,51.
    /// </remarks>
    public double InnerStdDevLimit { get; set; }

    /// <summary>
    /// Outer standard deviation limit, for example 3,5
    /// </summary>
    public double OuterStdDevLimit { get; set; }

    /// <summary>
    /// Calculation parameter...
    /// </summary>
    public double AdjustmentForAlfaBeta1Beta2 { get; set; }

    #region Navigation properties

    /// <summary>
    /// The navigation property for the method this curve control belongs to
    /// </summary>
    public virtual Method Method { get; set; } = default!;

    /// <summary>
    /// The navigation property for the method test this curve control uses
    /// </summary>
    public virtual MethodTest MethodTest { get; set; } = default!;

    #endregion
}
