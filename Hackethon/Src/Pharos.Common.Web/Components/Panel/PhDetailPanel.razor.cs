namespace Pharos.Common.Web.Components.Panel;

public partial class PhDetailPanel
{
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public string Width { get; set; } = "66.6667%";
    [Parameter] public bool ShowLimiter { get; set; } = true;
    [Parameter] public bool ShowCloseButton { get; set; } = true;
    [Parameter] public EventCallback OnCloseClick { get; set; }

    private string InlineStyle => $"--ph-detail-panel-width: {Width};";

    private bool ShouldShowCloseButton => ShowCloseButton && OnCloseClick.HasDelegate;

    private Task HandleCloseClicked(MouseEventArgs _)
    {
        return OnCloseClick.InvokeAsync();
    }
}
