namespace Pharos.Server.Sample.Domain;

public enum SampleTrayError
{
    /// <summary>
    /// No error.
    /// </summary>
    None = 0,

    /// <summary>
    /// More than one tray with same barcode in the lab.
    /// </summary>
    DuplicateBarcode = 1,

    /// <summary>
    /// The barcode of the tray was unreadable by the instrument.
    /// </summary>
    UnreadableBarcode = 2,

    /// <summary>
    /// The barcode read was invalid (contains characters not valid for a sample tray barcode).
    /// </summary>
    InvalidBarcode = 3,

    /// <summary>
    /// The instrument has detected an unknown object in the Sample Tray position
    /// </summary>
    UnknownObject = 4
}