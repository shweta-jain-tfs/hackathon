using System.ComponentModel.DataAnnotations;
using Pharos.Server.Application.AuditTrail.Enums;
using Constants = Pharos.Server.Domain.Constants;

namespace Pharos.Server.AuditTrail.Domain
{
    public class AuditTrailRecord
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Specified ID assigned to each Event
        /// </summary>
        [MaxLength(Constants.EventIdLength)]
        public EventId EventId { get; set; }

        /// <summary>
        /// Fields that have modified due to the event occurred
        /// </summary>
        [MaxLength(Constants.EventDataLength)]
        public required List<AuditEventData> EventData { get; set; }

        [MaxLength(Constants.HashLength)]
        public string Hash { get; set; } = string.Empty;

        /// <summary>
        /// The record creation timestamp.
        /// </summary>
        public DateTimeOffset Created { get; set; }

        /// <summary>
        /// The ID of the user who created the record.
        /// </summary>
        [MaxLength(Constants.UsernameLength)]
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Mandatory comment for each audit trail record
        /// </summary>
        [MaxLength(Constants.AuditTrailCommentLength)]
        public string Comment { get; set; } = string.Empty;

        /// <summary>
        /// EventData in string form used for multi-column search
        /// </summary>
        [MaxLength(Constants.EventDataLength)]
        public string EventDataString { get; set; } = string.Empty;

        /// <summary>
        /// Optional note for each audit trail record
        /// </summary>
        public AuditTrailRecordNotes? AuditTrailRecordNotes { get; set; }
    }
}
