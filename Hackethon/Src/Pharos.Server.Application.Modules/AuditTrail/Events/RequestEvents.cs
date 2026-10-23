using Pharos.Server.Application.AuditTrail.Enums;

namespace Pharos.Server.Application.AuditTrail.Events;

public static class RequestEvents
{
    public record RequestsImportedEvent : BaseAuditTrailEvent
    {
        public override EventId EventId => EventId.RequestsImported;

        public static RequestsImportedEvent Create()
        {
            return new RequestsImportedEvent();
        }
    }
}
