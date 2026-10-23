using System.ComponentModel.DataAnnotations;
using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Application.Settings.Sample;

public class TubeBarcodesSettings
{
    [DisplayNameLocal("TubeBarcodeSettings_Code39")]
    public bool Code39 { get; set; } = true;

    [DependsFrom(nameof(Code39), "True", DependencyType.Disable)]
    [DisplayNameLocal("TubeBarcodeSettings_Code39Checksum")]
    public bool Code39Checksum { get; set; } = true;

    [DisplayNameLocal("TubeBarcodeSettings_Code93")]
    public bool Code93 { get; set; } = true;

    [DisplayNameLocal("TubeBarcodeSettings_Code128")]
    public bool Code128 { get; set; } = true;

    [DisplayNameLocal("TubeBarcodeSettings_Interleaved2Of5")]
    public bool Interleaved2Of5 { get; set; } = true;

    [DependsFrom(nameof(Interleaved2Of5), "True", DependencyType.Disable)]
    [DisplayNameLocal("TubeBarcodeSettings_Interleaved2Of5Checksum")]
    public bool Interleaved2Of5Checksum { get; set; } = true;

    [DisplayNameLocal("TubeBarcodeSettings_CodaBar")]
    public bool CodaBar { get; set; }

    [DisplayNameLocal("TubeBarcodeSettings_Ean8")]
    public bool Ean8 { get; set; }

    [DisplayNameLocal("TubeBarcodeSettings_Industrial2Of5")]
    public bool Industrial2Of5 { get; set; }

    [DisplayNameLocal("TubeBarcodeSettings_Coop2Of5")]
    public bool Coop2Of5 { get; set; }

    [DisplayNameLocal("TubeBarcodeSettings_QrCode")]
    public bool QrCode { get; set; }

    [DisplayNameLocal("TubeBarcodeSettings_DataMatrix")]
    public bool DataMatrix { get; set; }

    [DisplayNameLocal("TubeBarcodeSettings_Prefix1")]
    [MaxLength(10)]
    [DisallowedCharacters(@"[&\\^|]")]
    public string Prefix1 { get; set; } = "";

    [DisplayNameLocal("TubeBarcodeSettings_Prefix2")]
    [MaxLength(10)]
    [DisallowedCharacters(@"[&\\^|]")]
    public string Prefix2 { get; set; } = "";

    [DisplayNameLocal("TubeBarcodeSettings_Prefix3")]
    [MaxLength(10)]
    [DisallowedCharacters(@"[&\\^|]")]
    public string Prefix3 { get; set; } = "";

    [DisplayNameLocal("TubeBarcodeSettings_Prefix4")]
    [MaxLength(10)]
    [DisallowedCharacters(@"[&\\^|]")]
    public string Prefix4 { get; set; } = "";

    [DisplayNameLocal("TubeBarcodeSettings_Prefix5")]
    [MaxLength(10)]
    [DisallowedCharacters(@"[&\\^|]")]
    public string Prefix5 { get; set; } = "";
}
