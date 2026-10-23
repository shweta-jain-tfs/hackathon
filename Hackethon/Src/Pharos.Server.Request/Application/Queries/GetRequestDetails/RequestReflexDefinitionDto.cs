namespace Pharos.Server.Request.Application.Queries.GetRequestDetails;

public class RequestReflexDefinitionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public bool Automatic { get; set; }
    public string? RequestorId { get; set; }
    public RequestReflexConditionDto ReflexCondition { get; set; } = null!;
    public List<RequestReflexTestDto> ReflexTests { get; set; } = null!;
}