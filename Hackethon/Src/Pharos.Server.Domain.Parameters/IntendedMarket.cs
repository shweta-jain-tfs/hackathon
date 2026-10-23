using System.ComponentModel.DataAnnotations;

namespace Pharos.Server.Domain.Parameters;

public class IntendedMarket
{
    /// <summary>
    /// This market includes all countries, except the ones listed in this enum.
    /// </summary>
    public const int Worldwide = 0;

    public const int Japan = 1;
    // ReSharper disable once InconsistentNaming.
    public const int US = 3;
    public const int Taiwan = 4;
    public const int China = 5;
    public const int NameLength = 30;

    public static readonly int[] PredefinedMarketsIds = [Worldwide, Japan, US, Taiwan, China];

    public int Id { get; set; }

    [MaxLength(NameLength)]
    public string Name { get; set; } = default!;

    #region Navigation properties

    public virtual IList<Package> Packages { get; set; } = default!;

    #endregion
}