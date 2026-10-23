
using System.ComponentModel.DataAnnotations;
using Pharos.Server.Domain.Enums;

namespace Pharos.Server.Domain.Parameters.Methods;

/// <summary>
/// Method presentation includes parameters for how plots should look like,
/// including for example scaling and texts for x and y-axis
/// </summary>
public class MethodPresentation
{
    public const int AxisTextMaxLength = 15;
    public const int AxisUnitTextMaxLength = 10;

    /// <summary>
    /// Scale of x-axis (linear, logarithmic)
    /// </summary>
    public AxisScale XAxisScale { get; set; }

    /// <summary>
    /// Text / label on x axis, for example "Concentration"
    /// </summary>
    [MaxLength(AxisTextMaxLength)]
    public string XAxisText { get; set; } = default!;

    /// <summary>
    /// Unit on x axis, for example "kU/l" (kilo units per liter)
    /// </summary>
    [MaxLength(AxisUnitTextMaxLength)]
    public string XAxisUnit { get; set; } = default!;

    /// <summary>
    /// Scale of y-axis (linear, logarithmic)
    /// </summary>
    public AxisScale YAxisScale { get; set; }

    /// <summary>
    /// Text / label on y axis, for example "Response"
    /// </summary>
    [MaxLength(AxisTextMaxLength)]
    public string YAxisText { get; set; } = default!;

    /// <summary>
    /// Unit on y-axis, for example "RU" (response units)
    /// </summary>
    [MaxLength(AxisUnitTextMaxLength)]
    public string YAxisUnit { get; set; } = default!;

    /// <summary>
    /// The value indicating whether the response axis is fixed (or automatic).
    /// </summary>
    public bool FixedYAxis { get; set; }

    /// <summary>
    /// Fixed lower limit of response values on y-axis, zero if not used for current method
    /// </summary>
    public double? FixedYAxisLowerLimit { get; set; }

    /// <summary>
    /// Fixed upper limit of response values on y-axis, zero if not used for current method
    /// </summary>
    public double? FixedYAxisUpperLimit { get; set; }
}