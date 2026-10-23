using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using MediatR;

namespace Pharos.Server.Domain.Common;

/// <summary>
/// Represents the base domain entity.
/// </summary>
public abstract class BaseEntity : BaseEntityWithoutId
{
    /// <summary>
    /// Gets or sets the entity identifier.
    /// </summary>
    public int Id { get; set; }
}

/// <summary>
/// Represents the base domain entity.
/// </summary>
public abstract class BaseEntityWithoutId
{
    private readonly List<INotification> _domainEvents = [];

    /// <summary>
    /// Gets a collection of domain events associated with the entity.
    /// Domain events are dispatched (fired) when the changes made to a domain object
    /// are persisted (e.g, saved object is saved to the database).
    /// </summary>
    [NotMapped]
    [JsonIgnore]
    public IReadOnlyCollection<INotification> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Adds a domain event to the DomainEvents collection.
    /// Domain events are published when the changes made to a domain object
    /// are persisted (e.g, saved object is saved to the database).
    /// </summary>
    /// <param name="domainEvent">The domain event object.</param>
    public void AddDomainEvent(INotification domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Removes a domain event from the DomainEvents collection.
    /// </summary>
    /// <param name="domainEvent">The domain event object.</param>
    public void RemoveDomainEvent(INotification domainEvent)
    {
        _domainEvents.Remove(domainEvent);
    }

    /// <summary>
    /// Clears all events from the entity.
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
