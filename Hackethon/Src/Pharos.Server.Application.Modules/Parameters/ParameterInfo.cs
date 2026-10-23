namespace Pharos.Server.Application.Parameters;

/// <summary>
/// The parameter info used for GetReflex
/// </summary>
public class ParameterInfo
{
    /// <summary>
    /// Does the test report concentration?
    /// </summary>
    public bool ReportConcentration { get; set; }

    /// <summary>
    /// Does the test report class?
    /// </summary>
    public bool ReportClass { get; set; }

    /// <summary>
    /// Does the test report quotient?
    /// </summary>
    public bool ReportQuotient { get; set; }

    /// <summary>
    /// Does the test report cut off?
    /// </summary>
    public bool ReportCutOff { get; set; }

    /// <summary>
    /// The shown options for class
    /// </summary>
    public List<string?> ClassOptions { get; set; } = new();

    /// <summary>
    /// The shown options for cut off
    /// </summary>
    public List<string?> CutOffOptions { get; set; } = new();
}