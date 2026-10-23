namespace Pharos.Server.Domain.Parameters.Methods;

public class MethodAnalyticalRun
{
    #region AllowMonthlyCalibration

    /// <summary>
    /// Monthly calibration is where you first run calibrators to get a calibration curve
    /// (used to from raw response on Y-axis, match that point on the curve and go down to X-axis to find the concentration)
    /// and later on only run curve control to check that the current calibration curve still can be used for concentration calculations.
    /// If curve controls fails to meet their requirements, it is time to run calibrators again, to get a new calibration curve
    /// </summary>
    /// <remarks>
    /// If the conjugate lot number changes, it is also time run calibrators...
    /// </remarks>
    public bool AllowMonthlyCalibration => UserAllowMonthlyCalibration ?? DefaultAllowMonthlyCalibration;

    /// <summary>
    /// The default value for the <see cref="AllowMonthlyCalibration"/> property provided in the method update file.
    /// </summary>
    public bool DefaultAllowMonthlyCalibration { get; set; }

    /// <summary>
    /// The user-provided value for the <see cref="AllowMonthlyCalibration"/> property.
    /// </summary>
    public bool? UserAllowMonthlyCalibration { get; set; }

    #endregion

    #region MaxAnalyticalRunSize

    /// <summary>
    /// The maximum number of patient assays in one analytical run.
    /// If the run includes more assays, it is time to create a new one.
    /// </summary>
    public int MaxAnalyticalRunSize => UserMaxAnalyticalRunSize ?? DefaultMaxAnalyticalRunSize;

    /// <summary>
    /// The default value for the <see cref="MaxAnalyticalRunSize"/> property provided in the method update file.
    /// </summary>
    public int DefaultMaxAnalyticalRunSize { get; set; }

    /// <summary>
    /// The user-provided value for the <see cref="MaxAnalyticalRunSize"/> property.
    /// </summary>
    public int? UserMaxAnalyticalRunSize { get; set; }

    #endregion

    /// <summary>
    /// If this value is true the new calibration curve must be requested when either ImmunoCAP or conjugate
    /// lot number changes. Normally, only the conjugate lot number change requires a new curve.
    /// </summary>
    public bool RunCalibrationOnCarrierLotChange { get; set; }
}
