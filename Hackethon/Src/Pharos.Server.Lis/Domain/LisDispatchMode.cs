namespace Pharos.Server.Lis.Domain;

/// <summary>
/// Defines the dispatch mode for message prioritization when processing from the queue.
/// </summary>
internal enum LisDispatchMode
{
    /// <summary>
    /// Dispatch all messages regardless of priority.
    /// Used when periodic results export is disabled.
    /// </summary>
    All = 0,

    /// <summary>
    /// Dispatch all messages except Low priority.
    /// Used for default dispatching when periodic results export is enabled.
    /// </summary>
    ExcludeLow = 1,

    /// <summary>
    /// Dispatch only Low priority messages.
    /// Used for periodic results export.
    /// </summary>
    OnlyLow = 2
}