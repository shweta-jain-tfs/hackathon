namespace Pharos.Server.Request.Application.Queries.GetRequestDetails;

public class RequestReflexTestDto
{
    public int Id { get; set; }
    public string TestName { get; set; } = string.Empty;
    public string MethodName { get; set; } = string.Empty;
    public int NumberOfReplicates { get; set; }
    public int InstrumentDilutionFactor { get; set; }
    public int TestSortOrder { get; set; }
}