namespace Pharos.Server.AuditTrail.Application.Queries;
public class AuditTrailRecordDto
{
    public int Id { get; set; }
    public string EventData { get; set; } = string.Empty;
    public DateTimeOffset Created { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty; // Used for displaying the event description in the UI
    public string? SearchText { get; set; } // Used for referring to the search term in the UI
}