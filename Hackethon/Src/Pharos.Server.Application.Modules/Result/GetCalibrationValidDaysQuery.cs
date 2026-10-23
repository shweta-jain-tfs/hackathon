using MediatR;

namespace Pharos.Server.Application.Result;

/// <summary>
/// Query to retrieve the valid days for calibration curves.
/// </summary>
public record GetCalibrationValidDaysQuery(string InstrumentNumber) : IRequest<List<CalibrationValidDaysDto>>;

/// <summary>
/// Information about the valid days of a calibration curve.
/// </summary>
public class CalibrationValidDaysDto
{
    /// <summary>
    /// Gets or sets the number of days the calibration is valid for.
    /// </summary>
    public int CalibrationValidForDays { get; set; }

    /// <summary>
    /// Gets or sets the conjugate lot number associated with the calibration.
    /// </summary>
    public string? ConjugateLotNumber { get; set; }

    /// <summary>
    /// Gets or sets the immuno cap lot number associated with the calibration.
    /// </summary>
    public string? ImmunoCapLotNumber { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier for the calibration curve.
    /// </summary>
    public int CurveId { get; set; }

    /// <summary>
    /// Gets or sets the number of days the calibration is valid for.
    /// </summary>
    public int MaxNumberOfDays { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier for the method associated with the calibration.
    /// </summary>
    public int MethodId { get; set; }

    /// <summary>
    /// Gets or sets the name of the method associated with the calibration.
    /// </summary>
    public required string MethodName { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the calibration curve was created.
    /// </summary>
    public DateTimeOffset CurveCreated { get; set; }
}
