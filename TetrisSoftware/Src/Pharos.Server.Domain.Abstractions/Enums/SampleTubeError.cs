namespace Pharos.Server.Domain.Enums;

/// <summary>
/// Specifies the error that can occur with a tube.
/// </summary>
public enum SampleTubeError
{
    None = 0,

    TubeLoadedInWrongDrawer = 1,

    TubeShapeError = 2,

    TubeGripError = 3,

    UnreadableBarcode = 4,

    ClotDetected = 5,

    VolumeMeasurementError = 6,

    BarcodeMismatchError = 7,

    NoSeparationDetected = 8,

    SampleShortage = 9,

    /// <summary>
    /// The same tube barcode was detected more than once when it was prohibited by settings.
    /// </summary>
    DuplicateBarcode = 10,

    MissingTestOrders = 11,

    QcOutOfRange = 12,

    QcNotNeeded = 13,

    /// <summary>
    /// The barcode format is invalid (does not match the settings).
    /// </summary>
    InvalidBarcode = 14,

    MethodsNotActive = 15,

    MultipleBarcodes = 16,

    PipettingError = 17,
}