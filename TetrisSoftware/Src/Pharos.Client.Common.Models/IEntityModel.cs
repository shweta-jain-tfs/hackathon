namespace Pharos.Client.Common.Models;

/// <summary>
/// Defines a client-side abstraction of a domain model in the Model-View-ViewModel design pattern.
/// </summary>
public interface IEntityModel<TKey>
{
    /// <summary>
    /// Gets the data entity ID.
    /// </summary>
    TKey Id { get; set; }
}
