using Pharos.Server.Application.TaskCard;

namespace Pharos.Server.TaskCard.Application.Queries;

public class TaskCardDto
{
    public int Id { get; set; }
    public TaskCardType CardType { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public TaskCardState CardState { get; set; }
}