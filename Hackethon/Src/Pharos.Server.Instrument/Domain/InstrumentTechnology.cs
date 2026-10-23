using Pharos.Server.Domain.Common;
using Pharos.Server.Domain.Enums;

namespace Pharos.Server.Instrument.Domain;

/// <summary>
/// Represents an assay technology that is available for an instrument.
/// </summary>
internal class InstrumentTechnology : BaseEntity
{
    /// <summary>
    /// Gets or sets the identifier of the instrument for which the assay technology is available.
    /// </summary>
    public int InstrumentId { get; set; }

    /// <summary>
    /// Gets or sets the assay technology available for the instrument.
    /// </summary>
    public AssayTechnology Technology { get; set; }

    /// <summary>
    /// Indicates whether the assay technology is active for the instrument.
    /// </summary>
    public bool IsActive { get; set; }

    #region Navigation properties

    /// <summary>
    /// Navigation property for the associated instrument.
    /// </summary>
    public virtual Instrument Instrument { get; set; } = default!;

    #endregion
}
