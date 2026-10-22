namespace SimpleTasks.Models;

public sealed class TaskItem
{
    public Guid Id { get; } = Guid.NewGuid();
    public required string Title { get; init; }
    public bool IsCompleted { get; set; }
}
