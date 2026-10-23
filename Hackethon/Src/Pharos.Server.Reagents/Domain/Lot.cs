using System.ComponentModel.DataAnnotations;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Common;

namespace Pharos.Server.Reagents.Domain;

/// <summary>
/// Represents a lot (batch) of consumables (articles) produced by Thermo Fisher
/// and used in the instruments.
/// </summary>
internal class Lot : BaseEntity
{
    /// <summary>
    /// The type of the article in this lot.
    /// </summary>
    public ArticleType ArticleType { get; set; }

    /// <summary>
    /// The string that uniquely identifies the lot (5-characters).
    /// The lot number is a unique combination of article identifier and lot number symbols from the reagent barcode.
    /// </summary>
    [MaxLength(Constants.LotNumberLength)]
    public string LotNumber { get; set; } = default!;

    /// <summary>
    /// The expiration date (last day of use) of this reagent lot.
    /// After a reagent is open, its expiration date no longer equals to the lot expiration date.
    /// </summary>
    public DateTimeOffset ExpiryDate { get; set; }

    #region Navigation properties

    public virtual IList<Reagent> Reagents { get; set; } = default!;

    #endregion
}
