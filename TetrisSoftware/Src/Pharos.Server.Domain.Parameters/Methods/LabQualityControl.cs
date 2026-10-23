using System.ComponentModel.DataAnnotations;

namespace Pharos.Server.Domain.Parameters.Methods;

#pragma warning disable CS8618
public class LabQualityControl : BaseQualityControl
{
    [MaxLength(Constants.QualityControlIdOnBarcodeLength)]
    public string IdOnBarcode { get; set; }

    public QualityControlBarcodeFormat? BarcodeFormat { get; set; }
    public int? BarcodeFormatId { get; set; }
}