using MediatR;

namespace Pharos.Server.Application.Instrument;

/// <summary>
/// This event is published when a scanned barcode is not recognized by any module.
/// </summary>
public record UnknownBarcodeEvent(string InstrumentNumber, string Barcode) : INotification;

