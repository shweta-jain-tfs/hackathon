namespace Pharos.Server.Reflex.Application.Queries.GetReflexDefinition;

public class ReflexDefinitionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public bool Automatic { get; set; }
    public bool Active { get; set; }
    public bool HasRequestorEnabled { get; set; }
    public string? RequestorId { get; set; } = null!;
    public string? CommentCode { get; set; } = null!;
    public ReflexConditionDto? ReflexCondition { get; set; }

    /// <summary>
    /// The date and time when the reflex definition details were queried. This is used to merge concurrent edits.
    /// </summary>
    public DateTimeOffset QueriedAt { get; set; }

    public List<ReflexTestDto>? ReflexTests { get; set; }
}