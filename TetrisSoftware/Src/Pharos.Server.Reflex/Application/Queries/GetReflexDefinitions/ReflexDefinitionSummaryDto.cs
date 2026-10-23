namespace Pharos.Server.Reflex.Application.Queries.GetReflexDefinitions;
public class ReflexDefinitionSummaryDto
{
    public int Id { get; set; }
    public string? Name { get; set; } = string.Empty;
    public bool Automatic { get; set; }
    public bool Active { get; set; }
    public bool HasRequestorEnabled { get; set; }
    public string? RequestorId { get; set; }

}
