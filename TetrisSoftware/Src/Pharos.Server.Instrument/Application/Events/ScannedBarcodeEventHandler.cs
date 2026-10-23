using MediatR;
using Pharos.Server.Application.Instrument;

namespace Pharos.Server.Instrument.Application.Events;

public record ScannedBarcodeEvent(string InstrumentNumber, string Barcode) : INotification;
internal class ScannedBarcodeEventHandler(ICurrentInstrumentServerHub currentInstrument, IInstrumentManager instrumentManager) : INotificationHandler<ScannedBarcodeEvent>
{
    public async Task Handle(ScannedBarcodeEvent notification, CancellationToken ct)
    {
        if (instrumentManager.FindInstrument(notification.InstrumentNumber) is Domain.Instrument instrument && !string.IsNullOrWhiteSpace(notification.Barcode))
        {
            await currentInstrument.SendScannedBarcode(instrument.Id, notification.Barcode);
        }
    }
}