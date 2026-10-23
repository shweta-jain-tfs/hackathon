using MediatR;

namespace Pharos.Server.Application.Instrument;

/// <summary>
/// This event is published every time when a barcode is scanned on the instrument's external barcode reader.
/// </summary>
public class BarcodeScannedEvent(string instrumentNumber, string barcode) : INotification
{
    public string InstrumentNumber { get; init; } = instrumentNumber;
    public string Barcode { get; init; } = barcode;

    public bool Handled { get; set; }
}
