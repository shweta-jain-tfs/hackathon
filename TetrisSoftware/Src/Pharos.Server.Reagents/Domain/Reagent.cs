using System.ComponentModel.DataAnnotations;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Common;

namespace Pharos.Server.Reagents.Domain;

/// <summary>
/// Represent a reagent used in one of the instruments.
/// </summary>
internal class Reagent : BaseEntity
{
    /// <summary>
    /// The reagent barcode. It is unique for a cartridge or a bottle but not unique for a carrier.
    /// </summary>
    [MaxLength(Constants.ArticleBarcodeLength)]
    public string Barcode { get; set; } = default!;

    /// <summary>
    /// The GS1 UDI in the human-readable format. Used to identify individual carriers.
    /// </summary>
    [MaxLength(Constants.MaxReagentCarrierUdi)]
    public string? Udi { get; set; }

    /// <summary>
    /// The reagent lot ID.
    /// </summary>
    public int LotId { get; set; }

    /// <summary>
    /// The time when this reagent was loaded in instrument.
    /// </summary>
    public DateTimeOffset? LoadTimestamp { get; set; }

    /// <summary>
    /// The value indicating whether the expiration flag of the carrier has been overridden by the user.
    /// The instrument can use this reagent.
    /// </summary>
    public bool OverrideExpiration { get; set; }

    #region Navigation properties

    public virtual Lot Lot { get; set; } = default!;
    public virtual IList<ReagentContent>? ReagentContents { get; set; }

    #endregion
}
