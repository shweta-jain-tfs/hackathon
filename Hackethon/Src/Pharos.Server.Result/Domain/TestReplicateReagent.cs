using Pharos.Server.Domain;
using Pharos.Server.Domain.Enums;

namespace Pharos.Server.Result.Domain;

public class TestReplicateReagent
{
    public const int SerialNumberMaxLength = 8;

    public int TestReplicateId { get; set; }

    /// <summary>
    /// The type of the reagent.
    /// </summary>
    public ArticleType ReagentType { get; set; }

    /// <summary>
    /// The reagent barcode. All reagent will have primary barcode (1D, 2D) except bottles.
    /// </summary>
    [MaxLength(Constants.ArticleBarcodeLength)]
    public string Barcode { get; set; } = default!;

    /// <summary>
    /// The reagent Udi. All reagent will have UDI as secondary barcode but for bottles it will have only UDI.
    /// </summary>
    [MaxLength(Constants.ArticleUdiLength)]
    public string Udi { get; set; } = default!;

    /// <summary>
    /// The serial number of the reagent in the lot.
    /// </summary>
    [MaxLength(SerialNumberMaxLength)]
    public string SerialNumber { get; set; } = default!;

    /// <summary>
    /// The position number for a reagent cartridge call. Not used for carriers and bottles.
    /// </summary>
    public int CellPosition { get; set; }

    /// <summary>
    /// The string that uniquely identifies the lot (5-characters).
    /// The lot number is a unique combination of article identifier and lot number symbols from the reagent barcode.
    /// </summary>
    [MaxLength(Constants.LotNumberLength)]
    public string LotNumber { get; set; } = default!;

    /// <summary>
    /// This value indicating if the reagent was expired when used for this assay.
    /// </summary>
    public bool Expired { get; set; }
}
