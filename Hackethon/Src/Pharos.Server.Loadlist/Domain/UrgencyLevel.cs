using Pharos.Server.Application.TaskCard;

namespace Pharos.Server.LoadList.Domain;

public class UrgencyLevel
{
    public bool AnythingMissing { get; set; }
    public TaskCardState? TaskCardState { get; set; }
    public TimeSpan? ExpiresAt { get; set; }
}