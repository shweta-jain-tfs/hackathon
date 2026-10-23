using Pharos.Server.Domain.Enums;

namespace Pharos.Server.Instrument.Application.Queries.GetBarcodeDetails;

/// <summary>
/// Data transfer object for barcode identification results sent to clients via SignalR.
/// Encapsulates all information needed for client-side barcode routing and display.
/// </summary>
public record ScannedBarcodeDto
{
    /// <summary>
    /// The scanned barcode string
    /// </summary>
    public string Barcode { get; init; }

    /// <summary>
    /// The identified type of barcode (Carrier, Cartridge, Bottle, QualityControl, Tray, Sample, Unknown)
    /// </summary>
    public BarcodeType Type { get; init; }

    /// <summary>
    /// The Request ID associated with a sample barcode (0 if not applicable)
    /// </summary>
    public int RequestId { get; init; }

    /// <summary>
    /// True if this is a primary barcode (1D/2D), False if secondary (UDI).
    /// Used by client to show "Please scan UDI" toast and block modal for reagents.
    /// </summary>
    public bool IsPrimaryBarcode { get; init; }

    /// <summary>
    /// True if the bottles drawer is open.
    /// Used to start loading sequence for bottles if true, or handle bottle barcode scanned if false
    /// </summary>
    public bool IsBottleDrawerOpen { get; init; }

    /// <summary>
    /// True if a scanned tray is currently in a closed drawer.
    /// Used by the client to ask for confirmation before marking the tray as not in the instrument.
    /// </summary>
    public bool IsTrayInClosedDrawer { get; init; }

    /// <summary>
    /// True when an unknown barcode is eligible for the missing-request LIS query.
    /// </summary>
    public bool CanQueryMissingRequest { get; init; }

    public ScannedBarcodeDto(string barcode, BarcodeType type, int requestId = 0, bool isPrimaryBarcode = false,
        bool isBottleDrawerOpen = false, bool isTrayInClosedDrawer = false,
        bool canQueryMissingRequest = false)
    {
        Barcode = barcode;
        Type = type;
        RequestId = requestId;
        IsPrimaryBarcode = isPrimaryBarcode;
        IsBottleDrawerOpen = isBottleDrawerOpen;
        IsTrayInClosedDrawer = isTrayInClosedDrawer;
        CanQueryMissingRequest = canQueryMissingRequest;
    }
}