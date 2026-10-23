namespace Pharos.Server.Domain.Parameters;

/// <summary>
/// Lists possible unit of measure for the Quantity property in articles.
/// </summary>
public enum ArticleQuantityUnit
{
    /// <summary>
    /// The number of tests in a single carrier.
    /// </summary>
    Tests,

    /// <summary>
    /// The numbers of doses in the reagent package (cartridge cells, quality control vials or bottles).
    /// </summary>
    Doses,

    /// <summary>
    /// The volume of the reagent in milliliters (cartridge cells or bottles).
    /// </summary>
    Milliliters,

    /// <summary>
    /// Defines how many times the cartridge can be used.
    /// It is only applicable to calibrator and curve control cartridges.
    ///
    /// The size of a set is defined by the number of curve control points
    /// (or the number of points in the curve).
    /// The set size for curve controls can vary in different methods.
    /// </summary>
    Sets
}
