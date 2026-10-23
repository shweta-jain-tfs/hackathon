using System.ComponentModel.DataAnnotations;
using Pharos.Server.Domain;

namespace Pharos.Server.AuditTrail.Domain;
public class AuditTrailRecordNotes
{
    [Key]
    public int Id { get; set; }

    public int AuditTrailRecordId { get; set; }

    [MaxLength(Constants.RecordNoteLength)]
    public string Note { get; set; } = string.Empty;
}
