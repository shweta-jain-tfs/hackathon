using System.ComponentModel.DataAnnotations;
using Pharos.Server.Domain.Common;

namespace Pharos.Server.Domain.Parameters.Methods;

/// <summary>
/// Parameters for the reagents used by corresponding method
/// </summary>
public class MethodReagent : BaseAuditableEntity
{
    public const int ReagentIdentityLength = 8;

    /// <summary>
    /// The ID for the method this reagent parameters belongs to
    /// </summary>
    public int MethodId { get; set; }

    /// <summary>
    /// Type of reagent
    /// </summary>
    /// <remarks>
    /// Sample, Conjugate, Development solution, Stop solution 
    /// </remarks>
    public ReagentType ReagentType { get; set; }

    /// <summary>
    /// The identity of the reagent.
    /// </summary>
    public string? ReagentIdentity
    {
        get
        {
            if (DefaultReagentIdentity == null)
            {
                // reagent cannot have the identity
                return null;
            }
            return UserReagentIdentity ?? DefaultReagentIdentity;
        }
    }

    /// <summary>
    /// The default value for the <see cref="ReagentIdentity"/> property provided in the method update file.
    /// </summary>
    [MaxLength(ReagentIdentityLength)]
    public string? DefaultReagentIdentity { get; set; }

    /// <summary>
    /// The user-provided value for the <see cref="ReagentIdentity"/> property.
    /// </summary>
    [MaxLength(ReagentIdentityLength)]
    public string? UserReagentIdentity { get; set; }

    /// <summary>
    /// Volume in microliters to use per replicate
    /// </summary>
    public int Volume { get; set; }

    #region Navigation properties

    /// <summary>
    /// The navigation property for the method this reagent parameters belongs to
    /// </summary>
    public virtual Method Method { get; set; } = default!;

    #endregion
}