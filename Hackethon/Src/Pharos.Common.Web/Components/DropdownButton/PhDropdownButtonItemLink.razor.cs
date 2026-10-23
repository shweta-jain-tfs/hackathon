namespace Pharos.Common.Web.Components.DropdownButton;

public partial class PhDropdownButtonItemLink
{
    [Parameter, EditorRequired] public string? Text { get; set; }
    [Parameter, EditorRequired] public string? Uri { get; set; }
    [Parameter] public EventCallback OnClick { get; set; }

    protected Task OnClickHandler(MouseEventArgs ev)
    {
        OnClick.InvokeAsync();
        return Task.CompletedTask;
    }
}