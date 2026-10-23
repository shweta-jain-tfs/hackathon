using System.ComponentModel.DataAnnotations;

namespace Pharos.Server.Domain.Parameters.Methods;

/// <summary>
/// This class includes parameters for reporting results, including some
/// cases where a numerical value cannot be used (outside calibration curve, for example).
/// </summary>
public class MethodReporting
{
    public const int ClassResultTextLength = 8;
    public const int QuantitativeResultTextLength = 7;

    #region MinConcentrationToReport

    public double MinConcentrationToReport => UserMinConcentrationToReport ?? DefaultMinConcentrationToReport;

    /// <summary>
    /// The default value for the <see cref="MinConcentrationToReport"/> property provided in the method update file.
    /// </summary>
    public double DefaultMinConcentrationToReport { get; set; }

    /// <summary>
    /// The user-provided value for the <see cref="MinConcentrationToReport"/> property.
    /// </summary>
    public double? UserMinConcentrationToReport { get; set; }

    #endregion

    #region TextAboveCutOff

    /// <summary>
    /// Report text to use if the patient result concentration value is above the defined cut-off concentration
    /// for the used test, see <see cref="MethodTest.CutOffConcentration"/>.
    /// </summary>
    public string? TextAboveCutOff => UserTextAboveCutOff ?? DefaultTextAboveCutOff;

    /// <summary>
    /// The default value for the <see cref="TextAboveCutOff"/> property provided in the method update file.
    /// </summary>
    [MaxLength(ClassResultTextLength)]
    public string? DefaultTextAboveCutOff { get; set; }

    /// <summary>
    /// The user-provided value for the <see cref="TextAboveCutOff"/> property.
    /// </summary>
    [MaxLength(ClassResultTextLength)]
    public string? UserTextAboveCutOff { get; set; }

    #endregion

    #region TextBelowCutOff

    /// <summary>
    /// Report text to use if the patient result concentration value is below the defined cut-off concentration
    /// for the used test, see <see cref="MethodTest.CutOffConcentration"/>.
    /// </summary>
    public string? TextBelowCutOff => UserTextBelowCutOff ?? DefaultTextBelowCutOff;

    /// <summary>
    /// The default value for the <see cref="TextBelowCutOff"/> property provided in the method update file.
    /// </summary>
    [MaxLength(ClassResultTextLength)]
    public string? DefaultTextBelowCutOff { get; set; }

    /// <summary>
    /// The user-provided value for the <see cref="TextBelowCutOff"/> property.
    /// </summary>
    [MaxLength(ClassResultTextLength)]
    public string? UserTextBelowCutOff { get; set; }

    #endregion

    #region TextQuantitativeAbove

    /// <summary>
    /// Report text to use if the patient result concentration value is above the highest calibration point
    /// <see cref="MethodCalibrationPoint.ExpectedConcentration"/> or above <see cref="MethodTest.HighestConcentrationToReport"/>
    /// </summary>
    public string TextQuantitativeAbove => UserTextQuantitativeAbove ?? DefaultTextQuantitativeAbove;

    /// <summary>
    /// The default value for the <see cref="TextQuantitativeAbove"/> property provided in the method update file.
    /// </summary>
    [MaxLength(QuantitativeResultTextLength)]
    public string DefaultTextQuantitativeAbove { get; set; } = default!;

    /// <summary>
    /// The user-provided value for the <see cref="TextQuantitativeAbove"/> property.
    /// </summary>
    [MaxLength(QuantitativeResultTextLength)]
    public string? UserTextQuantitativeAbove { get; set; }

    #endregion

    #region TextQuantitativeBelow

    /// <summary>
    /// Report text to use if the patient result concentration value is below the lowest calibration point
    /// <see cref="MethodCalibrationPoint.ExpectedConcentration"/> or below <see cref="MinConcentrationToReport"/>
    /// </summary>
    public string TextQuantitativeBelow => UserTextQuantitativeBelow ?? DefaultTextQuantitativeBelow;

    /// <summary>
    /// The default value for the <see cref="TextQuantitativeBelow"/> property provided in the method update file.
    /// </summary>
    [MaxLength(QuantitativeResultTextLength)]
    public string DefaultTextQuantitativeBelow { get; set; } = default!;

    /// <summary>
    /// The user-provided value for the <see cref="TextQuantitativeBelow"/> property.
    /// </summary>
    [MaxLength(QuantitativeResultTextLength)]
    public string? UserTextQuantitativeBelow { get; set; }

    #endregion

    #region TextQuantitativeDoubtful

    /// <summary>
    /// Report text to use if the patient test order has more than one replicate and:
    /// - at least one replicate is on the curve
    /// - at least one replicate is outside the curve
    /// </summary>
    public string? TextQuantitativeDoubtful => UserTextQuantitativeDoubtful ?? DefaultTextQuantitativeDoubtful;

    /// <summary>
    /// The default value for the <see cref="TextQuantitativeDoubtful"/> property provided in the method update file.
    /// </summary>
    [MaxLength(QuantitativeResultTextLength)]
    public string? DefaultTextQuantitativeDoubtful { get; set; }

    /// <summary>
    /// The user-provided value for the <see cref="TextQuantitativeDoubtful"/> property.
    /// </summary>
    [MaxLength(QuantitativeResultTextLength)]
    public string? UserTextQuantitativeDoubtful { get; set; }

    #endregion

    #region UseCalculatedTextQuantitativeAbove

    /// <summary>
    /// If this is true, it overrides the use of TextQuantitativeAbove,
    /// i.e. instead of using the text (">100") when over the curve
    /// a numerical text value will be "calculated" and used ("124 kU/l")
    /// </summary>
    public bool UseCalculatedTextQuantitativeAbove => UserUseCalculatedTextQuantitativeAbove ?? DefaultUseCalculatedTextQuantitativeAbove;

    /// <summary>
    /// The default value for the <see cref="UseCalculatedTextQuantitativeAbove"/> property provided in the method update file.
    /// </summary>
    [MaxLength(QuantitativeResultTextLength)]
    public bool DefaultUseCalculatedTextQuantitativeAbove { get; set; }

    /// <summary>
    /// The user-provided value for the <see cref="UseCalculatedTextQuantitativeAbove"/> property.
    /// </summary>
    [MaxLength(QuantitativeResultTextLength)]
    public bool? UserUseCalculatedTextQuantitativeAbove { get; set; }

    #endregion
}