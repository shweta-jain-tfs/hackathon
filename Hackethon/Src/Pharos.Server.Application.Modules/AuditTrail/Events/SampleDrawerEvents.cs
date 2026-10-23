using Pharos.Server.Application.AuditTrail.Enums;
using Pharos.Server.Domain.Enums;

namespace Pharos.Server.Application.AuditTrail.Events;

public static class SampleDrawerEvents
{
    public record SampleDrawerUnlockEvent(string InstrumentSerialNumber, DrawerType DrawerType, DateTimeOffset EventTime) : BaseAuditTrailEvent
    {
        public override EventId EventId => EventId.SampleDrawerUnlock;

        public static SampleDrawerUnlockEvent Create(string instrumentSerialNumber, DrawerType drawerType, DateTimeOffset eventTime)
        {
            var @event = new SampleDrawerUnlockEvent(instrumentSerialNumber, drawerType, eventTime);

            @event.AddEventData(EventDataFields.InstrumentSerialNumber, instrumentSerialNumber);
            @event.AddEventData(EventDataFields.DrawerType, drawerType.ToString());
            @event.AddEventData(EventDataFields.EventTime, eventTime);
            return @event;
        }
    }

    public record SampleDrawerOpenedEvent(string InstrumentSerialNumber, DrawerType DrawerType, DateTimeOffset EventTime) : BaseAuditTrailEvent
    {
        public override EventId EventId => EventId.SampleDrawerOpened;

        public static SampleDrawerOpenedEvent Create(string instrumentSerialNumber, DrawerType drawerType, DateTimeOffset eventTime)
        {
            var @event = new SampleDrawerOpenedEvent(instrumentSerialNumber, drawerType, eventTime);

            @event.AddEventData(EventDataFields.InstrumentSerialNumber, instrumentSerialNumber);
            @event.AddEventData(EventDataFields.DrawerType, drawerType.ToString());
            @event.AddEventData(EventDataFields.EventTime, eventTime);
            return @event;
        }
    }

    public record SampleDrawerClosedEvent(string InstrumentSerialNumber, DrawerType DrawerType, DateTimeOffset EventTime) : BaseAuditTrailEvent
    {
        public override EventId EventId => EventId.SampleDrawerClosed;

        public static SampleDrawerClosedEvent Create(string instrumentSerialNumber, DrawerType drawerType, DateTimeOffset eventTime)
        {
            var @event = new SampleDrawerClosedEvent(instrumentSerialNumber, drawerType, eventTime);

            @event.AddEventData(EventDataFields.InstrumentSerialNumber, instrumentSerialNumber);
            @event.AddEventData(EventDataFields.DrawerType, drawerType.ToString());
            @event.AddEventData(EventDataFields.EventTime, eventTime);
            return @event;
        }
    }

    public record SampleDrawerUnauthorizedUnlockAttemptEvent(string InstrumentSerialNumber, DrawerType DrawerType, DateTimeOffset EventTime) : BaseAuditTrailEvent
    {
        public override EventId EventId => EventId.SampleDrawerUnauthorizedUnlockAttempt;
        public static SampleDrawerUnauthorizedUnlockAttemptEvent Create(string instrumentSerialNumber, DrawerType drawerType, DateTimeOffset eventTime)
        {
            var @event = new SampleDrawerUnauthorizedUnlockAttemptEvent(instrumentSerialNumber, drawerType, eventTime);

            @event.AddEventData(EventDataFields.InstrumentSerialNumber, instrumentSerialNumber);
            @event.AddEventData(EventDataFields.DrawerType, drawerType.ToString());
            @event.AddEventData(EventDataFields.EventTime, eventTime);
            return @event;
        }
    }
}