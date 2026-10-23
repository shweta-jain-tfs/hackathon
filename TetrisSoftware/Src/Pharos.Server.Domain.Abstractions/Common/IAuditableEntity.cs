namespace Pharos.Server.Domain.Common;

public interface IAuditableEntity
{
}

public interface IAuditCreationEntity : IAuditableEntity
{
    /// <summary>
    /// The entity creation timestamp.
    /// </summary>
    public DateTimeOffset Created { get; set; }

    /// <summary>
    /// The ID of the user who created the entity.
    /// </summary>
    public Guid? CreatedBy { get; set; }
}

public interface IAuditModificationsEntity : IAuditableEntity
{

    /// <summary>
    /// The entity's last modification timestamp.
    /// </summary>
    public DateTimeOffset? LastModified { get; set; }

    /// <summary>
    /// The ID of the user who modified the entity.
    /// </summary>
    public Guid? LastModifiedBy { get; set; }
}
