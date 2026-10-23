using Pharos.Server.Domain.Common;

namespace Pharos.Server.Domain.Parameters.Methods;

#pragma warning disable CS8618
public class LabQualityControlTest : BaseQualityControlTest, IAuditCreationEntity
{
    public double? UpperLimit { get; set; }
    public double? LowerLimit { get; set; }

    /// <summary>
    /// The entity creation timestamp.
    /// </summary>
    public DateTimeOffset Created { get; set; }

    /// <summary>
    /// The ID of the user who created the entity.
    /// </summary>
    public Guid? CreatedBy { get; set; }
}