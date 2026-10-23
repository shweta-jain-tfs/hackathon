namespace Pharos.Server.Lis.Application.Interfaces;

/// <summary>
/// Represents the role and state of a LIS connection.
/// </summary>
internal sealed record LisConnectionStatus(LisConnectionRole Role, LisConnectionState State);

/// <summary>
/// Represents an abstraction of a pool of existing LIS connections.
/// </summary>
internal interface ILisConnectionPool
{
    /// <summary>
    /// Gets an enumerable containing a collection of LIS channel IDs for connections in the pool.
    /// </summary>
    IEnumerable<int> LisChannels { get; }

    /// <summary>
    /// Call this method to lock a connection for sending messages.
    /// </summary>
    /// <param name="lisChannelId">The channel ID of the LIS to which messages will be sent.</param>
    /// <param name="useImportConnection">Whether the import connection can be used for sending.</param>
    /// <returns>The LIS connection instance if lock is successful, otherwise null.</returns>
    ILisConnection? LockConnection(int lisChannelId, bool useImportConnection = false);

    /// <summary>
    /// Call this method to unlock the connection after each use.
    /// </summary>
    /// <param name="lisConnection">The LIS connection instance that was previously locked.</param>
    void UnlockConnection(ILisConnection lisConnection);

    /// <summary>
    /// Occurs when a new connection is added to the pool.
    /// </summary>
    event EventHandler<ILisConnection>? ConnectionAdded;

    /// <summary>
    /// Occurs when a connection is removed from the pool.
    /// </summary>
    event EventHandler<ILisConnection>? ConnectionRemoved;

    /// <summary>
    /// Returns a list of connection statuses for all connections associated with the specified LIS channel ID.
    /// </summary>
    IReadOnlyList<LisConnectionStatus> GetConnectionStates(int lisChannelId);
}
