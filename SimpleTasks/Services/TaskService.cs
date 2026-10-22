using SimpleTasks.Models;

namespace SimpleTasks.Services;

// One instance per Blazor circuit. Replace with database storage for persistence.
public sealed class TaskService
{
    public const int MaxTitleLength = 120;
    private readonly List<TaskItem> tasks =
    [
        new() { Title = "Explore the Blazor project", IsCompleted = true },
        new() { Title = "Add your first task" },
        new() { Title = "Build something useful" }
    ];

    public IReadOnlyList<TaskItem> Tasks => tasks.AsReadOnly();

    public bool Add(string? title)
    {
        var trimmed = title?.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.Length > MaxTitleLength)
            return false;
        tasks.Add(new TaskItem { Title = trimmed });
        return true;
    }

    public void Toggle(Guid id)
    {
        var task = tasks.Find(task => task.Id == id);
        if (task is not null)
            task.IsCompleted = !task.IsCompleted;
    }

    public void Delete(Guid id) => tasks.RemoveAll(task => task.Id == id);
}
