using Pharos.Server.Domain.Common;

namespace Pharos.Server.Reagents.Domain;

/// <summary>
/// Information about the current content of a reagent container (carrier, cartridge cell, or bottle).
/// </summary>
/// <remarks>
/// The primary reason why its entity is separated from <see cref="Reagent"/> is that cartridges have more
/// than one cell which is to be tracked separately.
/// </remarks>
internal class ReagentContent : BaseEntity
{
    public int ReagentId { get; set; }

    /// <summary>
    /// The position number for a reagent cartridge call. Valid values are: 1-12 for cartridges, 0 - for others.
    /// </summary>
    public int CellPosition { get; set; }

    /// <summary>
    /// The remaining quantity of the reagent (the unit of measure depends on the reagent type).
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// The value defining if the liquid in a container (cell) is unusable (due to an error).
    /// </summary>
    public bool Unusable { get; set; }

    /// <summary>
    /// The time when the reagent container was open.
    /// </summary>
    public DateTimeOffset? OpenTimestamp { get; set; }

    /// <summary>
    /// The expiration date of this reagent. By default, it is equal to the reagent lot's expiration date.
    /// After the reagent is open it gets another expiration date (usually, it's days from opening date).
    /// </summary>
    public DateTimeOffset ExpiryDate { get; set; }
}
