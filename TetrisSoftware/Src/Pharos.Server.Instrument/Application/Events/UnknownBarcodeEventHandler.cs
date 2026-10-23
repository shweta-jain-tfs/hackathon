using MediatR;
using Pharos.Server.Application.Instrument;

namespace Pharos.Server.Instrument.Application.Events;

internal class UnknownBarcodeEventHandler(
    IInstrumentManager instrumentManager,
    ICurrentInstrumentServerHub currentInstrumentHub)
    : INotificationHandler<UnknownBarcodeEvent>
{
    public async Task Handle(UnknownBarcodeEvent notification, CancellationToken cancellationToken)
    {
        if (instrumentManager.FindInstrument(notification.InstrumentNumber) is Domain.Instrument instrument)
        {
            await currentInstrumentHub.UnknownBarcodeScanned(instrument.Id, notification.Barcode);
        }
    }
}
