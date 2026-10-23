using MediatR;
using Pharos.Server.Application.Instrument;
using Tetris.Protocol.Pharos.Files;
using ICurrentInstrumentServerHub = Pharos.Server.Application.Instrument.ICurrentInstrumentServerHub;

namespace Pharos.Server.Instrument.Application.Events;

internal class InstrumentUsbEventHandler(
    IInstrumentManager instrumentManager,
    ICurrentInstrumentServerHub currentInstrumentHub) : INotificationHandler<InstrumentUsbEvent>
{
    public Task Handle(InstrumentUsbEvent notification, CancellationToken cancellationToken)
    {
        var instrument = instrumentManager.GetInstrument(notification.InstrumentNumber);
        var usbEvent = notification.UsbEvent;
        string message;
        if (usbEvent.Type == EnumUsbEventType.Inserted)
        {
            message = "USB Inserted (Drive " + usbEvent.DriveLetter + ")";
        }
        else if (usbEvent.Type == EnumUsbEventType.Removed)
        {
            message = "USB Removed (Drive " + usbEvent.DriveLetter + ")";
        }
        else if (usbEvent.Type == EnumUsbEventType.Error)
        {
            message = "USB Error (Drive " + usbEvent.DriveLetter + ")";
        }
        else if (usbEvent.Type == EnumUsbEventType.UnspecifiedUsbEvent)
        {
            message = "Unspecified USB Event (Drive " + usbEvent.DriveLetter + ")";
        }
        else
        {
            message = "Unknown USB Event (Drive " + usbEvent.DriveLetter + ")";
        }

        return currentInstrumentHub.ShowMessageBox(instrument.Id, message);
    }
}
