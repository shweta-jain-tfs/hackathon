using Pharos.Server.Application.AuditTrail.Enums;

namespace Pharos.Server.Application.AuditTrail.Events;
public static class SettingsEvents
{
    private static TEvent CreateEvent<TEvent, TData>(Func<TData, TEvent> ctor, EventDataFields field, TData data)
        where TEvent : BaseAuditTrailEvent
    {
        var @event = ctor(data);
        @event.AddEventData(field, data);
        return @event;
    }

    public record SettingsExportedEvent(string Username) : BaseAuditTrailEvent
    {
        public override EventId EventId => EventId.SettingsExported;
        public static SettingsExportedEvent Create(string username)
        {
            return CreateEvent(static u => new SettingsExportedEvent(u), EventDataFields.Username, username);
        }
    }

    public record SettingsModifiedEvent(Dictionary<string, string> ModifiedSettings) : BaseAuditTrailEvent
    {
        public override EventId EventId => EventId.SettingsChanged;
        public static SettingsModifiedEvent Create(Dictionary<string, string> modifiedSettings)
        {
            return CreateEvent(static ms => new SettingsModifiedEvent(ms), EventDataFields.ModifiedSettings, modifiedSettings);
        }
    }

    public record SettingsImportedEvent(Dictionary<string, string> ModifiedSettings) : BaseAuditTrailEvent
    {
        public override EventId EventId => EventId.SettingsImported;
        public static SettingsImportedEvent Create(Dictionary<string, string> modifiedSettings)
        {
            return CreateEvent(static ms => new SettingsImportedEvent(ms), EventDataFields.ModifiedSettings, modifiedSettings);
        }
    }
}