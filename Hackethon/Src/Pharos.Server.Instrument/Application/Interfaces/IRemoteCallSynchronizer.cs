namespace Pharos.Server.Instrument.Application.Interfaces;

/// <summary>
/// Provides synchronization services for remote calls between Pharos and instruments for data integrity and proper sequencing.
/// </summary>
internal interface IRemoteCallSynchronizer
{
    /// <summary>
    /// Asynchronously waits for the execution context to become available for the specified instrument within the given timeout period.
    /// </summary>
    /// <param name="instrumentNumber">The serial number of the instrument which needs synchronization.</param>
    /// <param name="timeout">The maximum time to wait for the execution context to become available.</param>
    /// <returns>A task that represents the asynchronous wait operation. The task result contains a boolean indicating whether the execution context was acquired within the timeout period.</returns>
    Task<bool> WaitForExecutionContextAsync(string instrumentNumber, TimeSpan timeout);

    /// <summary>
    /// Synchronously waits for the execution context to become available for the specified instrument within the given timeout period.
    /// </summary>
    /// <param name="instrumentNumber">The serial number of the instrument which needs synchronization.</param>
    /// <param name="timeout">The maximum time to wait for the execution context to become available.</param>
    /// <returns>True if the execution context was acquired within the timeout period; otherwise, false.</returns>
    bool WaitForExecutionContext(string instrumentNumber, TimeSpan timeout);

    /// <summary>
    /// Releases the execution context for the specified instrument, allowing other operations to proceed.
    /// </summary>
    /// <param name="instrumentNumber">The serial number of the instrument whose execution context is to be released.</param>
    void ReleaseExecutionContext(string instrumentNumber);
}
