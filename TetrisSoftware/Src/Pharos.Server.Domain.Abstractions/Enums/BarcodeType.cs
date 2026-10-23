namespace Pharos.Server.Domain.Enums;

/// <summary>
/// Represents the type of barcode identified by the instrument
/// </summary>
public enum BarcodeType
{
    Carrier,
    Cartridge,
    Bottle,
    QualityControl,
    Tray,
    Sample,
    UnknownBarcode
}