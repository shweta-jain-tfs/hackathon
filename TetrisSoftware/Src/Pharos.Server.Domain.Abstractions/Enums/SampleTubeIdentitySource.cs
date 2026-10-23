namespace Pharos.Server.Domain.Enums;

public enum SampleTubeIdentitySource
{
    /// <summary>
    /// The tube is identified by the instrument through the barcode scanning.
    /// </summary>
    BarcodeScanning = 0,

    /// <summary>
    /// The tube is identified by the user though the manual input of the barcode (or sample ID).
    /// </summary>
    ManualInput = 1,

    /// <summary>
    /// The tube is identified by LIS (fixed position in the tray) or by the request import.
    /// </summary>
    External = 2,

    /// <summary>
    /// Waiting for barcode to be confirmed by the user.
    /// </summary>
    ConfirmManualInput = 3,
}