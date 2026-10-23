using System.ComponentModel.DataAnnotations;

namespace Pharos.Server.Domain.Parameters;

/// <summary>
/// Represents an identity of the cartridge cell.
/// Needed to identify individual cells of calibrator and curve control cartridges.
/// </summary>
public class CartridgeCellIdentity
{
    public const int CellIdentityLength = 16;

    public int ArticleId { get; set; }

    public int CellPosition { get; set; }

    [MaxLength(CellIdentityLength)]
    public string CellIdentity { get; set; } = default!;

    #region Navigation properties

    public virtual Article Article { get; set; } = default!;

    #endregion
}
