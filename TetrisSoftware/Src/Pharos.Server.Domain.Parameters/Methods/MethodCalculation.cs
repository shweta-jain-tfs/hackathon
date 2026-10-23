using Pharos.Server.Domain.Enums;

namespace Pharos.Server.Domain.Parameters.Methods;

/// <summary>
/// Calculation parameters to use for a method.
/// Most of these parameters are just feed into the calculation
/// module, so, here we don't need to know that much about them... ;-)
/// </summary>
public class MethodCalculation
{
    /// <summary>
    /// Calculation type used for a method.
    /// Rodbard is the most common one. It's a type of curve fitting algorithm
    /// </summary>
    public CalculationType CalculationType { get; set; }

    public int CalibratorsForLeastSquareCalc { get; set; }

    public int CalibratorStartingPoint { get; set; }

    public double MinCoefficientOfCorrelation { get; set; }

    public double PredictedVarianceA { get; set; }

    public double PredictedVarianceB { get; set; }

    public RobustWeight RobustWeight { get; set; }

    public NormalWeight NormalWeight { get; set; }
}