using Pharos.Server.Application.TaskCard;

namespace Pharos.Server.TaskCard.Application.Queries;

public class TaskCardInformationDto
{
    public int NoOfTaskCards { get; set; }
    public TaskCardState HighestTaskCardState { get; set; }
}