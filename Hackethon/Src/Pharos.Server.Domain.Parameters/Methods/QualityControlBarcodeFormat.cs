using System.ComponentModel.DataAnnotations;
using Pharos.Server.Domain.Common;

namespace Pharos.Server.Domain.Parameters.Methods;

#pragma warning disable CS8618
public class QualityControlBarcodeFormat : BaseEntity
{
    [MaxLength(Constants.QualityControlBarcodeFormatNameLength)]
    public string Name { get; set; }
    public int TotalLength { get; set; }
    public int QcStartPosition { get; set; }
    public int QcLength { get; set; }
    public bool ContainsLotInformation { get; set; }
    public int LotStartPosition { get; set; }
    public int LotLength { get; set; }
}