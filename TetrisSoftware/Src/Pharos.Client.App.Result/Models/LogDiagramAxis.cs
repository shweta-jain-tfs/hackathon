namespace Pharos.Client.App.Result.Models;
public class LogDiagramAxis
{
    public double XMin { get; set; }
    public double XMax { get; set; }
    public double YMin { get; set; }
    public double YMax { get; set; }


    /// <summary>
    /// Scale the axis of curve chart based on input values in power of 10. 
    /// </summary>
    /// <param name="inMin">Real minimum range</param>
    /// <param name="inMax">Real maximum range</param>
    /// <returns>Adjusted end ranges</returns>
    public static (double xMin, double xMax) LogAutoScale(double inMin, double inMax)
    {
        var outMax = Math.Pow(10, Math.Floor(Math.Log10(inMax - 1E-5)) + 1);

        var outMin = 1.0;

        if (inMin > 0)
        {
            outMin = Math.Pow(10, Math.Floor(Math.Log10(inMin)));
        }

        return (outMin, outMax);
    }
}
