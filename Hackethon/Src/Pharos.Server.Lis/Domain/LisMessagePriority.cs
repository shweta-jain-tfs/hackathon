namespace Pharos.Server.Lis.Domain;

/// <summary>
/// Defines the priority levels for LIS messages.
/// High priority messages are dispatched before low priority messages.
/// </summary>
/// <seealso cref="LisDispatchMode"/>
internal enum LisMessagePriority
{
    /// <summary>
    /// High priority - Test order queries.
    /// Includes manual queries (user-initiated), automatic queries (scanned samples) and poll queries.
    /// </summary>
    High = 1,

    /// <summary>
    /// Normal priority - Manual results export.
    /// Used when results are manually exported by the user.
    /// </summary>
    Normal = 2,

    /// <summary>
    /// Low priority - Automatic results export.
    /// Used when results are automatically exported after test approval/rejection.
    /// When periodic export is enabled, these messages are dispatched periodically at the specified interval.
    /// </summary>
    Low = 3
}