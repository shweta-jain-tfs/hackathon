using MediatR;

namespace Pharos.Server.Domain.Common;

/// <summary>
/// Generic type for event that occurs when a new entity was updated from the database.
/// </summary>
public abstract class EntityUpdatedEvent<TEntity> : INotification where TEntity : BaseEntityWithoutId
{
    /// <summary>
    /// Gets the domain entity that was updated (in updated state).
    /// </summary>
    public TEntity UpdatedEntity { get; }

    /// <summary>
    /// Creates instance of the <see cref="EntityUpdatedEvent{TEntity}"/> class.
    /// </summary>
    /// <param name="updatedEntity">The domain entity that was updated.</param>
    protected EntityUpdatedEvent(TEntity updatedEntity)
    {
        UpdatedEntity = updatedEntity;
    }
}