using Pharos.Server.Domain.Enums;

namespace Pharos.Server.Result.Application.Queries.GetCalibrationCurve;

public class MethodPresentationDto
{
    /// <summary>
    /// Text / label on x axis, for example "Concentration"
    /// </summary>
    public string XAxisText { get; set; } = default!;

    /// <summary>
    /// Unit on x axis, for example "kU/l" (kilo units per liter)
    /// </summary>
    public string XAxisUnit { get; set; } = default!;

    /// <summary>
    /// Scale of y-axis (linear, logarithmic)
    /// </summary>
    public AxisScale YAxisScale { get; set; }

    /// <summary>
    /// Text / label on y axis, for example "Response"
    /// </summary>
    public string YAxisText { get; set; } = default!;

    /// <summary>
    /// Unit on y-axis, for example "RU" (response units)
    /// </summary>
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