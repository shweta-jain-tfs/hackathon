using Pharos.Server.Application.AuditTrail.Enums;

namespace Pharos.Server.Application.AuditTrail.Events;
public static class SampleTrayPositionEvents
{
    public record ManuallyEnteredBarcodeEvent(string InstrumentSerialNumber, string TrayId, int Position, string Barcode, DateTimeOffset EventTime) : BaseAuditTrailEvent
    {
        public override EventId EventId => EventId.ManualEntry;

        public static ManuallyEnteredBarcodeEvent Create(string instrumentSerialNumber, string trayId, int position, string barcode, DateTimeOffset eventTime)
        {
            var @event = new ManuallyEnteredBarcodeEvent(instrumentSerialNumber, trayId, position, barcode, eventTime);

            @event.AddEventData(EventDataFields.InstrumentSerialNumber, instrumentSerialNumber);
            @event.AddEventData(EventDataFields.TrayId, trayId);
            @event.AddEventData(EventDataFields.TubePosition, position);
            @event.AddEventData(EventDataFields.TubeBarcode, barcode);
            @event.AddEventData(EventDataFields.EventTime, eventTime);
            return @event;
        }
    }
}
