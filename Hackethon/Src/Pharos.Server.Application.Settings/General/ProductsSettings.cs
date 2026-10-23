using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Application.Settings.General;

public class ProductsSettings
{
    [DisplayNameLocal("GeneralSettings_ProductsSettings_Market")]
    public ProductsMarket Market { get; set; } = ProductsMarket.Worldwide;
}

public enum ProductsMarket
{
    [DisplayNameLocal("IntendedMarket_Worldwide")]
    Worldwide = 0,

    [DisplayNameLocal("IntendedMarket_Japan")]
    Japan = 1,

    [DisplayNameLocal("IntendedMarket_US")]
    US = 3,

    [DisplayNameLocal("IntendedMarket_Taiwan")]
    Taiwan = 4,

    [DisplayNameLocal("IntendedMarket_China")]
    China = 5,
}