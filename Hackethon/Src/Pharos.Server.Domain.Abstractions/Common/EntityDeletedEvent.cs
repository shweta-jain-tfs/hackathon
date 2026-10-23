using MediatR;

namespace Pharos.Server.Domain.Common;

/// <summary>
/// Generic type for event that occurs when a new entity was deleted from the database.
/// </summary>
public abstract class EntityDeletedEvent<TEntity> : INotification where TEntity : BaseEntityWithoutId
{
    /// <summary>
    /// Gets the domain entity that was deleted.
    /// </summary>
    public TEntity DeletedEntity { get; }

    /// <summary>
    /// Creates instance of the <see cref="EntityDeletedEvent{TEntity}"/> class.
    /// </summary>
    /// <param name="deletedEntity">The domain entity that was deleted.</param>
    protected EntityDeletedEvent(TEntity deletedEntity)
    {
        DeletedEntity = deletedEntity;
    }
}