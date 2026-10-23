namespace Pharos.Server.Application.Lis;

/// <summary>
/// Represents the result for a quality control.
/// </summary>
public record LisQualityControlTestResult : LisTestResult
{
    /// <summary>
    /// The unique ID of the quality control test result in Pharos.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Name of this quality control
    /// </summary>
    public string QualityControlName { get; set; } = string.Empty;

    /// <summary>
    /// The name of an assay method that must be used for the test run.
    /// It may or may not be the same as the Pharos internal method name.
    /// </summary>
    public string MethodName { get; set; } = string.Empty;

    /// <summary>
    /// The test name (within a method) defined by a laboratory.
    /// It may or may not be the same as the Pharos internal test name.
    /// </summary>
    public string TestName { get; set; } = string.Empty;

    /// <summary>
    /// Number of times (integer) the liquid was diluted by the lab before testing.
    /// A value of 1 represents “no dilution”.
    /// </summary>
    public int PreDilutionFactor { get; set; } = 1;

    /// <summary>
    /// Number of times (integer) the instrument should dilute for this test.
    /// A value of 1 represents “no dilution”.
    /// </summary>
    public int InstrumentDilutionFactor { get; set; } = 1;
}
