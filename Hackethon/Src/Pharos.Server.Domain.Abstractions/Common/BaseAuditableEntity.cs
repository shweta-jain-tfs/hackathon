namespace Pharos.Server.Domain.Common;

/// <summary>
/// Represents a base class for the entities that require an audit log.
/// </summary>
public abstract class BaseAuditableEntity : BaseEntity, IAuditCreationEntity, IAuditModificationsEntity
{
    /// <summary>
    /// The entity creation timestamp.
    /// </summary>
    public DateTimeOffset Created { get; set; }

    /// <summary>
    /// The ID of the user who created the entity.
    /// </summary>
    public Guid? CreatedBy { get; set; }

    /// <summary>
    /// The entity's last modification timestamp.
    /// </summary>
    public DateTimeOffset? LastModified { get; set; }

    /// <summary>
    /// The ID of the user who modified the entity.
    /// </summary>
    public Guid? LastModifiedBy { get; set; }
}