using System.ComponentModel.DataAnnotations;

namespace Pharos.Server.Domain.Reflex;

/// <summary>
/// Represents a reflex condition that must be evaluated to determine if reflex tests should be added to a request.
/// </summary>
public class ReflexCondition
{
    /// <summary>
    /// The name of the test that this reflex condition applies to. Used together with <see cref="MethodName"/> to identify the test.
    /// </summary>
    [MaxLength(Constants.TestNameLength)]
    public string TestName { get; set; } = default!;

    /// <summary>
    /// The name of the method that this reflex condition applies to. Used together with <see cref="TestName"/> to identify the test.
    /// </summary>
    [MaxLength(Constants.MethodNameLength)]
    public string MethodName { get; set; } = default!;

    /// <summary>
    /// The reflex condition Result Type. Eg Concentration.
    /// </summary>
    public ResultType ResultType { get; set; }

    /// <summary>
    /// The reflex condition Compare Type. Eg Above, Below etc.
    /// </summary>
    public CompareType CompareType { get; set; }

    /// <summary>
    /// The concentration value that the test result is compared to. Is also used as lower limit when between is selected.
    /// </summary>
    public double? ConcentrationCompareValue { get; set; }

    /// <summary>
    /// The concentration value that the test result is compared to. This value is only used when between is selected.
    /// </summary>
    public double? ConcentrationCompareValueUpperLimit { get; set; }

    /// <summary>
    /// The concentration raw value that the test result is compared to. Is also used as lower limit when between is selected.
    /// </summary>
    public double? ConcentrationRawCompareValue { get; set; }

    /// <summary>
    /// The concentration raw value that the test result is compared to. This value is only used when between is selected.
    /// </summary>
    public double? ConcentrationRawCompareValueUpperLimit { get; set; }

    /// <summary>
    /// The class string value that the test result class is compared to.
    /// </summary>
    [MaxLength(Constants.ClassCompareValueLength)]
    public string? ClassCompareValue { get; set; }

    /// <summary>
    /// The quotient value that the test result quotient is compared to.
    /// </summary>
    public double? QuotientCompareValue { get; set; }

    /// <summary>
    /// The quotient value that the test result is compared to. This value is only used when between is selected.
    /// </summary>
    public double? QuotientCompareValueUpperLimit { get; set; }

    /// <summary>
    /// The cut-off string value that the test result cut off value is compared to.
    /// </summary>
    [MaxLength(Constants.CutOffCompareValueLength)]
    public string? CutOffCompareValue { get; set; }

    /// <summary>
    /// The RU value that the test result RU is compared to.
    /// </summary>
    public double? RuCompareValue { get; set; }
}
