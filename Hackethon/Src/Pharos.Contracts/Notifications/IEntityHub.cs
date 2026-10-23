namespace Pharos.Contracts.Notifications;

/// <summary>
/// Defines a strongly typed interface for sending notifications (server -> client)
/// related to a domain entity type.
/// </summary>
public interface IEntityHub<in TKey> : IHub
{
    /// <summary>
    /// Sends a message to clients that a new entity was created on the server.
    /// </summary>
    /// <param name="entityId">The entity ID.</param>
    /// <returns><see cref="Task"/> object.</returns>
    Task Created(TKey entityId);

    /// <summary>
    /// Sends a message to clients that an entity was updated on the server.
    /// </summary>
    /// <param name="entityId">The entity ID.</param>
    /// <returns><see cref="Task"/> object.</returns>
    Task Updated(TKey entityId);

    /// <summary>
    /// Sends a message to clients that an entity was deleted on the server.
    /// </summary>
    /// <param name="entityId">The entity ID.</param>
    /// <returns><see cref="Task"/> object.</returns>
    Task Deleted(TKey entityId);
}

public interface IEntityHub : IEntityHub<int>
{
}