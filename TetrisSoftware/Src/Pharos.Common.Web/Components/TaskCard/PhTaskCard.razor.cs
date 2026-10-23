using Pharos.Common.Web.Components.Status;
using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.TaskCard;

/// <summary>
/// Notification card displaying a task's title, severity, image, and a link to a relevant page.
/// </summary>
public partial class PhTaskCard
{
    [Inject] private NavigationManager? NavigationManager { get; set; }

    [Parameter] public bool DisplayTime { get; set; }

    [Parameter] public string Description { get; set; } = string.Empty;

    [Parameter] public TaskCardImageType ImageType { get; set; }

    [Parameter] public bool NavigationForceLoad { get; set; } = false;
    [Parameter] public string? NavigationUrlForCard { get; set; }

    [Parameter] public TaskCardState State { get; set; } = TaskCardState.Information;

    [Parameter] public string? Time { get; set; }

    [Parameter] public string? Title { get; set; }

    private void HandleClickOnCard()
    {
        if (!string.IsNullOrEmpty(NavigationUrlForCard) && NavigationManager != null)
        {
            NavigationManager.NavigateTo(NavigationUrlForCard, NavigationForceLoad);
        }
    }

    [Parameter] public bool HasHelpInformation { get; set; }

    [Parameter] public EventCallback OnHelpClicked { get; set; }

    private static StatusType GetStatus(TaskCardState state)
    {
        return state switch
        {
            TaskCardState.Information => StatusType.Info,
            TaskCardState.Warning => StatusType.Warning,
            TaskCardState.Error => StatusType.Alarm,
            TaskCardState.FlashingRed => StatusType.Alarm,
            _ => StatusType.None
        };
    }

    private void HandleClickOnInformationIcon()
    {
        OnHelpClicked.InvokeAsync();
    }

    private string TaskCardCss => new CssBuilder("ph-task-card")
        .AddClass("ph-task-card--clickable")
        .AddClass("ph-task-card--alarm", State == TaskCardState.FlashingRed || State == TaskCardState.Error)
        .AddClass("ph-task-card--warning", State == TaskCardState.Warning)
        .AddClass("ph-task-card--info", State == TaskCardState.Information)
        .Build();


}

public enum TaskCardSize
{
    Default,
    Large
}

