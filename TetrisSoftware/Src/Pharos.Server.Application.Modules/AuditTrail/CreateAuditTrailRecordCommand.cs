using MediatR;
using Pharos.Server.Application.AuditTrail.Enums;

namespace Pharos.Server.Application.AuditTrail
{
    public class CreateAuditTrailRecordCommand : IRequest<bool>
    {
        public EventId EventId { get; set; }
        public List<AuditEventData> EventData { get; set; } = [];
        public EventLevel Level { get; set; }
        public Category Category { get; set; }
        public string Comment{ get; set; } = string.Empty;
    }
}
