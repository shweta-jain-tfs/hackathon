using Pharos.Server.Application.TaskCard;

namespace Pharos.Server.TaskCard.Application.Queries;

public class TaskCardListItemDto
{
    public int Id { get; set; }
    public TaskCardType CardType { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public string? InstrumentNumber { get; set; }
    public string? InstrumentDisplayName { get; set; }
    public TaskCardState CardState { get; set; }
}
