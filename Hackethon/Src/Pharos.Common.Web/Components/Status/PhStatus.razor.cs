using Pharos.Common.Web.Components.Icons;
using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.Status;

/// <summary>
/// Status symbol with text. Either the icon or the text can be omitted. Can have a link.
/// The size of the icon can be set explicitly or automatically based on the font size.
/// </summary>
public partial class PhStatus
{
    [Parameter] public string? Href { get; set; }
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }
    [Parameter] public StatusGap Gap { get; set; } = StatusGap.Xs;
    [Parameter] public StatusSize Size { get; set; } = StatusSize.Auto;

    /// <summary>
    /// Font size of the text. Set <see cref="Size"/> to <see cref="StatusSize.Auto"/> to automatically set the icon size based on this font size.
    /// </summary>
    [Parameter] public StatusFontSize FontSize { get; set; } = StatusFontSize.Inherit;
    [Parameter] public StatusType Status { get; set; } = StatusType.None;
    [Parameter] public string? Text { get; set; }

    private string CssClasses => new CssBuilder("ph-status")
        .AddClass("ph-status--gap-sm", Gap == StatusGap.Sm)
        .Build();

    private string CssClassesText => new CssBuilder("")
        .AddClass("t-body1", FontSize == StatusFontSize.Body1)
        .AddClass("t-body2", FontSize == StatusFontSize.Body2)
        .AddClass("t-h1", FontSize == StatusFontSize.H1)
        .AddClass("t-h2", FontSize == StatusFontSize.H2)
        .AddClass("t-h3", FontSize == StatusFontSize.H3)
        .AddClass("t-helper-text", FontSize == StatusFontSize.HelperText)
        .Build();

    private StatusIconType? Icon => Status switch
    {
        StatusType.Alarm => StatusIconType.Alarm,
        StatusType.Alert => StatusIconType.Alert,
        StatusType.Disconnected => StatusIconType.Off,
        StatusType.Info => StatusIconType.PendingAction,
        StatusType.Processing => StatusIconType.Progress,
        StatusType.Success => StatusIconType.Ok,
        StatusType.Warning => StatusIconType.Warning,
        StatusType.WorkflowCompleted => StatusIconType.WorkflowCompleted,
        StatusType.WorkflowInProgress => StatusIconType.WorkflowInProgress,
        StatusType.WorkflowNotStarted => StatusIconType.WorkflowNotStarted,
        _ => null,
    };

    private StatusIconSize IconSize => Size switch
    {
        StatusSize.Auto => AutoIconSize,
        StatusSize.Xxs => StatusIconSize.Xxs,
        StatusSize.Xs => StatusIconSize.Xs,
        StatusSize.Sm => StatusIconSize.Sm,
        StatusSize.Md => StatusIconSize.Md,
        StatusSize.Lg => StatusIconSize.Lg,
        StatusSize.Xl => StatusIconSize.Xl,
        _ => StatusIconSize.Sm,
    };

    private StatusIconSize AutoIconSize => FontSize switch
    {
        StatusFontSize.Body1 => StatusIconSize.Md,
        StatusFontSize.Body2 => StatusIconSize.Sm,
        StatusFontSize.H1 => StatusIconSize.Xl,
        StatusFontSize.H2 => StatusIconSize.Xl,
        StatusFontSize.H3 => StatusIconSize.Lg,
        StatusFontSize.HelperText => StatusIconSize.Sm,
        _ => StatusIconSize.Sm,
    };

}
