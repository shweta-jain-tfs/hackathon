using Pharos.Server.Domain.Enums;
using Pharos.Server.SystemErrors.Domain;

namespace Pharos.Server.SystemErrors.Application.Queries.GetSystemErrorDetails;

public class SystemErrorDetailsDto
{
    public int Id { get; set; }
    public int Code { get; set; }
    public ErrorClassification Classification { get; set; }
    public List<string> Parameters { get; set; } = [];
    public List<ResponseAction> PossibleResponses { get; set; } = [];
    public DateTimeOffset Timestamp { get; set; }
    public string? InstrumentName { get; set; }
    public ResponseAction? Response { get; set; }
    public DateTimeOffset? ResponseTimestamp { get; set; }
}