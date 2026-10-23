using System.Text.Json.Serialization;
using Pharos.Server.Domain.Enums;
using Pharos.Server.SystemErrors.Domain;

namespace Pharos.Server.SystemErrors.Application.Queries.GetSystemErrors;

public class SystemErrorSummaryDto
{
    public int Id { get; set; }
    public int Code { get; set; }
    public ErrorClassification Classification { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string? InstrumentName { get; set; }

    [JsonIgnore]
    public string? InstrumentSerialNumber { get; set; }

    public ResponseAction? Response { get; set; }
    
    public bool IsAwaitingAcknowledge { get; set; }
    public bool IsUnhandled { get; set; }
}