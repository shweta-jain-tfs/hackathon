using MediatR;

namespace Pharos.Server.Domain.Common;

/// <summary>
/// Generic type for event that occurs when a new entity is added to the database.
/// </summary>
public abstract class EntityCreatedEvent<TEntity> : INotification where TEntity : BaseEntityWithoutId
{
    /// <summary>
    /// Gets the domain entity that was added
    /// </summary>
    public TEntity NewEntity { get; }

    /// <summary>
    /// Creates instance of the <see cref="EntityCreatedEvent{TEntity}"/> class.
    /// </summary>
    /// <param name="newEntity">The domain entity that was created.</param>
    protected EntityCreatedEvent(TEntity newEntity)
    {
        NewEntity = newEntity;
    }
}