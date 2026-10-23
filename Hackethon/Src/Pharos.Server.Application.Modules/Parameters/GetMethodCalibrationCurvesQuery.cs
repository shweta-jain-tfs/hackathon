using MediatR;

namespace Pharos.Server.Application.Parameters;

// Query to retrieve a list of method calibration curves.
public record GetMethodCalibrationCurvesQuery : IRequest<List<MethodCalibrationCurveDto>>;

/// <summary>
/// Represents a method calibration curve.
/// </summary>
public class MethodCalibrationCurveDto
{
    // Unique identifier for the calibration curve.
    public int CurveId { get; set; }

    // Identifier for the associated method.
    public int MethodId { get; set; }

    // Name of the method associated with the calibration curve.
    public required string MethodName { get; set; }

    // Identifier for the test associated with the method.
    public int MethodTestId { get; set; }

    // Maximum number of days the calibration curve is valid.
    public int MaxNumberOfDays { get; set; }

    // Indicates if monthly calibration is allowed for this method.
    public bool AllowMonthlyCalibration { get; set; }
}
