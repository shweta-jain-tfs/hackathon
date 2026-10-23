namespace Pharos.Server.Domain.Parameters.Methods;

/// <summary>
/// Priority parameters for a test, used by ViewAllergy method only.
/// </summary>
/// <remarks>
/// In LIS settings there may be a setting that prioritizes the order of the tests for an imported sample
/// LIS: Request priority order = "Not used", "Use order numbers A" or "Use order numbers B".
/// When a sample is imported, the list of tests may be re-sorted according to this setting
///
/// Each test may indicate that they belong to "Priority A" and/or "Priority B"
/// For each (A/B) a sort number can be supplied
/// </remarks>
public class MethodTestPriority
{
    /// <summary>
    /// Mark this test to belong to the "Priority group A"
    /// </summary>
    public bool IncludeInPriorityGroupA { get; set; }

    /// <summary>
    /// Define the sort order for this test in the "Priority group A"
    /// </summary>
    public int? OrderInPriorityGroupA { get; set; }

    /// <summary>
    /// Mark this test to belong to the "Priority group B"
    /// </summary>
    public bool IncludeInPriorityGroupB { get; set; }

    /// <summary>
    /// Define the sort order for this test in the "Priority group B"
    /// </summary>
    public int? OrderInPriorityGroupB { get; set; }
}
