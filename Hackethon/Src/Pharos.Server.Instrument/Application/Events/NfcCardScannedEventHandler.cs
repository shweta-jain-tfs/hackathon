using MediatR;
using Pharos.Server.Application.Instrument;
using ICurrentInstrumentServerHub = Pharos.Server.Application.Instrument.ICurrentInstrumentServerHub;

namespace Pharos.Server.Instrument.Application.Events;

internal class NfcCardScannedEventHandler(
    IInstrumentManager instrumentManager,
    ICurrentInstrumentServerHub currentInstrumentHub) : INotificationHandler<NfcCardScannedEvent>
{
    public Task Handle(NfcCardScannedEvent notification, CancellationToken cancellationToken)
    {
        var instrument = instrumentManager.GetInstrument(notification.InstrumentNumber);
        return currentInstrumentHub.ShowMessageBox(instrument.Id, $"NFC Scanned (UID {notification.ChipUid})");
    }
}
