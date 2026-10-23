using System.ComponentModel.DataAnnotations;
using Pharos.Server.Domain.Common;

namespace Pharos.Server.Domain.Reflex;

/// <summary>
/// Represents a reflex test that is associated with a reflex definition.
/// </summary>
public class ReflexTest : BaseEntity, IAuditCreationEntity
{
    /// <summary>
    /// The reflex test name.
    /// </summary>
    [MaxLength(Constants.TestNameLength)]
    public string TestName { get; set; } = null!;

    /// <summary>
    /// The reflex method name.
    /// </summary>
    [MaxLength(Constants.MethodNameLength)]
    public string MethodName { get; set; } = null!;

    /// <summary>
    /// The reflex nr of replicates.
    /// </summary>
    public int NumberOfReplicates { get; set; }

    /// <summary>
    /// The reflex instr dilution factor.
    /// </summary>
    public int InstrumentDilutionFactor { get; set; }

    /// <summary>
    /// The order in which the test must be executed on a particular sample
    /// (if there is more than one for a sample).
    /// </summary>
    /// <remarks>
    /// If the patient sample may not
    /// have enough liquid to perform all prescribed tests, the doctor can set
    /// the test order so that more important tests are executed first.
    /// </remarks>
    public int TestSortOrder { get; set; }

    /// <summary>
    /// The entity creation timestamp.
    /// </summary>
    public DateTimeOffset Created { get; set; }

    /// <summary>
    /// The ID of the user who created the entity.
    /// </summary>
    public Guid? CreatedBy { get; set; }
}
