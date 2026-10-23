namespace Pharos.Server.SystemUpdate.Application.Queries.GetUnacknowledgedUpdateOutcome;

public class SystemUpdateOutcomeDto
{
    public bool IsAcknowledged { get; set; }
    public bool IsSuccessful { get; set; }
}