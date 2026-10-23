#pragma warning disable CS8618
using System.ComponentModel.DataAnnotations;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Common;

namespace Pharos.Server.Request.Domain;

/// <summary>
/// Custom parameters for a test order to be used when sending it to an instrument
/// </summary>
internal class TestOrderCustomParameter : BaseEntity
{
    public int TestOrderId { get; set; }

    [MaxLength(Constants.LotNumberLength)]
    public string CarrierLot { get; set; }

    [MaxLength(Constants.LotNumberLength)]
    public string DiluentLot { get; set; }

    [MaxLength(Constants.LotNumberLength)]
    public string ConjugateLot { get; set; }

    [MaxLength(Constants.LotNumberLength)]
    public string DevelopmentLot { get; set; }

    [MaxLength(50)]
    public string CustomRunId { get; set; }

    [MaxLength(50)]
    public string CustomTestId { get; set; }

    #region Navigation properties

    /// <summary>
    /// The test order navigation property
    /// </summary>
    public virtual TestOrder TestOrder { get; set; }

    #endregion
}