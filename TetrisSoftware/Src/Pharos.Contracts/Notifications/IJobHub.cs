namespace Pharos.Contracts.Notifications;

/// <summary>
///  A hub for notifications about background job status changes.
/// </summary>
public interface IJobHub : IHub
{
    /// <summary>
    /// Signals that a job has completed its execution.
    /// </summary>
    /// <param name="jobName">The unique name of the job.</param>
    /// <param name="startedBy">The ID of the users who started the job.</param>
    /// <param name="isSuccessful">The value indicating whether the job was successful or not.</param>
    /// <param name="resultMessage">The localized job result message (either success or error message).</param>
    /// <returns></returns>
    Task JobCompleted(string jobName, Guid startedBy, bool isSuccessful, string resultMessage);
}

public static class JobNames
{
    public const string FlrImport = "flr-import";
    public const string ParametersImport = "parameters-import";
    public const string RequestGeneration = "request-generation";
    public const string LogDump = "log-dump";
}
