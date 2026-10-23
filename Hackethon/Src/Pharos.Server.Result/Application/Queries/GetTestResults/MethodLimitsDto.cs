namespace Pharos.Server.Result.Application.Queries.GetTestResults;
public class MethodLimitsDto
{
    //moved
    public string CurveControlName { get; set; } = default!;
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
    /// The expected concentration for this curve control, for example 0,70
    /// </summary>
    public double ExpectedConcentration { get; set; }

}
