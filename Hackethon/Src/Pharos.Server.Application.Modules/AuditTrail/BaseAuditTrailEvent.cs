using Pharos.Server.Application.AuditTrail.Enums;

namespace Pharos.Server.Application.AuditTrail;

public abstract record BaseAuditTrailEvent : IAuditTrailEvent
{
    public abstract EventId EventId { get; }
    public List<AuditEventData> EventData { get; } = [];
    public string Comment { get; set; } = string.Empty;

    public CreateAuditTrailRecordCommand ToCommand()
    {
        return new CreateAuditTrailRecordCommand
        {
            EventId = EventId,
            EventData = EventData,
            Comment = Comment
        };
    }

    public void AddEventData<T>(EventDataFields field, T value)
    {
        var eventData = new AuditEventData();
        eventData.AddData(field, value);
        EventData.Add(eventData);
    }
}
