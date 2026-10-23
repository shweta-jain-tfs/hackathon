using System.ComponentModel.DataAnnotations;
using Pharos.Server.Domain.Common;

#pragma warning disable CS8618

namespace Pharos.Server.Domain.Parameters;

/// <summary>
/// A package with a number of articles sent to the customer
/// Information of which articles are included can be defined in <see cref="PackageItem"/>
/// </summary>
public class Package : BaseAuditableEntity
{
    public const int PackageBarcodeLength = 3;
    public const int OrderingNumberLength = 15;
    public const int PackageFullNameLength = 55;

    [MaxLength(PackageBarcodeLength)]
    public string PackageBarcode { get; set; }

    /// <summary>
    /// The order number to use when ordering this package from ThermoFisher IDD
    /// For example: "10-9251-02" for a package including one bottle of "Total IgE conjugate"
    /// </summary>
    [MaxLength(OrderingNumberLength)]
    public string OrderingNumber { get; set; }

    /// <summary>
    /// The long describing name of the package
    /// </summary>
    [MaxLength(PackageFullNameLength)]
    public string? PackageFullName { get; set; }

    public int IntendedMarketId { get; set; }

    /// <summary>
    /// Package may be defined, but is not visible in the system at the moment
    /// For example: Packages used for method "View" is hidden until the "View" method is made visible
    /// </summary>
    public bool Hidden { get; set; }

    #region Navigation properties

    /// <summary>
    /// This package is intended to a specific market
    /// </summary>
    public IntendedMarket IntendedMarket { get; set; }

    /// <summary>
    /// The navigation property to reach the contents of the package
    /// </summary>
    public virtual IList<PackageItem> PackageItems { get; set; }

    #endregion

}