namespace Pharos.Contracts;

public class AssaySchedulerMethodDto
{
    /// <summary>
    /// The name of the instrument method.
    /// </summary>
    public required string MethodName { get; set; }

    /// <summary>
    /// Indicates whether the method is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Indicates when to run calibrators.
    /// </summary>
    public int WhenToRun { get; set; }
}
