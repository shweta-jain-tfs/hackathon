using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.Icons;

/// <summary>
/// Colored status icon. Has more sizes than <see cref="PhIcon"/> and is multi-colored.
/// There's an inner accent icon which is hidden in the smallest sizes. Some icons have
/// an optional outline that follows the shape of the icon. Others, such as workflow and
/// alert icons, are monochrome.
/// </summary>
public partial class PhStatusIcon
{
    [Parameter, EditorRequired] public StatusIconType Icon { get; set; } = StatusIconType.Ok;
    [Parameter] public StatusIconSize Size { get; set; } = StatusIconSize.Md;

    /// <summary>
    /// Show outline around the icon. Intended for use with large icons on colored backgrounds.
    /// </summary>
    [Parameter] public bool Outline { get; set; }

    private Type? _iconComponent;

    protected string CssClasses { get; set; } = "";

    private static Type GetIconComponentType(StatusIconType icon)
    {
        return icon switch
        {
            StatusIconType.Ok or StatusIconType.Standby => typeof(Svg.Status_Ok),
            StatusIconType.Warning => typeof(Svg.Status_Warning),
            StatusIconType.Alarm => typeof(Svg.Status_Alarm),
            StatusIconType.Alert => typeof(Svg.Alert),
            StatusIconType.Off => typeof(Svg.Status_Off),
            StatusIconType.PendingAction => typeof(Svg.Status_PendingAction),
            StatusIconType.Progress => typeof(Svg.Status_Progress),
            StatusIconType.WorkflowCompleted
                or StatusIconType.WorkflowInProgress
                or StatusIconType.WorkflowNotStarted => typeof(Svg.Status_Workflow),
            _ => throw new ArgumentOutOfRangeException(
                nameof(icon),
                icon,
                $"No SVG mapping for {icon}.")
        };
    }

    protected override void OnParametersSet()
    {
        _iconComponent = GetIconComponentType(Icon);

        var cssBuilder = new CssBuilder("ph-status-icon")
                .AddClass("ph-status-icon--ok", Icon == StatusIconType.Ok)
                .AddClass("ph-status-icon--standby", Icon == StatusIconType.Standby)
                .AddClass("ph-status-icon--off", Icon == StatusIconType.Off)
                .AddClass("ph-status-icon--alarm", Icon == StatusIconType.Alarm)
                .AddClass("ph-status-icon--alert", Icon == StatusIconType.Alert)
                .AddClass("ph-status-icon--warning", Icon == StatusIconType.Warning)
                .AddClass("ph-status-icon--pending-action", Icon == StatusIconType.PendingAction)
                .AddClass("ph-status-icon--progress", Icon == StatusIconType.Progress)
                .AddClass("ph-status-icon--no-accent", Size == StatusIconSize.Xxs || Size == StatusIconSize.Xs)
                .AddClass("ph-status-icon--outline", Outline)
                .AddClass("ph-status-icon--size-xxs", Size == StatusIconSize.Xxs)
                .AddClass("ph-status-icon--size-xs", Size == StatusIconSize.Xs)
                .AddClass("ph-status-icon--size-sm", Size == StatusIconSize.Sm)
                .AddClass("ph-status-icon--size-md", Size == StatusIconSize.Md)
                .AddClass("ph-status-icon--size-lg", Size == StatusIconSize.Lg)
                .AddClass("ph-status-icon--size-xl", Size == StatusIconSize.Xl)
                .AddClass("ph-status-icon--size-2xl", Size == StatusIconSize.Xxl)
                .AddClass("ph-status-icon--size-3xl", Size == StatusIconSize.Xxxl)
                .AddClass("ph-status-icon--size-4xl", Size == StatusIconSize.Xxxxl)
                .AddClass("ph-status-icon--workflow-completed", Icon == StatusIconType.WorkflowCompleted)
                .AddClass("ph-status-icon--workflow-in-progress", Icon == StatusIconType.WorkflowInProgress)
                .AddClass("ph-status-icon--workflow-not-started", Icon == StatusIconType.WorkflowNotStarted);

        CssClasses = cssBuilder.Build();
    }
}