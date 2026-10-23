using System.ComponentModel.DataAnnotations;
using System.Drawing;
using Pharos.Server.Domain.Common;
using Pharos.Server.Domain.Enums;

namespace Pharos.Server.Domain.Parameters;

/// <summary>
/// Physical article manufactured by Thermo Fisher IDD.
/// Can be one of different types, for example carriers, conjugate bottles, curve control strips, ...
/// </summary>
public class Article : BaseAuditableEntity
{
    public const int FullNameLength = 55;
    public const int FriendlyNameLength = 50;
    public const int IdentityLength = 16;
    public const int TextFieldLength = 20;

    /// <summary>
    /// Unique article number (alphanumerical). Is included in the beginning of the article's barcode
    /// </summary>
    [MaxLength(Constants.ArticleNumberLength)]
    public string ArticleNumber { get; set; } = default!;

    /// <summary>
    /// Type of article
    /// </summary>
    public ArticleType ArticleType { get; set; }

    /// <summary>
    /// The describing name of the article (matching the product catalog)
    /// For example "ImmunoCap Specific IgE Conjugate"
    /// </summary>
    [MaxLength(FullNameLength)]
    public string? FullName { get; set; }

    /// <summary>
    /// The nominal quantity of the reagent. Unit of measure is defined by <see cref="UnitOfMeasure"/>.
    /// See remarks for more information.
    /// </summary>
    /// <remarks>
    /// For calibrator and curve control cartridges, this value is the number of "sets" in the cartridge.
    /// For other cartridges, this value is the volume of one cartridge cell.
    /// </remarks>
    public int Quantity { get; set; }

    /// <summary>
    /// The unit of measure for the <see cref="Quantity"/> property.
    /// </summary>
    public ArticleQuantityUnit UnitOfMeasure => ArticleType switch
    {
        ArticleType.ImmunoCapCarrier => ArticleQuantityUnit.Tests,
        ArticleType.EliaWellCarrier => ArticleQuantityUnit.Tests,
        ArticleType.EliaCalibratorWellCarrier => ArticleQuantityUnit.Tests,
        ArticleType.DilutionWellCarrier => ArticleQuantityUnit.Tests,
        ArticleType.ConjugateCartridge => ArticleQuantityUnit.Doses,
        ArticleType.QualityControlCartridge => ArticleQuantityUnit.Doses,
        ArticleType.DiluentCartridge => ArticleQuantityUnit.Milliliters,
        ArticleType.DevelopmentSolutionCartridge => ArticleQuantityUnit.Milliliters,
        ArticleType.StopSolutionBottle => ArticleQuantityUnit.Milliliters,
        ArticleType.WashAdditiveBottle => ArticleQuantityUnit.Milliliters,
        ArticleType.WashConcentrateBottle => ArticleQuantityUnit.Milliliters,
        ArticleType.MaintenanceConcentrateBottle => ArticleQuantityUnit.Milliliters,
        ArticleType.CurveControlCartridge => ArticleQuantityUnit.Sets,
        ArticleType.CalibratorCartridge => ArticleQuantityUnit.Sets,
        ArticleType.FluoroCVial => ArticleQuantityUnit.Milliliters,
        ArticleType.QualityControlTube => ArticleQuantityUnit.Doses,
        ArticleType.None => ArticleQuantityUnit.Doses,
        _ => ArticleQuantityUnit.Doses
    };

    /// <summary>
    /// The number of cells in the cartridge. Used only for reagents packaged in cartridges.
    /// </summary>
    public int? NumberOfCells { get; set; }

    /// <summary>
    /// The value indicate the max number of allowed pierces holes (per cell)
    /// Used to be able to use 2 pipettes at same time to meet throughput requirements.
    /// Some cartridge will only allow 1 hole pierced to meet evaporation requirements. 
    /// </summary>
    public int? MaxPiercedHoles { get; set; }

    /// <summary>
    /// Name of method this article is made for.
    /// For example "sIgE" for "ImmunoCap Specific IgE Conjugate"
    /// </summary>
    [MaxLength(Constants.MethodNameLength)]
    public string? MethodName { get; set; }

    /// <summary>
    /// Identity of the article
    /// For example "t1" for a carrier.
    /// Not all articles have an identity
    /// </summary>
    [MaxLength(IdentityLength)]
    public string? Identity { get; set; }

    public int? MaxDaysInInstrument { get; set; }

    /// <summary>
    /// The maximum number of hours the reagent can be used after the cartridge foil has been pierced.
    /// </summary>
    public int? MaxHoursInInstrument { get; set; }

    /// <summary>
    /// Article may be defined, but is not visible in the system at the moment
    /// For example: Articles used for method "View" is hidden until the "View" method is made visible
    /// </summary>
    public bool Hidden { get; set; }

    /// <summary>
    /// Friendly name of the article
    /// </summary>
    [MaxLength(FriendlyNameLength)]
    public string? FriendlyName { get; set; }

    /// <summary>
    /// An extra label used to represent the article in the Pharos UI.
    /// For cartridges, this is a method name(printed on the label).
    /// Cartridges can be used in up to 4 assay methods.
    /// </summary>
    [MaxLength(TextFieldLength)]
    public string? Text1 { get; set; }

    /// <summary>
    /// An extra label used to represent the article in the Pharos UI.
    /// For cartridges, this is a method name(printed on the label).
    /// Cartridges can be used in up to 4 assay methods.
    /// </summary>
    [MaxLength(TextFieldLength)]
    public string? Text2 { get; set; }

    /// <summary>
    /// An extra label used to represent the article in the Pharos UI.
    /// For cartridges, this is a method name(printed on the label).
    /// Cartridges can be used in up to 4 assay methods.
    /// </summary>
    [MaxLength(TextFieldLength)]
    public string? Text3 { get; set; }

    /// <summary>
    /// An extra label used to represent the article in the Pharos UI.
    /// For cartridges, this is a method name(printed on the label).
    /// Cartridges can be used in up to 4 assay methods.
    /// </summary>
    [MaxLength(TextFieldLength)]
    public string? Text4 { get; set; }

    /// <summary>
    /// The shape of the article indicator in Pharos UI.
    /// </summary>
    public ArticleIndicatorShape Shape { get; set; }

    /// <summary>
    /// Color of the article indicator in UI.
    /// </summary>
    public Color? Color1 { get; set; }

    /// <summary>
    /// Color of the article indicator in UI.
    /// Used together with <see cref="Text2"/>. Not used for carriers.
    /// </summary>
    public Color? Color2 { get; set; }

    /// <summary>
    /// Color of the article indicator in UI.
    /// Used together with <see cref="Text3"/>. Not used for carriers.
    /// </summary>
    public Color? Color3 { get; set; }

    /// <summary>
    /// Color of the article indicator in UI.
    /// Used together with <see cref="Text4"/>. Not used for carriers.
    /// </summary>
    public Color? Color4 { get; set; }

    public List<string?> Texts => [Text1, Text2, Text3, Text4];

    public List<Color?> Colors => [Color1, Color2, Color3, Color4];

    #region Navigation properties

    public virtual IList<CartridgeCellIdentity>? CartridgeCells { get; set; }

    #endregion
}