using Pharos.Server.Application.AuditTrail.Enums;
using Pharos.Server.Domain.Common;

namespace Pharos.Server.Application.AuditTrail;

public interface IAuditTrailEvent
{
    EventId EventId { get; }
    [ProtectedData] List<AuditEventData> EventData { get; }
    [ProtectedData] string Comment { get; }

    CreateAuditTrailRecordCommand ToCommand();
}
